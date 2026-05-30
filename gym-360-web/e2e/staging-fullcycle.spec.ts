/**
 * Staging full-cycle headed sweep — admin + staff web.
 *
 * Walks every route, captures console errors / pageerrors / network 4xx-5xx
 * (with request payloads), and runs safe create cycles. Findings are written
 * to test_logs/staging_fullcycle_findings.json for layer triage.
 *
 * Run (headed, serial):
 *   ADMIN_URL=https://staging-admin.gym-365.com \
 *   STAFF_URL=https://staging-staff.gym-365.com \
 *   npx playwright test e2e/staging-fullcycle.spec.ts --project=chromium --headed --workers=1 --no-deps
 */
import { test, expect, type Page } from '@playwright/test';
import { mkdirSync, writeFileSync, existsSync, readFileSync } from 'fs';
import path from 'path';

const ADMIN_URL = process.env.ADMIN_URL ?? 'https://staging-admin.gym-365.com';
const STAFF_URL = process.env.STAFF_URL ?? 'https://staging-staff.gym-365.com';
const EMAIL_ADMIN = 'admin@demo.local';
const EMAIL_STAFF = 'staff@demo.local';
const PASS = '123456';

const FINDINGS = path.resolve(__dirname, '../../test_logs/staging_fullcycle_findings.json');

type Finding = {
  app: 'admin' | 'staff';
  route: string;
  kind: 'console' | 'pageerror' | 'network' | 'flow';
  detail: string;
  method?: string;
  status?: number;
  url?: string;
  payload?: string;
};

const all: Finding[] = [];

function record(f: Finding) {
  all.push(f);
  // eslint-disable-next-line no-console
  console.log(`[FIND][${f.app}][${f.route}][${f.kind}] ${f.detail}`);
}

function persist() {
  mkdirSync(path.dirname(FINDINGS), { recursive: true });
  const prev: Finding[] = existsSync(FINDINGS) ? JSON.parse(readFileSync(FINDINGS, 'utf8')) : [];
  writeFileSync(FINDINGS, JSON.stringify([...prev, ...all], null, 2));
}

// Attach listeners that route findings to the current route label.
function attach(page: Page, app: 'admin' | 'staff', getRoute: () => string) {
  page.on('console', (m) => {
    if (m.type() === 'error') {
      const t = m.text();
      // ignore benign noise
      if (/favicon|Failed to load resource: the server responded with a status of 401/.test(t)) return;
      record({ app, route: getRoute(), kind: 'console', detail: t.slice(0, 400) });
    }
  });
  page.on('pageerror', (e) => {
    record({ app, route: getRoute(), kind: 'pageerror', detail: e.message.slice(0, 400) });
  });
  page.on('response', async (res) => {
    const s = res.status();
    if (s < 400) return;
    const req = res.request();
    const url = res.url();
    if (/\.(png|jpg|svg|ico|woff2?|css|js)(\?|$)/.test(url)) return;
    let payload: string | undefined;
    try { payload = req.postData()?.slice(0, 500); } catch { /* */ }
    record({
      app, route: getRoute(), kind: 'network',
      detail: `${req.method()} ${s} ${url}`,
      method: req.method(), status: s, url, payload,
    });
  });
}

async function loginAdmin(page: Page) {
  await page.goto(`${ADMIN_URL}/login`, { waitUntil: 'domcontentloaded' });
  await page.fill('input[type="email"]', EMAIL_ADMIN);
  await page.fill('input[type="password"]', PASS);
  await page.click('button[type="submit"]');
  await page.waitForURL((u) => !u.pathname.startsWith('/login'), { timeout: 30000 });
}

async function loginStaff(page: Page) {
  await page.goto(`${STAFF_URL}/login`, { waitUntil: 'domcontentloaded' });
  await page.fill('input[type="email"]', EMAIL_STAFF);
  await page.fill('input[type="password"]', PASS);
  await page.click('button[type="submit"]');
  await page.waitForURL((u) => !u.pathname.startsWith('/login'), { timeout: 30000 });
  if (page.url().includes('/select-gym')) {
    await page.locator('button, a[href]').first().click({ timeout: 8000 }).catch(() => {});
    await page.waitForURL((u) => !u.pathname.startsWith('/select-gym'), { timeout: 15000 }).catch(() => {});
  }
}

async function walk(page: Page, base: string, routes: string[], setRoute: (r: string) => void, app: string) {
  for (const r of routes) {
    setRoute(r);
    await page.goto(`${base}${r}`, { waitUntil: 'domcontentloaded' }).catch(() => {});
    await page.waitForTimeout(2500);
    // basic render sanity: body has text
    const txt = (await page.locator('body').innerText().catch(() => '')) || '';
    if (txt.trim().length < 10) {
      record({ app: app as 'admin' | 'staff', route: r, kind: 'flow', detail: 'blank/near-empty page render' });
    }
    await page.screenshot({ path: `test_logs/shot_${app}_${r.replace(/\W+/g, '_') || 'root'}.png` }).catch(() => {});
  }
}

// ───────────────────────────── ADMIN ─────────────────────────────
test.describe('Admin staging full cycle', () => {
  test.setTimeout(240000);
  let route = '/login';

  test('admin: walk + create cycle', async ({ page }) => {
    attach(page, 'admin', () => route);
    await loginAdmin(page);

    const routes = [
      '/', '/gyms', '/users', '/subscriptions', '/plans', '/content',
      '/analytics', '/analytics/churn', '/reports', '/support', '/settings',
    ];
    await walk(page, ADMIN_URL, routes, (r) => (route = r), 'admin');

    // Safe create cycle: add a gym, capture POST payload.
    route = '/gyms (create)';
    await page.goto(`${ADMIN_URL}/gyms`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2000);
    const addBtn = page.locator('button:has-text("Add Gym"), button:has-text("إضافة")').first();
    if (await addBtn.isVisible().catch(() => false)) {
      await addBtn.click();
      await page.waitForTimeout(1500);
      const stamp = Date.now().toString().slice(-6);
      const name = `QA Gym ${stamp}`;
      const code = `QA${stamp}`;
      // fill by label proximity (matches admin-comprehensive-bugs.spec.ts pattern)
      const fillByLabel = async (label: string, val: string) => {
        const inp = page.locator(`label:has-text("${label}")`).first().locator('~ input, + input').first();
        if (await inp.isVisible().catch(() => false)) await inp.fill(val);
      };
      await fillByLabel('Gym Name', name);
      await fillByLabel('Gym Code', code);
      await fillByLabel('Email', `qa${stamp}@demo.local`);
      await fillByLabel('Phone', `+2010${stamp}00`);
      const submit = page.locator('button:has-text("Create"), button:has-text("Save"), button[type="submit"]').last();
      if (await submit.isVisible().catch(() => false) && await submit.isEnabled().catch(() => false)) {
        await submit.click();
        await page.waitForTimeout(3000);
      } else {
        record({ app: 'admin', route: '/gyms (create)', kind: 'flow', detail: 'Create button disabled or missing after filling form' });
      }
    } else {
      record({ app: 'admin', route: '/gyms', kind: 'flow', detail: 'Add Gym button not found' });
    }

    persist();
    expect(true).toBe(true);
  });
});

// ───────────────────────────── STAFF ─────────────────────────────
test.describe('Staff staging full cycle', () => {
  test.setTimeout(300000);
  let route = '/login';

  test('staff: walk + create cycles', async ({ page }) => {
    attach(page, 'staff', () => route);
    await loginStaff(page);

    const routes = [
      '/', '/members', '/trainers', '/staff', '/classes', '/check-ins',
      '/announcements', '/messages', '/gamification', '/exercises',
      '/billing', '/subscription', '/analytics', '/data-import', '/kiosk-pin', '/settings',
    ];
    await walk(page, STAFF_URL, routes, (r) => (route = r), 'staff');

    const stamp = Date.now().toString().slice(-6);

    // Create cycle: announcement (least destructive)
    route = '/announcements (create)';
    await page.goto(`${STAFF_URL}/announcements`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2000);
    const annBtn = page.locator('button:has-text("New"), button:has-text("Create"), button:has-text("Add"), button:has-text("إضافة")').first();
    if (await annBtn.isVisible().catch(() => false)) {
      await annBtn.click();
      await page.waitForTimeout(1200);
      const title = page.locator('input[name="title"], input[placeholder*="title" i], input[placeholder*="عنوان"]').first();
      if (await title.isVisible().catch(() => false)) await title.fill(`QA Announce ${stamp}`);
      const body = page.locator('textarea').first();
      if (await body.isVisible().catch(() => false)) await body.fill(`QA test body ${stamp}`);
      const submit = page.locator('button:has-text("Create"), button:has-text("Save"), button:has-text("Publish"), button[type="submit"]').last();
      if (await submit.isEnabled().catch(() => false)) { await submit.click(); await page.waitForTimeout(2500); }
      else record({ app: 'staff', route: '/announcements (create)', kind: 'flow', detail: 'submit disabled after fill' });
    } else {
      record({ app: 'staff', route: '/announcements', kind: 'flow', detail: 'Create/New button not found' });
    }

    // Create cycle: class
    route = '/classes (create)';
    await page.goto(`${STAFF_URL}/classes`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2000);
    const clsBtn = page.locator('button:has-text("New"), button:has-text("Create"), button:has-text("Add Class"), button:has-text("Add"), button:has-text("إضافة")').first();
    if (await clsBtn.isVisible().catch(() => false)) {
      await clsBtn.click();
      await page.waitForTimeout(1200);
      record({ app: 'staff', route: '/classes (create)', kind: 'flow', detail: 'class create modal opened (manual field inspection in headed run)' });
    } else {
      record({ app: 'staff', route: '/classes', kind: 'flow', detail: 'Add Class button not found' });
    }

    persist();
    expect(true).toBe(true);
  });
});
