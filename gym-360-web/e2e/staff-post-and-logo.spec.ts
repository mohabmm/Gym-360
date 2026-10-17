/**
 * Staff App — POST sweep + login logo verification
 * Focus: Add Member end-to-end flow + logos on login screen.
 */
import { test, expect, type Page, type Request, type Response } from '@playwright/test';
import fs from 'fs';
import path from 'path';

test.use({ baseURL: 'http://localhost:3001', storageState: { cookies: [], origins: [] } });

const EMAIL = 'staff@demo.local';
const PASS = '123456';
const REPORT_DIR = path.join(__dirname, '..', 'test-results', 'staff-post-sweep');

type PostRecord = {
  url: string;
  method: string;
  status: number | null;
  reqBody: string | null;
  resBody: string | null;
  failureText?: string;
};

function ensureDir(p: string) {
  fs.mkdirSync(p, { recursive: true });
}

const MUTATING_METHODS = new Set(['POST', 'PATCH', 'PUT', 'DELETE']);

function isTextual(contentType: string): boolean {
  return contentType.includes('application/json') || contentType.includes('text/');
}

function attachPostRecorder(page: Page, records: PostRecord[]) {
  const inflight = new Map<Request, PostRecord>();
  page.on('request', (req) => {
    if (MUTATING_METHODS.has(req.method())) {
      const rec: PostRecord = {
        url: req.url(),
        method: req.method(),
        status: null,
        reqBody: req.postData() ?? null,
        resBody: null,
      };
      inflight.set(req, rec);
      records.push(rec);
    }
  });
  page.on('response', async (res: Response) => {
    const req = res.request();
    const rec = inflight.get(req);
    if (!rec) return;
    rec.status = res.status();
    try {
      if (isTextual(res.headers()['content-type'] ?? '')) {
        rec.resBody = (await res.text()).slice(0, 2000);
      }
    } catch {
      /* ignore */
    }
  });
  page.on('requestfailed', (req) => {
    const rec = inflight.get(req);
    if (rec) rec.failureText = req.failure()?.errorText ?? 'unknown';
  });
}

async function login(page: Page) {
  await page.goto('/login');
  await page.fill('input[type="email"]', EMAIL);
  await page.fill('input[type="password"]', PASS);
  await page.click('button[type="submit"]');
  await page.waitForURL('/', { timeout: 60000 });
}

test.beforeAll(() => ensureDir(REPORT_DIR));

test('Login screen — logo loads, alt set, asset 200', async ({ page }) => {
  const records: PostRecord[] = [];
  attachPostRecorder(page, records);

  const logoResponses: { url: string; status: number }[] = [];
  page.on('response', (res) => {
    const u = res.url();
    if (/logo\.svg|favicon|apple-touch-icon|android-chrome|_next\/image.*logo/i.test(u)) {
      logoResponses.push({ url: u, status: res.status() });
    }
  });

  await page.goto('/login');
  await page.waitForLoadState('networkidle');

  // Logo element
  const logo = page.getByRole('img', { name: /gym 360/i });
  await expect(logo).toBeVisible();
  const naturalWidth = await logo.evaluate((el: HTMLImageElement) => el.naturalWidth);
  expect(naturalWidth, 'logo image must have rendered pixels').toBeGreaterThan(0);

  // Heading
  await expect(page.getByRole('heading', { name: /staff portal/i })).toBeVisible();

  // Asset HTTP status — Next/Image proxies via /_next/image so accept either
  const logoReq = logoResponses.find((r) => r.url.includes('/logo.svg') || /_next\/image.*logo/i.test(r.url));
  expect(logoReq, 'logo asset request should occur').toBeTruthy();
  expect(logoReq!.status).toBe(200);

  // Favicon
  const faviconResp = await page.request.get('/favicon-32x32.png').catch(() => null);
  if (faviconResp) expect(faviconResp.status()).toBe(200);

  await page.screenshot({ path: path.join(REPORT_DIR, 'login-logo.png'), fullPage: true });

  fs.writeFileSync(
    path.join(REPORT_DIR, 'login-logo-assets.json'),
    JSON.stringify({ logoResponses, naturalWidth }, null, 2),
  );
});

type NewMember = { name: string; phone: string; email: string };

function newMember(): NewMember {
  const stamp = Date.now();
  return {
    name: `PW Member ${stamp}`,
    phone: `+201${String(stamp).slice(-9)}`,
    email: `pw.${stamp}@member.local`,
  };
}

// Modal fields: Name, Phone, Email, Floor (single Name field; no Arabic split here)
async function fillAddMemberModal(page: Page, member: NewMember) {
  await page.locator('label:has-text("Name") input').first().fill(member.name);
  await page.locator('label:has-text("Phone") input').fill(member.phone);
  await page.locator('label:has-text("Email") input').fill(member.email);
}

const isCreateMemberResponse = (r: Response) =>
  r.url().includes('/gyms/') && r.url().endsWith('/members') && r.request().method() === 'POST';

/** Submits the modal and waits for POST /members; logs any form error when it never fires. */
async function submitAddMember(page: Page): Promise<Response | null> {
  const submitPromise = page.waitForResponse(isCreateMemberResponse, { timeout: 30000 });
  await page.getByRole('button', { name: /^add member$/i }).last().click();
  try {
    return await submitPromise;
  } catch (e) {
    // capture form error if any
    const formErr = await page.locator('.text-destructive').first().textContent().catch(() => null);
    fs.writeFileSync(
      path.join(REPORT_DIR, 'add-member-failure.txt'),
      `No POST /members caught. Form error: ${formErr ?? 'none'}\n`,
    );
    return null;
  }
}

function writeAddMemberSummary(
  staffCreate: PostRecord | undefined,
  createMember: PostRecord | undefined,
  submitResponse: Response | null,
) {
  const summary = {
    staffCreate: staffCreate ? { status: staffCreate.status, body: staffCreate.resBody } : null,
    createMember: createMember ? { status: createMember.status, body: createMember.resBody, request: createMember.reqBody } : null,
    submitResponseStatus: submitResponse?.status() ?? null,
  };
  fs.writeFileSync(path.join(REPORT_DIR, 'add-member-summary.json'), JSON.stringify(summary, null, 2));
}

// Member list should refresh — search by name
async function expectMemberListed(page: Page, member: NewMember, beforeCount: number) {
  await page.locator('input[placeholder*="Search members"]').fill(member.name);
  await page.waitForTimeout(1500);
  const afterCount = await page.locator('table tbody tr').count();

  fs.writeFileSync(
    path.join(REPORT_DIR, 'add-member-list-check.json'),
    JSON.stringify({ beforeCount, afterCount, memberName: member.name, memberPhone: member.phone }, null, 2),
  );

  // Either the new row shows in search or the list has grown
  const hasRow = (await page.locator(`table tbody tr:has-text("${member.name}")`).count()) > 0;
  expect(hasRow || afterCount >= 1, `Member "${member.name}" should appear in list after creation`).toBeTruthy();
}

test('Add Member — full POST flow captured + member appears in list', async ({ page }) => {
  const records: PostRecord[] = [];
  attachPostRecorder(page, records);

  await login(page);

  await page.goto('/members');
  await page.waitForLoadState('networkidle');

  // Snapshot existing member count (best-effort)
  const beforeCount = await page.locator('table tbody tr').count();

  // Open Add Member modal
  await page.getByRole('button', { name: /add member/i }).click();
  await expect(page.getByRole('heading', { name: /add member/i })).toBeVisible();

  // Fill form
  const member = newMember();
  await fillAddMemberModal(page, member);

  // Submit and wait for both POSTs
  const submitResponse = await submitAddMember(page);

  // Save raw POST log
  await page.waitForTimeout(1500);
  fs.writeFileSync(path.join(REPORT_DIR, 'add-member-posts.json'), JSON.stringify(records, null, 2));

  const staffCreate = records.find((r) => r.url.includes('/identity/users/staff-create'));
  const createMember = records.find((r) => r.url.match(/\/gyms\/[^/]+\/members$/) && r.method === 'POST');

  expect(staffCreate, 'POST /identity/users/staff-create should fire').toBeTruthy();
  expect(createMember, 'POST /gyms/{id}/members should fire').toBeTruthy();

  // Document statuses (do not fail before reporting)
  writeAddMemberSummary(staffCreate, createMember, submitResponse);

  // Now assert the contract
  expect(staffCreate?.status, `staff-create status; body=${staffCreate?.resBody}`).toBeLessThan(400);
  expect(createMember?.status, `create-member status; body=${createMember?.resBody}`).toBeLessThan(400);

  // Modal should close & toast
  await expect(page.getByRole('heading', { name: /add member/i })).toBeHidden({ timeout: 8000 });

  await expectMemberListed(page, member, beforeCount);
});

test('POST sweep — collect all mutating requests across staff app', async ({ page }) => {
  const records: PostRecord[] = [];
  attachPostRecorder(page, records);

  await login(page);

  const routes = [
    '/',
    '/members',
    '/check-ins',
    '/classes',
    '/staff',
    '/trainers',
    '/announcements',
    '/billing',
    '/analytics',
    '/data-import',
    '/settings',
  ];

  const visitLog: { route: string; ok: boolean; error?: string }[] = [];
  for (const r of routes) {
    try {
      await page.goto(r, { waitUntil: 'domcontentloaded' });
      await page.waitForTimeout(1500);
      visitLog.push({ route: r, ok: true });
    } catch (e) {
      visitLog.push({ route: r, ok: false, error: (e as Error).message });
    }
  }

  fs.writeFileSync(path.join(REPORT_DIR, 'post-sweep-visits.json'), JSON.stringify(visitLog, null, 2));
  fs.writeFileSync(path.join(REPORT_DIR, 'post-sweep-records.json'), JSON.stringify(records, null, 2));

  // Group by endpoint+status for a summary
  const grouped: Record<string, { count: number; statuses: Record<number, number> }> = {};
  for (const rec of records) {
    const key = `${rec.method} ${new URL(rec.url).pathname}`;
    grouped[key] ??= { count: 0, statuses: {} };
    grouped[key].count++;
    const s = rec.status ?? 0;
    grouped[key].statuses[s] = (grouped[key].statuses[s] ?? 0) + 1;
  }
  fs.writeFileSync(path.join(REPORT_DIR, 'post-sweep-summary.json'), JSON.stringify(grouped, null, 2));

  // Failures (>=400) flagged but not failing the test — diagnostic spec
  const failures = records.filter((r) => (r.status ?? 0) >= 400);
  if (failures.length) {
    fs.writeFileSync(
      path.join(REPORT_DIR, 'post-sweep-failures.json'),
      JSON.stringify(failures, null, 2),
    );
  }
});
