/**
 * Admin App — POST request deep coverage
 *
 * For every admin POST flow, drive the UI end-to-end and assert that the
 * resulting POST request returns a 2xx response. Captures request body and
 * response body so failures show the exact payload + server error.
 */

import { test, expect, type Page, type Request, type Response } from '@playwright/test';

const ADMIN_EMAIL = 'admin@demo.local';
const ADMIN_PASSWORD = '123456';

type Captured = {
  url: string;
  status: number;
  reqBody: unknown;
  resBody: string;
};

function makePostRecorder(page: Page, urlMatcher: (url: string) => boolean) {
  const captured: Captured[] = [];
  const seen = new Map<Request, Captured>();

  page.on('request', (req) => {
    if (req.method() !== 'POST') return;
    if (!urlMatcher(req.url())) return;
    let body: unknown = null;
    try { body = req.postDataJSON(); } catch { body = req.postData(); }
    const entry: Captured = { url: req.url(), status: 0, reqBody: body, resBody: '' };
    seen.set(req, entry);
    captured.push(entry);
  });

  page.on('response', async (res) => {
    const req = res.request();
    const entry = seen.get(req);
    if (!entry) return;
    entry.status = res.status();
    try { entry.resBody = (await res.text()).slice(0, 1000); } catch {}
  });

  return captured;
}

async function loginIfNeeded(page: Page) {
  await page.goto('/');
  if (page.url().includes('/login')) {
    await page.fill('input[type="email"]', ADMIN_EMAIL);
    await page.fill('input[type="password"]', ADMIN_PASSWORD);
    await page.click('button[type="submit"]');
    await page.waitForURL('/', { timeout: 30000 });
  }
}

function uniqueSuffix() {
  return `${Date.now().toString(36)}${Math.floor(Math.random() * 1000)}`;
}

test.describe('Admin POST — Gym lifecycle', () => {
  test.beforeEach(async ({ page }) => loginIfNeeded(page));

  test('POST /gyms — create gym returns 2xx', async ({ page }) => {
    const captured = makePostRecorder(page, (u) => /\/api\/v1\/gyms(\?|$)/.test(u));

    await page.goto('/gyms');
    await page.waitForTimeout(2500);

    await page.locator('button:has-text("Add Gym")').click();
    await expect(page.locator('label:has-text("Gym Name")').first()).toBeVisible({ timeout: 5000 });

    const sfx = uniqueSuffix();
    await page.locator('label:has-text("Gym Name")').first().locator('~ input, + input').fill(`E2E Gym ${sfx}`);
    await page.locator('label:has-text("Gym Code")').first().locator('~ input, + input').fill(`E2E${sfx}`.slice(0, 12));
    await page.locator('label:has-text("City")').first().locator('~ input, + input').fill('Cairo');

    const ownerSelect = page.locator('select').filter({ hasText: 'Select an owner' });
    if (await ownerSelect.count()) {
      const opts = await ownerSelect.locator('option').count();
      if (opts > 1) await ownerSelect.selectOption({ index: 1 });
    }

    const createBtn = page.locator('button:has-text("Create Gym")').last();
    await expect(createBtn).toBeEnabled({ timeout: 8000 });
    await createBtn.click();
    await page.waitForTimeout(3000);

    expect(captured.length, 'POST /gyms must fire').toBeGreaterThan(0);
    const post = captured[captured.length - 1];
    expect(
      post.status >= 200 && post.status < 300,
      `POST /gyms returned ${post.status}. body=${JSON.stringify(post.reqBody)} resp=${post.resBody}`,
    ).toBe(true);

    expect(post.reqBody, 'request body must include gymCode').toHaveProperty('gymCode');
    expect((post.reqBody as Record<string, unknown>).ownerId).not.toBe('00000000-0000-0000-0000-000000000000');
  });

  test('POST /admin/gyms/{id}/approve — UI flow on /gyms/onboarding returns 2xx', async ({ page, request }) => {
    // Seed a pending gym so onboarding has at least one row.
    const tokenResp = await request.post('http://localhost:8080/api/v1/identity/login', {
      data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD },
    });
    const token = (await tokenResp.json()).data.accessToken as string;

    const ownersResp = await request.get('http://localhost:8080/api/v1/admin/users?role=owner&page=1&pageSize=20', {
      headers: { Authorization: `Bearer ${token}` },
    });
    const ownersJson = await ownersResp.json();
    const owners = ownersJson?.data?.items ?? ownersJson?.data ?? [];
    if (!owners.length) { test.skip(true, 'No gym owners'); return; }

    const sfx = uniqueSuffix();
    const createResp = await request.post('http://localhost:8080/api/v1/gyms', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        ownerId: owners[0].id,
        gymCode: `APR${sfx}`.slice(0, 10),
        name: `Approve UI ${sfx}`,
        description: null,
        address: { street: 'TBD', city: 'Cairo', state: 'Cairo', country: 'EG', zipCode: '00000' },
        contact: { email: 'owner@example.com', phone: '+20000000000' },
      },
    });
    if (!createResp.ok()) { test.skip(true, `seed failed ${createResp.status()}`); return; }

    const captured = makePostRecorder(page, (u) => /\/admin\/gyms\/[^/]+\/approve$/.test(u));

    await page.goto('/gyms/onboarding');
    await page.waitForTimeout(2500);

    const approveBtn = page.locator('button:has-text("Approve")').first();
    if (!(await approveBtn.isVisible({ timeout: 5000 }).catch(() => false))) {
      test.skip(true, 'No Approve button on onboarding page');
      return;
    }
    await approveBtn.click();
    // confirm modal if shown
    const confirm = page.locator('button:has-text("Approve"), button:has-text("Confirm")').last();
    await confirm.click().catch(() => {});
    await page.waitForTimeout(2500);

    expect(captured.length, 'approve POST must fire').toBeGreaterThan(0);
    const post = captured[captured.length - 1];
    expect(
      post.status >= 200 && post.status < 300,
      `approve returned ${post.status}: ${post.resBody}`,
    ).toBe(true);
  });

  test('POST /admin/gyms/{id}/suspend — direct API on Active gym returns 2xx', async ({ request }) => {
    const t = await request.post('http://localhost:8080/api/v1/identity/login', {
      data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD },
    });
    const token = (await t.json()).data.accessToken as string;

    const list = await request.get('http://localhost:8080/api/v1/gyms?page=1&pageSize=50', {
      headers: { Authorization: `Bearer ${token}` },
    });
    const lj = await list.json();
    const items = lj?.data?.items ?? lj?.data ?? [];
    const activeGym = items.find((g: { status: string }) => String(g.status).toLowerCase() === 'active');
    if (!activeGym) { test.skip(true, 'No active gym available'); return; }

    const r = await request.post(`http://localhost:8080/api/v1/admin/gyms/${activeGym.id}/suspend`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { reason: 'E2E suspend' },
    });
    expect(r.status(), `body=${await r.text()}`).toBeGreaterThanOrEqual(200);
    expect(r.status()).toBeLessThan(300);

    // restore: reactivate the gym we just suspended so other tests are not affected
    await request.post(`http://localhost:8080/api/v1/admin/gyms/${activeGym.id}/reactivate`, {
      headers: { Authorization: `Bearer ${token}` },
      data: {},
    });
  });

  test('POST /admin/gyms/{id}/reactivate — direct API after suspend returns 2xx', async ({ request }) => {
    const t = await request.post('http://localhost:8080/api/v1/identity/login', {
      data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD },
    });
    const token = (await t.json()).data.accessToken as string;

    const list = await request.get('http://localhost:8080/api/v1/gyms?page=1&pageSize=50', {
      headers: { Authorization: `Bearer ${token}` },
    });
    const lj = await list.json();
    const items = lj?.data?.items ?? lj?.data ?? [];
    const activeGym = items.find((g: { status: string }) => String(g.status).toLowerCase() === 'active');
    if (!activeGym) { test.skip(true, 'No active gym available'); return; }

    // suspend first
    const sus = await request.post(`http://localhost:8080/api/v1/admin/gyms/${activeGym.id}/suspend`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { reason: 'pre-reactivate' },
    });
    if (!sus.ok()) { test.skip(true, `pre-suspend failed ${sus.status()}`); return; }

    // Allow status update to settle (outbox / event propagation)
    await new Promise((res) => setTimeout(res, 500));

    const r = await request.post(`http://localhost:8080/api/v1/admin/gyms/${activeGym.id}/reactivate`, {
      headers: { Authorization: `Bearer ${token}` },
      data: {},
    });
    expect(r.status(), `body=${await r.text()}`).toBeGreaterThanOrEqual(200);
    expect(r.status()).toBeLessThan(300);
  });
});

test.describe('Admin POST — Plans & Features', () => {
  test.beforeEach(async ({ page }) => loginIfNeeded(page));

  test('POST /admin/plans — create plan returns 2xx', async ({ page }) => {
    const captured = makePostRecorder(page, (u) => /\/api\/v1\/admin\/plans(\?|$)/.test(u));

    await page.goto('/plans');
    await page.waitForTimeout(2500);

    await page.locator('button:has-text("Create Plan")').first().click();
    await page.waitForTimeout(800);

    const sfx = uniqueSuffix();
    await page.locator('label:has-text("Plan Name")').first().locator('~ input, + input').fill(`E2E Plan ${sfx}`);
    await page.locator('label:has-text("Plan Code")').first().locator('~ input, + input').fill(`EPLN${sfx}`.slice(0, 10).toUpperCase());

    // Best-effort fill numeric prices if visible
    const priceInputs = page.locator('input[type="number"]');
    const priceCount = await priceInputs.count();
    for (let i = 0; i < priceCount; i++) {
      const cur = await priceInputs.nth(i).inputValue();
      if (!cur) await priceInputs.nth(i).fill(String(100 + i));
    }

    const submit = page.locator('button:has-text("Create Plan")').last();
    await expect(submit).toBeEnabled({ timeout: 5000 });
    await submit.click();
    await page.waitForTimeout(2500);

    expect(captured.length, 'POST /admin/plans must fire').toBeGreaterThan(0);
    const post = captured[captured.length - 1];
    expect(
      post.status >= 200 && post.status < 300,
      `create plan returned ${post.status}. body=${JSON.stringify(post.reqBody)} resp=${post.resBody}`,
    ).toBe(true);
  });

  test('POST /admin/features — create feature returns 2xx', async ({ page }) => {
    const captured = makePostRecorder(page, (u) => /\/api\/v1\/admin\/features(\?|$)/.test(u));

    await page.goto('/plans');
    await page.waitForTimeout(2500);

    const addFeature = page.locator('button:has-text("Add Feature"), button:has-text("Create Feature"), button:has-text("New Feature")').first();
    if (!(await addFeature.isVisible({ timeout: 5000 }).catch(() => false))) {
      test.skip(true, 'No Add Feature trigger visible');
      return;
    }
    await addFeature.click();
    await page.waitForTimeout(700);

    const sfx = uniqueSuffix();
    const nameInput = page.locator('label:has-text("Feature Name"), label:has-text("Name")').first().locator('~ input, + input');
    const codeInput = page.locator('label:has-text("Feature Code")').first().locator('~ input, + input');
    if (!(await nameInput.isVisible().catch(() => false)) || !(await codeInput.isVisible().catch(() => false))) {
      test.skip(true, 'Feature form fields not visible');
      return;
    }
    await nameInput.fill(`E2E Feature ${sfx}`);
    await codeInput.fill(`E2EF${sfx}`.slice(0, 12).toUpperCase());

    const submit = page.locator('button:has-text("Create"), button:has-text("Save")').last();
    await submit.click();
    await page.waitForTimeout(2500);

    expect(captured.length, 'POST /admin/features must fire').toBeGreaterThan(0);
    const post = captured[captured.length - 1];
    expect(
      post.status >= 200 && post.status < 300,
      `create feature returned ${post.status}. body=${JSON.stringify(post.reqBody)} resp=${post.resBody}`,
    ).toBe(true);
  });

  test('POST /admin/deploy/{scope}/{id} — deploy after toggle returns 2xx', async ({ page }) => {
    const captured = makePostRecorder(page, (u) => /\/api\/v1\/admin\/deploy\//.test(u));

    await page.goto('/plans');
    await page.waitForTimeout(3000);

    const matrixToggle = page.locator('table button[aria-label*="Toggle"]').first();
    if (!(await matrixToggle.isVisible({ timeout: 5000 }).catch(() => false))) {
      test.skip(true, 'Feature matrix toggle not visible');
      return;
    }
    await matrixToggle.click();
    await page.waitForTimeout(1500);

    const deployBtn = page.locator('button:has-text("Deploy")').first();
    if (!(await deployBtn.isVisible({ timeout: 4000 }).catch(() => false))) {
      test.skip(true, 'Deploy banner not appearing after toggle');
      return;
    }
    await deployBtn.click();
    await page.waitForTimeout(800);
    await page.locator('button:has-text("Deploy Now")').last().click();
    await page.waitForTimeout(3000);

    expect(captured.length, 'POST /admin/deploy must fire').toBeGreaterThan(0);
    const post = captured[captured.length - 1];
    expect(
      post.status >= 200 && post.status < 300,
      `deploy returned ${post.status}. body=${JSON.stringify(post.reqBody)} resp=${post.resBody}`,
    ).toBe(true);
  });
});

test.describe('Admin POST — Identity & Settings', () => {
  test.beforeEach(async ({ page }) => loginIfNeeded(page));

  test('POST /admin/users — add admin returns 2xx', async ({ page }) => {
    const captured = makePostRecorder(page, (u) => /\/api\/v1\/admin\/users(\?|$)/.test(u));

    await page.goto('/settings');
    await page.waitForTimeout(2000);

    const adminTeamTab = page.locator('button:has-text("Admin Team")');
    if (await adminTeamTab.isVisible({ timeout: 4000 }).catch(() => false)) {
      await adminTeamTab.click();
      await page.waitForTimeout(800);
    }

    const addAdmin = page.locator('button:has-text("Add Admin")').first();
    if (!(await addAdmin.isVisible({ timeout: 4000 }).catch(() => false))) {
      test.skip(true, 'Add Admin button not visible');
      return;
    }
    await addAdmin.click();
    await page.waitForTimeout(600);

    const sfx = uniqueSuffix();
    await page.locator('label:has-text("First Name")').first().locator('~ input, + input').fill('E2E');
    await page.locator('label:has-text("Last Name")').first().locator('~ input, + input').fill('Admin');
    await page.locator('label:has-text("Email")').first().locator('~ input, + input').fill(`e2e+${sfx}@admin.local`);

    const submit = page.locator('button:has-text("Add Admin"), button:has-text("Save"), button:has-text("Create")').last();
    await expect(submit).toBeEnabled({ timeout: 4000 });
    await submit.click();
    await page.waitForTimeout(2500);

    expect(captured.length, 'POST /admin/users must fire').toBeGreaterThan(0);
    const post = captured[captured.length - 1];
    expect(
      post.status >= 200 && post.status < 300,
      `add admin returned ${post.status}. body=${JSON.stringify(post.reqBody)} resp=${post.resBody}`,
    ).toBe(true);
  });
});

test.describe('Admin POST — Direct API contract checks', () => {
  // These hit the BE directly with the admin JWT to validate raw POST contracts
  // independently of UI quirks. Catches BE regressions even when UI works.

  let token = '';

  test.beforeAll(async ({ request }) => {
    const r = await request.post('http://localhost:8080/api/v1/identity/login', {
      data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD },
    });
    expect(r.ok(), `admin login failed: ${r.status()} ${await r.text()}`).toBe(true);
    token = (await r.json()).data.accessToken as string;
  });

  test('POST /api/v1/gyms with missing required fields returns 400', async ({ request }) => {
    const r = await request.post('http://localhost:8080/api/v1/gyms', {
      headers: { Authorization: `Bearer ${token}` },
      data: { name: '' },
    });
    expect([400, 422]).toContain(r.status());
  });

  test('POST /api/v1/admin/plans with valid payload returns 2xx', async ({ request }) => {
    const sfx = uniqueSuffix();
    const r = await request.post('http://localhost:8080/api/v1/admin/plans', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        code: `API${sfx}`.slice(0, 10).toUpperCase(),
        name: `API Plan ${sfx}`,
        description: 'created by direct API e2e',
        monthlyPrice: 100,
        annualPrice: 1000,
        memberLimit: 100,
        trainerLimit: 10,
        staffLimit: 10,
        isPublic: false,
        sortOrder: 999,
      },
    });
    expect(r.status(), `body=${await r.text()}`).toBeGreaterThanOrEqual(200);
    expect(r.status()).toBeLessThan(300);
  });

  test('POST /api/v1/admin/features with valid payload returns 2xx', async ({ request }) => {
    const sfx = uniqueSuffix();
    const r = await request.post('http://localhost:8080/api/v1/admin/features', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        code: `APIF${sfx}`.slice(0, 12).toUpperCase(),
        name: `API Feature ${sfx}`,
        description: null,
        category: 'Training',
      },
    });
    expect(r.status(), `body=${await r.text()}`).toBeGreaterThanOrEqual(200);
    expect(r.status()).toBeLessThan(300);
  });

  test('POST /api/v1/admin/users with valid payload returns 2xx', async ({ request }) => {
    const sfx = uniqueSuffix();
    const r = await request.post('http://localhost:8080/api/v1/admin/users', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        firstName: 'Api',
        lastName: 'Admin',
        email: `api+${sfx}@admin.local`,
        adminRole: 'support',
      },
    });
    expect(r.status(), `body=${await r.text()}`).toBeGreaterThanOrEqual(200);
    expect(r.status()).toBeLessThan(300);
  });

  test('POST /api/v1/admin/deploy/all returns 2xx', async ({ request }) => {
    const r = await request.post('http://localhost:8080/api/v1/admin/deploy/all', {
      headers: { Authorization: `Bearer ${token}` },
      data: { deployedBy: '00000000-0000-0000-0000-000000000000' },
    });
    expect(r.status(), `body=${await r.text()}`).toBeGreaterThanOrEqual(200);
    expect(r.status()).toBeLessThan(300);
  });
});
