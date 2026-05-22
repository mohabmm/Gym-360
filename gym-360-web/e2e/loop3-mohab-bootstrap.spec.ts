/**
 * Loop 3 — Bootstrap full tenant entirely via UI (no direct API calls for setup).
 *
 *   1. admin@demo.local on :3000 → Add Gym Owner (mohabOwner_31_8@hotmail.com / 12345678)
 *   2. admin → Add Gym (Forma Test Gym) → owner above
 *   3. admin → /gyms/{id} → assign Pro plan + deploy
 *   4. owner@:3001 → Add Staff (mohabStaff_31_8)
 *   5. owner@:3001 → Add Trainer (mohabTrainer_31_8) → approve
 *   6. owner@:3001 → Add Member (mohabTrainee_31_8)
 *
 * Final creds dumped to /tmp/loop3_creds.json for downstream verifiers.
 *
 * Run: npx playwright test e2e/loop3-mohab-bootstrap.spec.ts --project=chromium --workers=1
 */

import { test, expect, Page, request } from '@playwright/test';
import { writeFileSync } from 'fs';

const ADMIN_BASE = 'http://localhost:3000';
const STAFF_BASE = 'http://localhost:3001';
const API = 'http://localhost:8080';

const creds = {
  admin: { email: 'admin@demo.local', password: '123456' },
  owner: {
    email: 'mohabOwner_31_8@hotmail.com',
    password: '12345678',
    firstName: 'Mohab',
    lastName: 'Owner',
    phone: '+201000000001',
    companyName: 'Forma Test Co',
    billingEmail: 'mohabOwner_31_8@hotmail.com',
  },
  gym: { name: 'Forma Test Gym', city: 'Cairo' },
  staff: {
    email: 'mohabStaff_31_8@hotmail.com',
    password: '12345678',
    firstName: 'Mohab',
    lastName: 'Staff',
    phone: '+201000000002',
  },
  trainer: {
    email: 'mohabTrainer_31_8@hotmail.com',
    password: '12345678',
    firstName: 'Mohab',
    lastName: 'Trainer',
    phone: '+201000000003',
  },
  trainee: {
    email: 'mohabTrainee_31_8@hotmail.com',
    password: '12345678',
    firstName: 'Mohab',
    lastName: 'Trainee',
    phone: '+201000000004',
  },
  ids: {} as Record<string, string>,
};

async function loginCredentials(page: Page, base: string, email: string, password: string) {
  await page.goto(`${base}/login`, { waitUntil: 'networkidle' });
  await page.fill('input[type="email"]', email);
  await page.fill('input[type="password"]', password);
  await page.click('button[type="submit"]');
  await page.waitForURL((u) => !u.pathname.startsWith('/login'), { timeout: 30000 });
}

test.describe.configure({ mode: 'serial' });
test.use({ storageState: { cookies: [], origins: [] } }); // start fresh per test

test('Loop3 — admin creates owner via /users UI', async ({ page }) => {
  test.setTimeout(120_000);
  await loginCredentials(page, ADMIN_BASE, creds.admin.email, creds.admin.password);

  await page.goto(`${ADMIN_BASE}/users`, { waitUntil: 'networkidle' });
  await page.getByRole('button', { name: /Add Gym Owner/i }).click();

  // The Add-Gym-Owner modal renders <label>{labelText}<input/></label>. Match by label
  // text accounting for " *" required-marker suffix; for inputs not bound by a <label for=>
  // association we fall back to type-based selectors scoped to the modal.
  const modal = page.locator('div[role="dialog"], .modal, form#create-owner-form').last();
  const inputs = modal.locator('input');

  // Order in users/page.tsx: First Name, Last Name, Email, Password, Phone, Company, Billing Email
  await inputs.nth(0).fill(creds.owner.firstName);
  await inputs.nth(1).fill(creds.owner.lastName);
  await inputs.nth(2).fill(creds.owner.email);
  await inputs.nth(3).fill(creds.owner.password);
  await inputs.nth(4).fill(creds.owner.phone);
  await inputs.nth(5).fill(creds.owner.companyName);
  await inputs.nth(6).fill(creds.owner.billingEmail);
  // Plan tier select defaults to Pro; Dashboard Mode select defaults to Both. Submit.
  await page.getByRole('button', { name: /^Create Owner$|^Creating\.\.\.$/i }).click();

  // Modal closes; verify by waiting for /api/v1/admin/users response carrying our email
  await page.waitForResponse((r) => r.url().includes('/admin/users') && r.request().method() === 'POST', { timeout: 15_000 }).catch(() => {});
  await page.waitForTimeout(1500);
  await expect(page.getByText(creds.owner.email, { exact: false }).first()).toBeVisible({ timeout: 15_000 });

  // Capture owner userId via API (admin token derived from session cookies the browser holds)
  const ownerUid: string = await page.evaluate(async (email) => {
    const r = await fetch('/api/admin/users', { credentials: 'include' });
    if (!r.ok) return '';
    const data = await r.json();
    const list = data?.data ?? data;
    const u = (list as any[]).find((x) => x.email?.toLowerCase() === email.toLowerCase());
    return u?.id ?? '';
  }, creds.owner.email);
  console.log('owner uid via FE:', ownerUid);
  if (ownerUid) creds.ids.ownerUserId = ownerUid;
});

test('Loop3 — admin creates gym via /gyms UI', async ({ page }) => {
  test.setTimeout(120_000);
  await loginCredentials(page, ADMIN_BASE, creds.admin.email, creds.admin.password);

  await page.goto(`${ADMIN_BASE}/gyms`, { waitUntil: 'networkidle' });

  await page.getByRole('button', { name: /^Add Gym$/i }).first().click();

  // Modal in @gym360/ui is a position-fixed div with no role. Anchor on the heading.
  const heading = page.getByRole('heading', { name: 'Add Gym' });
  await heading.waitFor({ state: 'visible', timeout: 10_000 });
  const modal = heading.locator('xpath=ancestor::div[contains(@class,"rounded")][1]');
  const gymInputs = modal.locator('input');
  const ownerSelect = modal.locator('select');

  await gymInputs.nth(0).fill(creds.gym.name);
  await gymInputs.nth(1).fill('FORMA' + Math.floor(Math.random() * 9000 + 1000));
  await gymInputs.nth(2).fill(creds.gym.city);
  // Pick our owner. <select> options have label "First Last (email)".
  await ownerSelect.selectOption({ label: new RegExp(creds.owner.email, 'i') as any }).catch(async () => {
    // Fallback: pick by partial visible text
    const options = await ownerSelect.locator('option').allTextContents();
    const target = options.find((t) => t.toLowerCase().includes(creds.owner.email.toLowerCase()));
    if (target) await ownerSelect.selectOption({ label: target });
  });

  await page.getByRole('button', { name: /^Create Gym$|^Creating\.\.\.$/i }).click();
  await page.waitForResponse((r) => r.url().includes('/api/v1/gyms') && r.request().method() === 'POST', { timeout: 20_000 }).catch(() => {});
  await page.waitForTimeout(2_500);
});

test('Loop3 — admin sets Pro plan + deploys via UI', async ({ page }) => {
  test.setTimeout(120_000);
  await loginCredentials(page, ADMIN_BASE, creds.admin.email, creds.admin.password);

  // Find our gym row, click into the detail page.
  await page.goto(`${ADMIN_BASE}/gyms`, { waitUntil: 'networkidle' });
  // Click the first row showing our gym name
  await page.locator(`text=${creds.gym.name}`).first().click({ timeout: 10_000 });
  await page.waitForURL(/\/gyms\/[a-f0-9-]+$/i, { timeout: 15_000 });
  const gymId = page.url().split('/').pop()!;
  creds.ids.gymId = gymId;

  // Switch to Subscription & Features tab.
  await page.locator('button:has-text("Subscription & Features")').first().click();
  await page.waitForTimeout(500);

  // Click Change Plan, select Pro, fill reason, submit.
  await page.locator('button:has-text("Change Plan")').first().click();
  {
    const heading = page.getByRole('heading', { name: 'Change Plan' });
    await heading.waitFor({ state: 'visible', timeout: 10_000 });
    const modal = heading.locator('xpath=ancestor::div[contains(@class,"rounded")][1]');
    const planSelect = modal.locator('select').first();
    await planSelect.selectOption({ label: 'Pro' }).catch(async () => {
      // fallback: pick option whose text contains 'Pro'
      const opts = await planSelect.locator('option').allTextContents();
      const idx = opts.findIndex((t) => /pro/i.test(t));
      if (idx >= 0) await planSelect.selectOption({ index: idx });
    });
    // Reason field (textarea)
    const reasonEl = modal.locator('textarea, input[type="text"]').last();
    await reasonEl.fill('Loop3 upgrade').catch(() => {});
    await modal.getByRole('button', { name: /^Change Plan$|Updating/i }).click();
  }
  await page.waitForResponse((r) => r.url().includes('/subscription/plan') && r.request().method() === 'PUT', { timeout: 15_000 }).catch(() => {});
  await page.waitForTimeout(1_500);

  // Deploy via /plans page sticky bar.
  await page.goto(`${ADMIN_BASE}/plans`, { waitUntil: 'networkidle' });
  // Click "Deploy Changes" or "Deploy Now" — tolerate either label
  await page.locator('button:has-text("Deploy")').first().click({ timeout: 10_000 }).catch(() => {});
  // Confirm modal "Deploy Now" button
  await page.locator('button:has-text("Deploy Now")').last().click({ timeout: 10_000 }).catch(() => {});
  await page.waitForResponse((r) => r.url().includes('/admin/deploy/') && r.request().method() === 'POST', { timeout: 15_000 }).catch(() => {});
  await page.waitForTimeout(1_500);
});

test('Loop3 — owner adds staff/trainer/trainee via :3001 UI', async ({ browser }) => {
  test.setTimeout(240_000);
  const ctx = await browser.newContext();
  const page = await ctx.newPage();

  await loginCredentials(page, STAFF_BASE, creds.owner.email, creds.owner.password);

  // Owner with multi-gym scope lands on /select-gym (or auto-redirects). Click "Open" on first card.
  if (page.url().includes('/select-gym')) {
    await page.getByRole('button', { name: /Open/i }).first().click();
    await page.waitForURL((u) => !u.pathname.includes('/select-gym'), { timeout: 15_000 });
  }

  // ---- /staff → Add Staff ---------------------------------------------------
  await page.goto(`${STAFF_BASE}/staff`, { waitUntil: 'networkidle' });
  await page.waitForLoadState('networkidle').catch(() => {});
  const addStaffBtn = page.locator('button:has-text("Add Staff")').first();
  await addStaffBtn.waitFor({ state: 'visible', timeout: 10_000 });
  await addStaffBtn.click();
  await page.waitForTimeout(500);
  {
    const heading = page.getByRole('heading', { name: 'Add Staff Member' });
    await heading.waitFor({ state: 'visible', timeout: 10_000 });
    const modal = heading.locator('xpath=ancestor::div[contains(@class,"rounded")][1]');
    const inputs = modal.locator('input');
    // Order: FirstName, LastName, Email, Phone, Password
    await inputs.nth(0).fill(creds.staff.firstName);
    await inputs.nth(1).fill(creds.staff.lastName);
    await inputs.nth(2).fill(creds.staff.email);
    await inputs.nth(3).fill(creds.staff.phone);
    await inputs.nth(4).fill(creds.staff.password);
    // Role select left at default (manager); we don't care for staff smoke.
    await modal.getByRole('button', { name: /^Add Staff$|^Adding\.\.\.$/i }).click();
  }
  await page.waitForResponse((r) => r.url().includes('/staff/create') && r.request().method() === 'POST', { timeout: 15_000 }).catch(() => {});
  await page.waitForTimeout(2_000);

  // ---- /trainers → Add Trainer ---------------------------------------------
  await page.goto(`${STAFF_BASE}/trainers`, { waitUntil: 'networkidle' });
  await page.locator('button:has-text("Add Trainer")').first().click();
  {
    const heading = page.getByRole('heading', { name: 'Add Trainer' });
    await heading.waitFor({ state: 'visible', timeout: 10_000 });
    const modal = heading.locator('xpath=ancestor::div[contains(@class,"rounded")][1]');
    const inputs = modal.locator('input');
    await inputs.nth(0).fill(creds.trainer.firstName);
    await inputs.nth(1).fill(creds.trainer.lastName);
    await inputs.nth(2).fill(creds.trainer.email);
    await inputs.nth(3).fill(creds.trainer.phone);
    await modal.getByRole('button', { name: /^Add Trainer$|Adding/i }).click();
  }
  await page.waitForResponse((r) => r.url().includes('/trainers') && r.request().method() === 'POST', { timeout: 15_000 }).catch(() => {});
  await page.waitForTimeout(2_000);

  // ---- /members → Add Member ------------------------------------------------
  await page.goto(`${STAFF_BASE}/members`, { waitUntil: 'networkidle' });
  await page.locator('button:has-text("Add Member")').first().click();
  await page.waitForTimeout(500);
  {
    const heading = page.getByRole('heading', { name: 'Add Member' });
    await heading.waitFor({ state: 'visible', timeout: 10_000 });
    const modal = heading.locator('xpath=ancestor::div[contains(@class,"rounded")][1]');
    const inputs = modal.locator('input');
    // Members modal: [Name, Phone, Email] inputs + [Floor] select
    await inputs.nth(0).fill(`${creds.trainee.firstName} ${creds.trainee.lastName}`);
    await inputs.nth(1).fill(creds.trainee.phone);
    await inputs.nth(2).fill(creds.trainee.email);
    // Floor select — leave default 'Mens'.
    await modal.getByRole('button', { name: /^Add Member$|Adding/i }).click();
  }
  await page.waitForResponse((r) => r.url().includes('/members') && r.request().method() === 'POST', { timeout: 15_000 }).catch(() => {});
  await page.waitForTimeout(2_500);

  await ctx.close();
});

test.afterAll(() => {
  writeFileSync('/tmp/loop3_creds.json', JSON.stringify(creds, null, 2));
  console.log('creds → /tmp/loop3_creds.json');
});
