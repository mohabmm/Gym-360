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
import { test, expect, type Locator, type Page } from '@playwright/test';
import { isShown } from './helpers/ui';
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

type Walk = {
  base: string;
  routes: string[];
  setRoute: (r: string) => void;
  app: 'admin' | 'staff';
};

async function walk(page: Page, { base, routes, setRoute, app }: Walk) {
  for (const r of routes) {
    setRoute(r);
    await page.goto(`${base}${r}`, { waitUntil: 'domcontentloaded' }).catch(() => {});
    await page.waitForTimeout(2500);
    // basic render sanity: body has text
    const txt = (await page.locator('body').innerText().catch(() => '')) || '';
    if (txt.trim().length < 10) {
      record({ app, route: r, kind: 'flow', detail: 'blank/near-empty page render' });
    }
    await page.screenshot({ path: `test_logs/shot_${app}_${r.replace(/\W+/g, '_') || 'root'}.png` }).catch(() => {});
  }
}

async function openPage(page: Page, url: string) {
  await page.goto(url, { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(2000);
}

// fill by label proximity (matches admin-comprehensive-bugs.spec.ts pattern)
async function fillByLabel(page: Page, label: string, val: string) {
  const inp = page.locator(`label:has-text("${label}")`).first().locator('~ input, + input').first();
  if (await isShown(inp)) await inp.fill(val);
}

async function fillIfShown(locator: Locator, val: string) {
  if (await isShown(locator)) await locator.fill(val);
}

async function canSubmit(submit: Locator): Promise<boolean> {
  return (await isShown(submit)) && (await submit.isEnabled().catch(() => false));
}

async function submitGymForm(page: Page) {
  const stamp = Date.now().toString().slice(-6);
  await fillByLabel(page, 'Gym Name', `QA Gym ${stamp}`);
  await fillByLabel(page, 'Gym Code', `QA${stamp}`);
  await fillByLabel(page, 'Email', `qa${stamp}@demo.local`);
  await fillByLabel(page, 'Phone', `+2010${stamp}00`);
  const submit = page.locator('button:has-text("Create"), button:has-text("Save"), button[type="submit"]').last();
  if (await canSubmit(submit)) {
    await submit.click();
    await page.waitForTimeout(3000);
  } else {
    record({ app: 'admin', route: '/gyms (create)', kind: 'flow', detail: 'Create button disabled or missing after filling form' });
  }
}

// Safe create cycle: add a gym, capture POST payload.
async function createGymCycle(page: Page) {
  await openPage(page, `${ADMIN_URL}/gyms`);
  const addBtn = page.locator('button:has-text("Add Gym"), button:has-text("إضافة")').first();
  if (!(await isShown(addBtn))) {
    record({ app: 'admin', route: '/gyms', kind: 'flow', detail: 'Add Gym button not found' });
    return;
  }
  await addBtn.click();
  await page.waitForTimeout(1500);
  await submitGymForm(page);
}

async function submitAnnouncementForm(page: Page, stamp: string) {
  await fillIfShown(page.locator('input[name="title"], input[placeholder*="title" i], input[placeholder*="عنوان"]').first(), `QA Announce ${stamp}`);
  await fillIfShown(page.locator('textarea').first(), `QA test body ${stamp}`);
  const submit = page.locator('button:has-text("Create"), button:has-text("Save"), button:has-text("Publish"), button[type="submit"]').last();
  if (await submit.isEnabled().catch(() => false)) { await submit.click(); await page.waitForTimeout(2500); }
  else record({ app: 'staff', route: '/announcements (create)', kind: 'flow', detail: 'submit disabled after fill' });
}

// Create cycle: announcement (least destructive)
async function createAnnouncementCycle(page: Page, stamp: string) {
  await openPage(page, `${STAFF_URL}/announcements`);
  const annBtn = page.locator('button:has-text("New"), button:has-text("Create"), button:has-text("Add"), button:has-text("إضافة")').first();
  if (!(await isShown(annBtn))) {
    record({ app: 'staff', route: '/announcements', kind: 'flow', detail: 'Create/New button not found' });
    return;
  }
  await annBtn.click();
  await page.waitForTimeout(1200);
  await submitAnnouncementForm(page, stamp);
}

// Create cycle: class
async function openClassCreateCycle(page: Page) {
  await openPage(page, `${STAFF_URL}/classes`);
  const clsBtn = page.locator('button:has-text("New"), button:has-text("Create"), button:has-text("Add Class"), button:has-text("Add"), button:has-text("إضافة")').first();
  if (!(await isShown(clsBtn))) {
    record({ app: 'staff', route: '/classes', kind: 'flow', detail: 'Add Class button not found' });
    return;
  }
  await clsBtn.click();
  await page.waitForTimeout(1200);
  record({ app: 'staff', route: '/classes (create)', kind: 'flow', detail: 'class create modal opened (manual field inspection in headed run)' });
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
    await walk(page, { base: ADMIN_URL, routes, setRoute: (r) => (route = r), app: 'admin' });

    route = '/gyms (create)';
    await createGymCycle(page);

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
    await walk(page, { base: STAFF_URL, routes, setRoute: (r) => (route = r), app: 'staff' });

    const stamp = Date.now().toString().slice(-6);

    route = '/announcements (create)';
    await createAnnouncementCycle(page, stamp);

    route = '/classes (create)';
    await openClassCreateCycle(page);

    persist();
    expect(true).toBe(true);
  });
});
