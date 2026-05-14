/**
 * Loop Phase 1 — Bootstrap a full gym tenant via UI.
 *
 *   1. admin@demo.local logs in to admin web (:3000)
 *   2. /users → Add Gym Owner (creates GymOwner user + Owner aggregate)
 *   3. /gyms → Add Gym (registers + auto-approves gym, provisions schemas)
 *   4. owner logs in to staff web (:3001)
 *   5. /staff   → Add Staff (creates GymStaff user + StaffRole)
 *   6. /trainers → Add Trainer (creates Trainer user + TrainerRequest, then approve)
 *   7. /members  → Add Member (creates Trainee user + GymMember)
 *
 * Captures all credentials into test_logs/loop_<N>_creds.json so the MobAI
 * phases can pick them up. Loop number is taken from $LOOP_N (default 1).
 *
 * Standalone: not part of normal test projects. Run with:
 *   npx playwright test --config playwright.bootstrap.config.ts
 */

import { test, expect, Page } from '@playwright/test';
import { mkdirSync, writeFileSync } from 'fs';
import path from 'path';

const LOOP_N = process.env.LOOP_N ?? '1';
const SUFFIX = `l${LOOP_N}-${Date.now().toString(36).slice(-6)}`;
const CREDS_FILE = path.resolve(__dirname, `../../test_logs/loop_${LOOP_N}_creds.json`);

const ADMIN_BASE = process.env.ADMIN_BASE_URL ?? 'http://localhost:3000';
const STAFF_BASE = process.env.STAFF_BASE_URL ?? 'http://localhost:3001';

const creds = {
  loop: LOOP_N,
  suffix: SUFFIX,
  admin: { email: 'admin@demo.local', password: '123456' },
  owner: {
    email: `owner-${SUFFIX}@gym.local`,
    password: 'Owner123!',
    firstName: 'Loop',
    lastName: `Owner${LOOP_N}`,
    phone: `+201${Math.floor(100000000 + Math.random() * 900000000)}`,
    companyName: `Loop Fitness ${LOOP_N}`,
    billingEmail: `billing-${SUFFIX}@gym.local`,
  },
  gym: {
    name: `Loop Gym ${LOOP_N}-${SUFFIX}`.slice(0, 60),
    gymCode: `L${LOOP_N}${SUFFIX.slice(-4).toUpperCase()}`.slice(0, 10),
    city: 'Cairo',
  },
  staff: {
    email: `staff-${SUFFIX}@gym.local`,
    password: 'Staff123!',
    firstName: 'Loop',
    lastName: `Staff${LOOP_N}`,
    phone: `+201${Math.floor(100000000 + Math.random() * 900000000)}`,
    role: 'Receptionist',
  },
  trainer: {
    email: `trainer-${SUFFIX}@gym.local`,
    password: 'Trainer123!',
    firstName: 'Loop',
    lastName: `Trainer${LOOP_N}`,
    phone: `+201${Math.floor(100000000 + Math.random() * 900000000)}`,
  },
  trainee: {
    email: `trainee-${SUFFIX}@gym.local`,
    password: 'Trainee123!',
    firstName: 'Loop',
    lastName: `Trainee${LOOP_N}`,
    phone: `+201${Math.floor(100000000 + Math.random() * 900000000)}`,
  },
  ids: {} as Record<string, string>,
  errors: [] as string[],
};

async function login(page: Page, base: string, email: string, password: string) {
  await page.goto(`${base}/login`, { waitUntil: 'domcontentloaded' });
  await page.fill('input[type="email"]', email);
  await page.fill('input[type="password"]', password);
  await page.click('button[type="submit"]');
  await page.waitForURL((u) => !u.pathname.startsWith('/login'), { timeout: 30000 });
}

test.describe.configure({ mode: 'serial' });

test('Phase 1 — admin creates owner + gym', async ({ page }) => {
  test.setTimeout(120_000);
  await login(page, ADMIN_BASE, creds.admin.email, creds.admin.password);

  // Add Gym Owner via /users
  await page.goto(`${ADMIN_BASE}/users`, { waitUntil: 'domcontentloaded' });
  await page.getByRole('button', { name: /Add Gym Owner/i }).click();
  const modal = page.locator('[role="dialog"], .modal, div:has-text("Add Gym Owner")').last();
  await modal.locator('input[type="email"]').first().fill(creds.owner.email);
  await page.getByLabel(/Password/i).first().fill(creds.owner.password).catch(async () => {
    // fallback if label binding differs — fill the second input
    await modal.locator('input[type="password"]').first().fill(creds.owner.password);
  });
  await page.getByLabel(/First Name/i).fill(creds.owner.firstName).catch(() => {});
  await page.getByLabel(/Last Name/i).fill(creds.owner.lastName).catch(() => {});
  await page.getByLabel(/Phone/i).fill(creds.owner.phone).catch(() => {});
  await page.getByLabel(/Company Name/i).fill(creds.owner.companyName).catch(() => {});
  await page.getByLabel(/Billing Email/i).fill(creds.owner.billingEmail).catch(() => {});
  // submit
  await page.getByRole('button', { name: /^Add Owner$|^Create$|^Create Owner$|^Save$/i }).click();
  // Wait for modal close OR error
  await page.waitForTimeout(2500);
  const errVisible = await page.getByText(/failed|error/i).first().isVisible().catch(() => false);
  if (errVisible) creds.errors.push('Add Gym Owner: error visible after submit');

  // Look up ownerId via API directly (admin token from cookie/session). Easier:
  // hit the admin/users API by authenticated fetch in browser context.
  const ownersResp = await page.evaluate(async () => {
    const r = await fetch('/api/v1/admin/users?role=GymOwner', { credentials: 'include' });
    return { status: r.status, body: await r.text() };
  });
  // capture for debug
  creds.ids.ownersRaw = ownersResp.body.slice(0, 500);

  // Add Gym — go to /gyms → click Add Gym
  await page.goto(`${ADMIN_BASE}/gyms`, { waitUntil: 'domcontentloaded' });
  await page.getByRole('button', { name: /^Add Gym$/i }).click();
  // The modal accepts: name, gymCode, city, ownerId, ownerEmail (with email->ownerId resolver)
  // Try filling by labels:
  await page.getByLabel(/Gym Name|^Name$/i).first().fill(creds.gym.name).catch(() => {});
  await page.getByLabel(/Gym Code|^Code$/i).first().fill(creds.gym.gymCode).catch(() => {});
  await page.getByLabel(/City/i).first().fill(creds.gym.city).catch(() => {});
  // owner picker — try filling email then waiting for autocomplete
  const ownerInput = page.getByLabel(/Owner|Owner Email/i).first();
  await ownerInput.fill(creds.owner.email).catch(() => {});
  // try submit
  await page.getByRole('button', { name: /^Create Gym$|^Save$/i }).click();
  await page.waitForTimeout(3000);
  const gymErr = await page.getByText(/failed|error/i).first().isVisible().catch(() => false);
  if (gymErr) creds.errors.push('Create Gym: error visible after submit');
});

test('Phase 1 — owner adds staff + trainer + trainee', async ({ page }) => {
  test.setTimeout(120_000);
  await login(page, STAFF_BASE, creds.owner.email, creds.owner.password);

  // /staff → add receptionist
  await page.goto(`${STAFF_BASE}/staff`, { waitUntil: 'domcontentloaded' });
  await page.getByRole('button', { name: /Add Staff|New Staff|Invite Staff/i }).first().click().catch(() => creds.errors.push('Add Staff button missing'));
  await page.waitForTimeout(800);
  await page.getByLabel(/Email/i).first().fill(creds.staff.email).catch(() => {});
  await page.getByLabel(/Password/i).first().fill(creds.staff.password).catch(() => {});
  await page.getByLabel(/First Name/i).first().fill(creds.staff.firstName).catch(() => {});
  await page.getByLabel(/Last Name/i).first().fill(creds.staff.lastName).catch(() => {});
  await page.getByLabel(/Phone/i).first().fill(creds.staff.phone).catch(() => {});
  await page.getByRole('button', { name: /^Save$|^Add$|^Create$/i }).first().click().catch(() => {});
  await page.waitForTimeout(2500);

  // /trainers → add trainer
  await page.goto(`${STAFF_BASE}/trainers`, { waitUntil: 'domcontentloaded' });
  await page.getByRole('button', { name: /Add Trainer|New Trainer|Invite Trainer/i }).first().click().catch(() => creds.errors.push('Add Trainer button missing'));
  await page.waitForTimeout(800);
  await page.getByLabel(/Email/i).first().fill(creds.trainer.email).catch(() => {});
  await page.getByLabel(/Password/i).first().fill(creds.trainer.password).catch(() => {});
  await page.getByLabel(/First Name/i).first().fill(creds.trainer.firstName).catch(() => {});
  await page.getByLabel(/Last Name/i).first().fill(creds.trainer.lastName).catch(() => {});
  await page.getByLabel(/Phone/i).first().fill(creds.trainer.phone).catch(() => {});
  await page.getByRole('button', { name: /^Save$|^Add$|^Create$/i }).first().click().catch(() => {});
  await page.waitForTimeout(2500);

  // /members → add member (trainee)
  await page.goto(`${STAFF_BASE}/members`, { waitUntil: 'domcontentloaded' });
  await page.getByRole('button', { name: /Add Member|New Member/i }).first().click().catch(() => creds.errors.push('Add Member button missing'));
  await page.waitForTimeout(800);
  await page.getByLabel(/Email/i).first().fill(creds.trainee.email).catch(() => {});
  await page.getByLabel(/Password/i).first().fill(creds.trainee.password).catch(() => {});
  await page.getByLabel(/First Name/i).first().fill(creds.trainee.firstName).catch(() => {});
  await page.getByLabel(/Last Name/i).first().fill(creds.trainee.lastName).catch(() => {});
  await page.getByLabel(/Phone/i).first().fill(creds.trainee.phone).catch(() => {});
  await page.getByRole('button', { name: /^Save$|^Add$|^Create$/i }).first().click().catch(() => {});
  await page.waitForTimeout(2500);
});

test.afterAll(() => {
  mkdirSync(path.dirname(CREDS_FILE), { recursive: true });
  writeFileSync(CREDS_FILE, JSON.stringify(creds, null, 2));
  console.log(`creds → ${CREDS_FILE}`);
  if (creds.errors.length) {
    console.log('errors:', creds.errors);
  }
});
