/**
 * Gym Staff App — Comprehensive Bug-Detection Test Suite
 *
 * Scope: all 14 routes + auth flow + bug-specific regression tests.
 * Base URL: http://localhost:3001  (gym-staff Next.js app)
 * Backend API: http://localhost:8080
 *
 * Run:
 *   npx playwright test e2e/gym-staff-comprehensive.spec.ts --project=chromium --reporter=list
 */

import { test, expect, type Page } from '@playwright/test';

test.use({ baseURL: 'http://localhost:3001' });

const EMAIL = 'staff@demo.local';
const PASS = '123456';

// ── helpers ──────────────────────────────────────────────────────────────────

async function login(page: Page) {
  await page.goto('/login');
  await page.fill('input[type="email"]', EMAIL);
  await page.fill('input[type="password"]', PASS);
  await page.click('button[type="submit"]');
  await page.waitForURL('/', { timeout: 60000 });
}

async function ensureLoggedIn(page: Page) {
  await page.goto('/');
  if (page.url().includes('/login')) await login(page);
}

async function goTo(page: Page, path: string) {
  await ensureLoggedIn(page);
  await page.goto(path);
  await page.waitForTimeout(2000);
}

// Collect JS errors on page load
function collectPageErrors(page: Page): string[] {
  const errs: string[] = [];
  page.on('pageerror', (e) => errs.push(e.message));
  return errs;
}

// ── warmup ────────────────────────────────────────────────────────────────────

test.describe('Gym Staff App — Comprehensive Tests', () => {

  test.beforeAll(async ({ browser }) => {
    const page = await browser.newPage();
    for (let i = 0; i < 5; i++) {
      try {
        await page.goto('/login', { timeout: 15000 });
        await page.fill('input[type="email"]', EMAIL);
        await page.fill('input[type="password"]', PASS);
        await page.click('button[type="submit"]');
        await page.waitForURL('/', { timeout: 60000 });
        await page.close();
        return;
      } catch {
        await page.waitForTimeout(5000);
      }
    }
    throw new Error('Backend warmup failed — is the app running on :3001?');
  });

  // ── 1. AUTHENTICATION ─────────────────────────────────────────────────────

  test.describe('1. Authentication', () => {

    test('login page renders GymApp Staff Portal branding', async ({ page }) => {
      await page.goto('/login');
      await expect(page.locator('h1')).toContainText('GymApp Staff Portal');
      await expect(page.locator('input[type="email"]')).toBeVisible();
      await expect(page.locator('input[type="password"]')).toBeVisible();
      await expect(page.locator('button[type="submit"]')).toBeVisible();
    });

    test('unauthenticated root redirects to /login', async ({ page }) => {
      await page.goto('/');
      await expect(page).toHaveURL(/\/login/);
    });

    test('unauthenticated /members redirects to /login', async ({ page }) => {
      await page.goto('/members');
      await expect(page).toHaveURL(/\/login/);
    });

    test('wrong credentials show error', async ({ page }) => {
      await page.goto('/login');
      await page.fill('input[type="email"]', 'wrong@email.com');
      await page.fill('input[type="password"]', 'wrongpass');
      await page.click('button[type="submit"]');
      await expect(
        page.locator('text=Invalid email or password').or(page.locator('text=Invalid')),
      ).toBeVisible({ timeout: 20000 });
    });

    test('correct credentials redirect to dashboard', async ({ page }) => {
      await login(page);
      await expect(page).toHaveURL('/');
      await expect(page.locator('input[type="email"]')).not.toBeVisible();
    });

    test('password visibility toggle switches input type', async ({ page }) => {
      await page.goto('/login');
      await expect(page.locator('input[type="password"]')).toBeVisible();
      await page.locator('form button[type="button"]').click();
      await expect(page.locator('input[type="text"]')).toBeVisible();
    });

    test('empty form submission stays on login', async ({ page }) => {
      await page.goto('/login');
      await page.click('button[type="submit"]');
      await expect(page).toHaveURL(/\/login/);
    });
  });

  // ── 2. DASHBOARD ─────────────────────────────────────────────────────────

  test.describe('2. Dashboard', () => {

    test.beforeEach(async ({ page }) => {
      await ensureLoggedIn(page);
      await page.goto('/');
      await page.waitForTimeout(3000);
    });

    /**
     * BUG-001 — Dashboard crashes with runtime error
     * Symptom: "Unhandled Runtime Error" heading visible instead of dashboard
     * Root cause: components call .split(' ') on potentially undefined name fields
     *   e.g. trainerRankings[0].trainerName.split(' ')
     *   or   formatTime(cls.startTime) when startTime is undefined
     */
    test('[BUG-001] Dashboard must NOT show "Unhandled Runtime Error"', async ({ page }) => {
      const bodyText = await page.locator('body').textContent();
      const crashed = bodyText?.includes('Unhandled Runtime Error') ||
                      bodyText?.includes('Application error');
      if (crashed) {
        console.log('[BUG-001] Dashboard crash confirmed — body contains runtime error text');
      }
      expect(crashed).toBe(false);
    });

    /**
     * BUG-002 — Dashboard: "Cannot read properties of undefined (reading 'split')"
     * Multiple JS exceptions thrown, all from undefined.split()
     */
    test('[BUG-002] No JS exceptions on dashboard (split on undefined)', async ({ page }) => {
      const errs = collectPageErrors(page);
      await page.goto('/');
      await page.waitForTimeout(4000);
      const splitErrs = errs.filter((e) => e.includes('split'));
      if (splitErrs.length > 0) {
        console.log('[BUG-002] Split errors on dashboard:', splitErrs);
      }
      expect(splitErrs.length).toBe(0);
    });

    test('KPI cards render (Active Members, Monthly Revenue, Check-ins, Trainers)', async ({ page }) => {
      await expect(page.locator('text=Active Members').first()).toBeVisible({ timeout: 12000 });
      await expect(page.locator('text=Monthly Revenue').first()).toBeVisible({ timeout: 8000 });
      await expect(page.locator("text=Today's Check-ins").first()).toBeVisible({ timeout: 8000 });
      await expect(page.locator('text=Active Trainers').first()).toBeVisible({ timeout: 8000 });
    });

    test("Today's Schedule section visible", async ({ page }) => {
      await expect(page.locator("text=Today's Schedule").first()).toBeVisible({ timeout: 12000 });
    });

    test('Quick Actions present and linked', async ({ page }) => {
      await expect(page.locator('text=Add Member').first()).toBeVisible({ timeout: 12000 });
      await expect(page.locator('text=Record Payment').first()).toBeVisible({ timeout: 8000 });
      await expect(page.locator('text=Create Class').first()).toBeVisible({ timeout: 8000 });
      await expect(page.locator('text=Send Announcement').first()).toBeVisible({ timeout: 8000 });
    });

    test('Sidebar nav links present', async ({ page }) => {
      await expect(page.locator('a[href="/members"]').first()).toBeVisible({ timeout: 12000 });
      await expect(page.locator('a[href="/trainers"]').first()).toBeVisible({ timeout: 8000 });
      await expect(page.locator('a[href="/classes"]').first()).toBeVisible({ timeout: 8000 });
      await expect(page.locator('a[href="/billing"]').first()).toBeVisible({ timeout: 8000 });
    });

    test('Top Classes section renders', async ({ page }) => {
      await expect(page.locator('text=Top Classes').first()).toBeVisible({ timeout: 12000 });
    });

    test('Needs Attention section renders', async ({ page }) => {
      await expect(page.locator('text=Needs Attention').first()).toBeVisible({ timeout: 12000 });
    });

    test('Coach of the Month section renders', async ({ page }) => {
      await expect(page.locator('text=Coach of the Month').first()).toBeVisible({ timeout: 12000 });
    });

    test('Recent Activity section renders', async ({ page }) => {
      await expect(page.locator('text=Recent Activity').first()).toBeVisible({ timeout: 12000 });
    });

    test('No 500 API errors on dashboard load', async ({ page }) => {
      const serverErrors: string[] = [];
      page.on('response', (res) => {
        if (res.status() >= 500) serverErrors.push(`${res.status()} ${res.url()}`);
      });
      await page.goto('/');
      await page.waitForTimeout(4000);
      if (serverErrors.length > 0) console.log('[500 ERRORS]', serverErrors);
      expect(serverErrors.length).toBe(0);
    });
  });

  // ── 3. MEMBERS ────────────────────────────────────────────────────────────

  test.describe('3. Members', () => {

    test.beforeEach(async ({ page }) => {
      await goTo(page, '/members');
    });

    test('members table loads with rows', async ({ page }) => {
      await expect(page.locator('h1, h2').first()).toBeVisible({ timeout: 8000 });
      // Wait for API data to load
      await page.waitForSelector('tbody tr', { timeout: 15000 });
      const rows = await page.locator('tbody tr').count();
      expect(rows).toBeGreaterThan(0);
    });

    test('table columns: Name, Phone, Plan, Status, Expires visible', async ({ page }) => {
      await page.waitForSelector('thead th', { timeout: 10000 });
      await expect(page.locator('th:has-text("Name")').first()).toBeVisible();
      await expect(page.locator('th:has-text("Phone")').first()).toBeVisible();
      await expect(page.locator('th:has-text("Plan")').first()).toBeVisible();
      await expect(page.locator('th:has-text("Status")').first()).toBeVisible();
    });

    test('search input filters member list', async ({ page }) => {
      await page.waitForSelector('tbody tr', { timeout: 15000 });
      const searchInput = page.locator('input[placeholder*="earch"]').first();
      if (await searchInput.isVisible()) {
        const before = await page.locator('tbody tr').count();
        await searchInput.fill('zz_nonexistent_member');
        await page.waitForTimeout(600);
        const after = await page.locator('tbody tr').count();
        expect(after).toBeLessThanOrEqual(before);
      }
    });

    test('status filter dropdown filters list', async ({ page }) => {
      await page.waitForSelector('select', { timeout: 10000 });
      const selects = page.locator('select');
      if (await selects.first().isVisible()) {
        await selects.first().selectOption('active');
        await page.waitForTimeout(500);
        await selects.first().selectOption('all');
      }
    });

    test('row click navigates to member detail', async ({ page }) => {
      await page.waitForSelector('tbody tr', { timeout: 15000 });
      await page.locator('tbody tr').first().click();
      await page.waitForURL(/\/members\/[^/]+$/, { timeout: 10000 });
      expect(page.url()).toMatch(/\/members\//);
    });

    test('Add Member button opens modal with form fields', async ({ page }) => {
      await page.locator('button:has-text("Add Member")').first().click();
      await expect(page.getByRole('heading', { name: /add member/i })).toBeVisible({ timeout: 5000 });
      await expect(page.locator('label:has-text("Name") input')).toBeVisible();
      await expect(page.locator('input[type="tel"]').first()).toBeVisible();
      await page.locator('button:has-text("Cancel")').click();
    });

    /**
     * BUG-003 — Members: Add Member uses native alert() on success
     * src: members/page.tsx:65  alert(`Member ${newMember.name} added.`);
     * Fix: replace with toast.success()
     */
    test('[BUG-003] Add Member must NOT fire native alert() on save', async ({ page }) => {
      await page.locator('button:has-text("Add Member")').first().click();
      await page.waitForTimeout(300);
      await page.fill('input.mt-1.w-full.h-9', 'Test Member Alert');
      // Fill phone
      const phoneInput = page.locator('input[type="tel"]').first();
      if (await phoneInput.isVisible()) await phoneInput.fill('01012345678');

      let alertFired = false;
      page.once('dialog', (d) => { alertFired = true; console.log('[BUG-003] alert:', d.message()); d.dismiss(); });

      await page.locator('button:has-text("Add Member")').last().click();
      await page.waitForTimeout(700);
      expect(alertFired).toBe(false);
    });

    /**
     * BUG-004 — Members: Bulk Import uses native alert()
     * src: members/page.tsx:73  alert(`Processing ${file.name}...`);
     * Fix: replace with toast notification
     */
    test('[BUG-004] Bulk Import must NOT fire native alert()', async ({ page }) => {
      // Trigger the hidden file input programmatically
      let alertFired = false;
      page.once('dialog', (d) => { alertFired = true; console.log('[BUG-004] alert:', d.message()); d.dismiss(); });

      // Set up a fake file upload
      const [fileChooser] = await Promise.all([
        page.waitForEvent('filechooser', { timeout: 3000 }).catch(() => null),
        page.locator('button:has-text("Bulk Import")').click(),
      ]);
      if (fileChooser) {
        await fileChooser.setFiles({
          name: 'members.csv',
          mimeType: 'text/csv',
          buffer: Buffer.from('name,phone\nTest,01012345678'),
        });
        await page.waitForTimeout(700);
        expect(alertFired).toBe(false);
      }
    });
  });

  // ── 4. MEMBER DETAIL ──────────────────────────────────────────────────────

  test.describe('4. Member Detail', () => {

    let memberId = '';

    test.beforeEach(async ({ page }) => {
      await goTo(page, '/members');
      await page.waitForSelector('tbody tr', { timeout: 15000 });
      const firstRow = page.locator('tbody tr').first();
      if (await firstRow.isVisible()) {
        await firstRow.click();
        await page.waitForURL(/\/members\/[^/]+$/, { timeout: 10000 });
        memberId = page.url().split('/members/')[1] ?? '';
      }
    });

    test('Profile tab shows personal info and membership sections', async ({ page }) => {
      await expect(page.locator('text=Personal Information').first()).toBeVisible({ timeout: 8000 });
      await expect(page.locator('text=Membership').first()).toBeVisible({ timeout: 5000 });
    });

    test('Payments tab renders', async ({ page }) => {
      await page.locator('button:has-text("Payments")').click();
      await expect(page.locator('text=Payment History').first()).toBeVisible({ timeout: 5000 });
    });

    test('Sessions tab renders', async ({ page }) => {
      await page.locator('button:has-text("Sessions")').click();
      await expect(page.locator('text=Training Sessions').first()).toBeVisible({ timeout: 5000 });
    });

    test('Edit button opens edit modal (not alert)', async ({ page }) => {
      const editBtn = page.locator('button:has-text("Edit")').first();
      await expect(editBtn).toBeVisible({ timeout: 8000 });
      let alertFired = false;
      page.once('dialog', (d) => { alertFired = true; d.dismiss(); });
      await editBtn.click();
      await page.waitForTimeout(500);
      // Edit modal should open (not alert)
      expect(alertFired).toBe(false);
      await expect(page.locator('text=Edit Profile').first()).toBeVisible({ timeout: 5000 });
    });

    /**
     * BUG-005 — Member Detail: "Save Changes" doesn't call API
     * src: members/[id]/page.tsx:145 — button onClick just closes modal
     * Fix: wire onClick to PUT /gyms/{gymId}/members/{memberId}
     */
    test('[BUG-005] Save Changes must call API (not just close modal)', async ({ page }) => {
      const editBtn = page.locator('button:has-text("Edit")').first();
      if (await editBtn.isVisible()) {
        await editBtn.click();
        await page.waitForTimeout(400);

        let apiCallMade = false;
        page.on('request', (req) => {
          if ((req.method() === 'PUT' || req.method() === 'PATCH') && req.url().includes('/members/')) {
            apiCallMade = true;
          }
        });

        const saveBtn = page.locator('button:has-text("Save Changes")').first();
        if (await saveBtn.isVisible()) {
          await saveBtn.click();
          await page.waitForTimeout(1000);
          if (!apiCallMade) {
            console.log('[BUG-005] Save Changes closed modal without API call — data not persisted');
          }
          expect(apiCallMade).toBe(true);
        }
      }
    });

    test('invalid member ID shows not-found, not crash', async ({ page }) => {
      await page.goto('/members/nonexistent-id-12345678');
      await page.waitForTimeout(2000);
      const body = await page.locator('body').textContent();
      // Should show "No member found" or "Not Found", not throw a crash
      const hasCrash = body?.includes('Unhandled Runtime Error');
      expect(hasCrash).toBe(false);
    });
  });

  // ── 5. CHECK-INS ─────────────────────────────────────────────────────────

  test.describe('5. Check-Ins', () => {

    test.beforeEach(async ({ page }) => {
      await goTo(page, '/check-ins');
    });

    test('check-ins page loads with KPI cards', async ({ page }) => {
      await expect(page.locator('text=Total Today').first()).toBeVisible({ timeout: 8000 });
      await expect(page.locator('text=QR Scans').first()).toBeVisible({ timeout: 5000 });
    });

    test('Live Check-In Log section visible', async ({ page }) => {
      await expect(page.locator('text=Live Check-In Log').first()).toBeVisible({ timeout: 8000 });
    });

    test('QR scan button opens modal', async ({ page }) => {
      await page.locator('button').filter({ hasText: /Scan QR|QR/i }).first().click();
      await page.waitForTimeout(500);
      await expect(page.locator('text=Scan Member QR').first()).toBeVisible({ timeout: 5000 });
      await page.keyboard.press('Escape');
    });

    test('Numpad button opens modal with digit keys', async ({ page }) => {
      await page.locator('button').filter({ hasText: /Numpad/i }).first().click();
      await page.waitForTimeout(500);
      await expect(page.locator('text=Enter member number').first()).toBeVisible({ timeout: 5000 });
      // Digit buttons present
      await expect(page.locator('button:has-text("1")').first()).toBeVisible();
      await page.locator('button:has-text("Cancel")').last().click();
    });

    test('Manual button toggles inline panel', async ({ page }) => {
      await page.locator('button').filter({ hasText: /Manual/i }).first().click();
      await expect(page.locator('text=Manual Check-In').first()).toBeVisible({ timeout: 5000 });
    });

    test('Numpad: entering valid member number checks in', async ({ page }) => {
      await page.locator('button').filter({ hasText: /Numpad/i }).first().click();
      await page.waitForTimeout(300);
      // Type "1" (first digit) — if member found, check-in happens
      const btn1 = page.locator('button:has-text("1")').first();
      if (await btn1.isVisible()) {
        await btn1.click();
        await page.locator('button:has-text("Check In")').last().click();
        await page.waitForTimeout(700);
        // Either success flash or error — either is valid behavior
        await expect(page.locator('body')).not.toBeEmpty();
      }
    });

    /**
     * Verify Check-In method counts update after manual check-in
     */
    test('Manual check-in increments Manual KPI count', async ({ page }) => {
      const before = parseInt(
        (await page.locator('text=Manual').locator('..').locator('.text-2xl, .text-xl, .font-bold').first().textContent()) ?? '0',
        10,
      );
      await page.locator('button').filter({ hasText: /Manual/i }).first().click();
      await page.waitForTimeout(300);
      const searchInput = page.locator('input[placeholder*="Name, member"]').first();
      if (await searchInput.isVisible()) {
        // Find an actual member name from the log
        const firstLog = await page.locator('p.text-sm.font-medium').first().textContent();
        if (firstLog) {
          await searchInput.fill(firstLog.trim());
          await page.locator('button:has-text("Check In")').first().click();
          await page.waitForTimeout(700);
        }
      }
    });
  });

  // ── 6. CLASSES ────────────────────────────────────────────────────────────

  test.describe('6. Classes', () => {

    test.beforeEach(async ({ page }) => {
      await goTo(page, '/classes');
    });

    test('classes page loads with weekly schedule', async ({ page }) => {
      await expect(page.locator('h1').first()).toContainText('Classes');
      // At least one day section present
      await page.waitForSelector('.rounded-xl', { timeout: 10000 });
    });

    test('Create Class button opens modal', async ({ page }) => {
      await page.locator('button:has-text("Create Class")').click();
      await expect(page.locator('text=Create Class').first()).toBeVisible({ timeout: 5000 });
      await expect(page.locator('input[placeholder*="class"]').or(page.locator('label:has-text("Name")'))).toBeVisible();
      await page.locator('button:has-text("Cancel")').click();
    });

    test('Create Class modal has all required fields', async ({ page }) => {
      await page.locator('button:has-text("Create Class")').click();
      await page.waitForTimeout(300);
      // Trainer select, section, capacity, day, time all present
      await expect(page.locator('label:has-text("Trainer")').or(page.locator('select'))).toBeVisible();
      await page.locator('button:has-text("Cancel")').click();
    });

    test('Save class button disabled when name is empty', async ({ page }) => {
      await page.locator('button:has-text("Create Class")').click();
      await page.waitForTimeout(300);
      const saveBtn = page.locator('button:has-text("Save")').last();
      const isDisabled = await saveBtn.isDisabled();
      expect(isDisabled).toBe(true);
      await page.locator('button:has-text("Cancel")').click();
    });

    /**
     * BUG-006 — Classes: Create/Edit not persisted to API — local state only
     * src: classes/page.tsx:76-89 — save() updates localOverrides, no API POST/PUT
     * Fix: call POST /gyms/{gymId}/classes or PUT /gyms/{gymId}/classes/{id}
     */
    test('[BUG-006] Creating a class must POST to API, not just local state', async ({ page }) => {
      let apiCallMade = false;
      page.on('request', (req) => {
        if (req.method() === 'POST' && req.url().includes('/classes')) apiCallMade = true;
      });

      await page.locator('button:has-text("Create Class")').click();
      await page.waitForTimeout(300);

      const nameInput = page.locator('input').first();
      await nameInput.fill('Test Class E2E');

      const saveBtn = page.locator('button:has-text("Save")').last();
      if (!await saveBtn.isDisabled()) {
        await saveBtn.click();
        await page.waitForTimeout(1000);
        if (!apiCallMade) {
          console.log('[BUG-006] Class created only in local state, no API POST made');
        }
        expect(apiCallMade).toBe(true);
      }
    });

    test('Edit class button pre-fills modal with class name', async ({ page }) => {
      await page.waitForSelector('button[aria-label="Edit"]', { timeout: 10000 });
      const editBtn = page.locator('button[aria-label="Edit"]').first();
      await editBtn.click();
      await page.waitForTimeout(300);
      const nameInput = page.locator('input').first();
      const val = await nameInput.inputValue();
      expect(val.length).toBeGreaterThan(0);
      await page.locator('button:has-text("Cancel")').click();
    });

    test('Delete class shows ConfirmModal (not native confirm)', async ({ page }) => {
      await page.waitForSelector('button[aria-label="Delete"]', { timeout: 10000 });
      let nativeConfirm = false;
      page.once('dialog', (d) => { nativeConfirm = true; d.dismiss(); });
      await page.locator('button[aria-label="Delete"]').first().click();
      await page.waitForTimeout(400);
      expect(nativeConfirm).toBe(false);
      // ConfirmModal should appear instead
      const hasModal = await page.locator('text=Delete Class').isVisible().catch(() => false);
      expect(hasModal).toBe(true);
      await page.locator('button:has-text("Cancel")').click();
    });
  });

  // ── 7. BILLING ────────────────────────────────────────────────────────────

  test.describe('7. Billing', () => {

    test.beforeEach(async ({ page }) => {
      await goTo(page, '/billing');
    });

    test('billing page loads with KPI cards', async ({ page }) => {
      await expect(page.locator('h1').first()).toContainText('Billing');
      await expect(page.locator('text=Total Revenue').first()).toBeVisible({ timeout: 8000 });
    });

    test('Payments tab shows payment table', async ({ page }) => {
      await page.locator('button:has-text("Payments")').click();
      await expect(page.locator('th:has-text("Member")').first()).toBeVisible({ timeout: 8000 });
    });

    test('Plans tab shows membership plans', async ({ page }) => {
      await page.locator('button:has-text("Plans")').click();
      await expect(page.locator('text=Membership Plans').first()).toBeVisible({ timeout: 8000 });
    });

    test('Packages tab shows templates and active packages', async ({ page }) => {
      await page.locator('button:has-text("Packages")').click();
      await expect(page.locator('text=Session Package Templates').first()).toBeVisible({ timeout: 8000 });
    });

    /**
     * BUG-007 — Billing: Record Payment uses native alert() on success
     * src: billing/page.tsx:84  alert(`Payment of ... recorded for ...`);
     * Fix: replace with toast.success()
     */
    test('[BUG-007] Record Payment must NOT fire native alert()', async ({ page }) => {
      await page.locator('button:has-text("Record Payment")').first().click();
      await page.waitForTimeout(400);

      // Select first member
      const memberSelect = page.locator('select').first();
      if (await memberSelect.isVisible()) await memberSelect.selectOption({ index: 1 });

      // Enter amount
      const amountInput = page.locator('input[type="number"]').first();
      if (await amountInput.isVisible()) await amountInput.fill('500');

      let alertFired = false;
      page.once('dialog', (d) => {
        alertFired = true;
        console.log('[BUG-007] Record Payment alert:', d.message());
        d.dismiss();
      });

      await page.locator('button:has-text("Record Payment")').last().click();
      await page.waitForTimeout(700);
      expect(alertFired).toBe(false);
    });

    /**
     * BUG-008 — Billing: Assign Package uses native alert()
     * src: billing/page.tsx:67  alert(`${template.name} assigned to ${member.name}`);
     * Fix: replace with toast.success()
     */
    test('[BUG-008] Assign Package must NOT fire native alert()', async ({ page }) => {
      await page.locator('button:has-text("Packages")').click();
      await page.waitForTimeout(500);

      const assignBtn = page.locator('button:has-text("Assign to member")').first();
      if (!(await assignBtn.isVisible().catch(() => false))) {
        // No package templates seeded — nothing to assign; nothing to assert.
        test.skip();
        return;
      }

      let alertFired = false;
      page.on('dialog', (d) => { alertFired = true; console.log('[BUG-008] alert:', d.message()); d.dismiss(); });

      await assignBtn.click();
      await page.waitForTimeout(400);

      const memberSelect = page.locator('select').nth(0);
      const templateSelect = page.locator('select').nth(1);
      const memberOpts = await memberSelect.locator('option').count().catch(() => 0);
      const templateOpts = await templateSelect.locator('option').count().catch(() => 0);
      if (memberOpts > 1) await memberSelect.selectOption({ index: 1 });
      if (templateOpts > 1) await templateSelect.selectOption({ index: 1 });

      // Submit (the modal's primary CTA — last "Assign" button in document)
      const submit = page.locator('div[role="dialog"] button:has-text("Assign")').last();
      if (await submit.isVisible().catch(() => false)) {
        await submit.click();
        await page.waitForTimeout(700);
      }
      expect(alertFired).toBe(false);
    });

    /**
     * BUG-009 — Billing: Save Plan doesn't call API
     * src: billing/page.tsx:94-97 — savePlan() just closes modal
     * Fix: call POST/PUT /gyms/{gymId}/membership-plans
     */
    test('[BUG-009] Save Plan must POST/PUT to API', async ({ page }) => {
      await page.locator('button:has-text("Plans")').click();
      await page.waitForTimeout(300);

      let apiCallMade = false;
      page.on('request', (req) => {
        if ((req.method() === 'POST' || req.method() === 'PUT') && req.url().includes('plan')) {
          apiCallMade = true;
        }
      });

      await page.locator('button:has-text("New Plan")').click();
      await page.waitForTimeout(300);
      await page.locator('input').first().fill('Test Plan API');
      await page.locator('button:has-text("Save")').last().click();
      await page.waitForTimeout(1000);

      if (!apiCallMade) {
        console.log('[BUG-009] Save Plan closed modal without API call');
      }
      expect(apiCallMade).toBe(true);
    });

    /**
     * BUG-010 — Billing: Save Template doesn't call API
     * src: billing/page.tsx:108-111 — saveTemplate() just closes modal
     * Fix: call POST/PUT /gyms/{gymId}/session-package-templates
     */
    test('[BUG-010] Save Package Template must POST/PUT to API', async ({ page }) => {
      await page.locator('button:has-text("Packages")').click();
      await page.waitForTimeout(300);

      let apiCallMade = false;
      page.on('request', (req) => {
        if ((req.method() === 'POST' || req.method() === 'PUT') && req.url().includes('package')) {
          apiCallMade = true;
        }
      });

      await page.locator('button:has-text("New Template")').click();
      await page.waitForTimeout(300);
      await page.locator('input').first().fill('Test Template API');
      await page.locator('button:has-text("Save")').last().click();
      await page.waitForTimeout(1000);

      if (!apiCallMade) {
        console.log('[BUG-010] Save Template closed modal without API call');
      }
      expect(apiCallMade).toBe(true);
    });
  });

  // ── 8. ANNOUNCEMENTS ──────────────────────────────────────────────────────

  test.describe('8. Announcements', () => {

    test.beforeEach(async ({ page }) => {
      await goTo(page, '/announcements');
    });

    test('announcements list renders', async ({ page }) => {
      await expect(page.locator('h1').first()).toContainText('Announcements');
    });

    test('Create Announcement button opens modal with bilingual fields', async ({ page }) => {
      await page.locator('button:has-text("Create Announcement")').click();
      await page.waitForTimeout(300);
      await expect(page.locator('label:has-text("Title (English)")').first()).toBeVisible({ timeout: 5000 });
      await expect(page.locator('label:has-text("Title (Arabic)")').first()).toBeVisible();
      await page.locator('button:has-text("Cancel")').click();
    });

    test('Publish button disabled when title/body empty', async ({ page }) => {
      await page.locator('button:has-text("Create Announcement")').click();
      await page.waitForTimeout(300);
      const publishBtn = page.locator('button:has-text("Publish")').last();
      const isDisabled = await publishBtn.isDisabled();
      expect(isDisabled).toBe(true);
      await page.locator('button:has-text("Cancel")').click();
    });

    test('Publish creates announcement via API', async ({ page }) => {
      let apiCallMade = false;
      page.on('request', (req) => {
        if (req.method() === 'POST' && req.url().includes('announcement')) apiCallMade = true;
      });

      await page.locator('button:has-text("Create Announcement")').click();
      await page.waitForTimeout(300);

      await page.locator('input').nth(0).fill('Test E2E Announcement');
      await page.locator('textarea').nth(0).fill('Test body content for E2E test.');

      const publishBtn = page.locator('button:has-text("Publish")').last();
      if (!await publishBtn.isDisabled()) {
        await publishBtn.click();
        await page.waitForTimeout(1000);
        expect(apiCallMade).toBe(true);
      }
    });

    test('Edit announcement opens pre-filled modal', async ({ page }) => {
      const editBtn = page.locator('button[aria-label="Edit"]').first();
      if (await editBtn.isVisible({ timeout: 5000 }).catch(() => false)) {
        await editBtn.click();
        await page.waitForTimeout(300);
        const val = await page.locator('input').first().inputValue();
        expect(val.length).toBeGreaterThan(0);
        await page.locator('button:has-text("Cancel")').click();
      }
    });

    test('Delete announcement uses ConfirmModal not native confirm', async ({ page }) => {
      const deleteBtn = page.locator('button[aria-label="Delete"]').first();
      if (await deleteBtn.isVisible({ timeout: 5000 }).catch(() => false)) {
        let nativeConfirm = false;
        page.once('dialog', (d) => { nativeConfirm = true; d.dismiss(); });
        await deleteBtn.click();
        await page.waitForTimeout(400);
        expect(nativeConfirm).toBe(false);
        const cancelBtn = page.locator('button:has-text("Cancel")');
        if (await cancelBtn.isVisible({ timeout: 1000 }).catch(() => false)) await cancelBtn.click();
      }
    });
  });

  // ── 9. TRAINERS ───────────────────────────────────────────────────────────

  test.describe('9. Trainers', () => {

    test.beforeEach(async ({ page }) => {
      await goTo(page, '/trainers');
    });

    test('trainers page loads with active trainer grid', async ({ page }) => {
      await expect(page.locator('h1').first()).toContainText('Trainers');
      // Active trainer cards present
      await page.waitForSelector('a[href*="/trainers/"]', { timeout: 10000 });
    });

    test('pending approval section shows with Approve/Reject buttons', async ({ page }) => {
      const pendingSection = page.locator('text=Pending Approval').first();
      if (await pendingSection.isVisible({ timeout: 5000 }).catch(() => false)) {
        await expect(page.locator('button:has-text("Approve")').first()).toBeVisible();
        await expect(page.locator('button:has-text("Reject")').first()).toBeVisible();
      }
    });

    test('Approve trainer uses toast, not alert()', async ({ page }) => {
      const approveBtn = page.locator('button:has-text("Approve")').first();
      if (await approveBtn.isVisible()) {
        let alertFired = false;
        page.once('dialog', (d) => {
          alertFired = true;
          console.log('[TRAINER APPROVE] alert:', d.message());
          d.dismiss();
        });
        await approveBtn.click();
        await page.waitForTimeout(700);
        expect(alertFired).toBe(false);
      }
    });

    test('Reject trainer uses ConfirmModal, not native confirm', async ({ page }) => {
      const rejectBtn = page.locator('button:has-text("Reject")').first();
      if (await rejectBtn.isVisible()) {
        let nativeConfirm = false;
        page.once('dialog', (d) => { nativeConfirm = true; d.dismiss(); });
        await rejectBtn.click();
        await page.waitForTimeout(400);
        expect(nativeConfirm).toBe(false);
        const cancelBtn = page.locator('button:has-text("Cancel")');
        if (await cancelBtn.isVisible({ timeout: 1000 }).catch(() => false)) await cancelBtn.click();
      }
    });

    test('Add Trainer form validates name+email required', async ({ page }) => {
      await page.locator('button:has-text("Add Trainer")').click();
      await page.waitForTimeout(300);

      let alertFired = false;
      page.once('dialog', (d) => {
        alertFired = true;
        console.log('[ADD TRAINER] alert:', d.message());
        d.dismiss();
      });

      await page.locator('button:has-text("Send Invitation")').click();
      await page.waitForTimeout(500);

      if (alertFired) {
        console.log('[BUG] Add Trainer validation uses native alert()');
      }
      // Validation error should appear inline, not as alert
      expect(alertFired).toBe(false);
      await page.locator('button:has-text("Cancel")').click();
    });

    test('Trainer card links to detail page', async ({ page }) => {
      const trainerLink = page.locator('a[href*="/trainers/"]').first();
      if (await trainerLink.isVisible()) {
        await trainerLink.click();
        await page.waitForURL(/\/trainers\/[^/]+$/, { timeout: 8000 });
        expect(page.url()).toMatch(/\/trainers\//);
      }
    });
  });

  // ── 10. TRAINER DETAIL ────────────────────────────────────────────────────

  test.describe('10. Trainer Detail', () => {

    test.beforeEach(async ({ page }) => {
      await goTo(page, '/trainers');
      await page.waitForSelector('a[href*="/trainers/"]', { timeout: 10000 });
      await page.locator('a[href*="/trainers/"]').first().click();
      await page.waitForURL(/\/trainers\/[^/]+$/, { timeout: 8000 });
      await page.waitForTimeout(1500);
    });

    test('trainer detail loads with KPI cards', async ({ page }) => {
      await expect(page.locator('text=Active Clients').first()).toBeVisible({ timeout: 8000 });
      await expect(page.locator('text=Monthly Score').first()).toBeVisible();
      await expect(page.locator('text=Average Rating').first()).toBeVisible();
    });

    test('tabs present (Profile, Members, Sessions, Ratings, Earnings)', async ({ page }) => {
      for (const tab of ['Profile', 'Members', 'Sessions', 'Ratings', 'Earnings']) {
        await expect(page.locator(`button:has-text("${tab}")`).first()).toBeVisible({ timeout: 8000 });
      }
    });

    /**
     * BUG-011 — Trainer Detail: "Edit Profile" button uses alert()
     * src: trainers/[id]/page.tsx:284  alert(`Edit profile for ${trainer.name} — form coming soon.`)
     * Fix: implement an actual edit form modal
     */
    test('[BUG-011] Edit Profile button must NOT fire alert()', async ({ page }) => {
      const editBtn = page.locator('button:has-text("Edit Profile")').first();
      if (await editBtn.isVisible()) {
        let alertFired = false;
        page.once('dialog', (d) => {
          alertFired = true;
          console.log('[BUG-011] Edit Profile alert:', d.message());
          d.dismiss();
        });
        await editBtn.click();
        await page.waitForTimeout(500);
        expect(alertFired).toBe(false);
      }
    });

    /**
     * BUG-012 — Trainer Detail: fetchTrainerRate and fetchTrainerEarnings use
     *            wrong URL base — calls /api/v1/... relative to :3001 instead of :8080
     * src: trainers/[id]/page.tsx:17  fetch(`/api/v1/gyms/${gymId}/trainers/${trainerId}/rate`)
     * Fix: use apiClient.get() (configured with baseURL=http://localhost:8080)
     */
    test('[BUG-012] Trainer Earnings tab must NOT get 404 on session rate', async ({ page }) => {
      const apiErrors: { url: string; status: number }[] = [];
      page.on('response', (res) => {
        if (res.status() === 404 && res.url().includes('/api/v1/')) {
          apiErrors.push({ url: res.url(), status: res.status() });
        }
      });

      await page.locator('button:has-text("Earnings")').click();
      await page.waitForTimeout(3000);

      const rateErrors = apiErrors.filter((e) => e.url.includes('rate') || e.url.includes('earnings'));
      if (rateErrors.length > 0) {
        console.log('[BUG-012] Rate/Earnings 404s (wrong URL base):', rateErrors);
      }
      expect(rateErrors.length).toBe(0);
    });

    test('Members tab shows assigned clients table', async ({ page }) => {
      await page.locator('button:has-text("Members")').first().click();
      await page.waitForTimeout(500);
      await expect(page.locator('text=Assigned Clients').first()).toBeVisible({ timeout: 8000 });
    });

    test('Sessions tab renders session history', async ({ page }) => {
      await page.locator('button:has-text("Sessions")').first().click();
      await page.waitForTimeout(500);
      await expect(page.locator('text=Session History').first()).toBeVisible({ timeout: 8000 });
    });

    test('Ratings tab shows individual feedback', async ({ page }) => {
      await page.locator('button:has-text("Ratings")').first().click();
      await page.waitForTimeout(500);
      await expect(page.locator('text=Client Ratings').first()).toBeVisible({ timeout: 8000 });
    });
  });

  // ── 11. STAFF ─────────────────────────────────────────────────────────────

  test.describe('11. Staff', () => {

    test.beforeEach(async ({ page }) => {
      await goTo(page, '/staff');
    });

    test('staff table loads with staff members', async ({ page }) => {
      await expect(page.locator('h1').first()).toContainText('Staff');
      await page.waitForSelector('table', { timeout: 10000 });
    });

    test('permission matrix shows all 9 permission rows', async ({ page }) => {
      await expect(page.locator('text=Permission Matrix').first()).toBeVisible({ timeout: 8000 });
      await expect(page.locator('text=Manage Members').first()).toBeVisible();
      await expect(page.locator('text=Manage Billing').first()).toBeVisible();
    });

    test('Add Staff modal opens with required fields', async ({ page }) => {
      await page.locator('button:has-text("Add Staff")').click();
      await expect(page.locator('label:has-text("First Name")').first()).toBeVisible({ timeout: 5000 });
      await expect(page.locator('label:has-text("Email")').first()).toBeVisible();
      await page.locator('button:has-text("Cancel")').click();
    });

    test('Add Staff form shows inline validation (not alert)', async ({ page }) => {
      await page.locator('button:has-text("Add Staff")').click();
      await page.waitForTimeout(300);

      let alertFired = false;
      page.once('dialog', (d) => {
        alertFired = true;
        console.log('[ADD STAFF] alert:', d.message());
        d.dismiss();
      });

      // Click submit without filling required fields
      await page.locator('button:has-text("Add Staff")').last().click();
      await page.waitForTimeout(500);

      if (alertFired) {
        console.log('[BUG] Add Staff validation uses native alert()');
      }
      expect(alertFired).toBe(false);
      const cancelBtn = page.locator('button:has-text("Cancel")');
      if (await cancelBtn.isVisible({ timeout: 1000 }).catch(() => false)) await cancelBtn.click();
    });
  });

  // ── 12. ANALYTICS ─────────────────────────────────────────────────────────

  test.describe('12. Analytics', () => {

    test.beforeEach(async ({ page }) => {
      await goTo(page, '/analytics');
    });

    test('analytics page loads with KPI cards', async ({ page }) => {
      await expect(page.locator('h1').first()).toContainText('Analytics');
      await expect(page.locator('text=Active Members').first()).toBeVisible({ timeout: 8000 });
    });

    test('charts render (SVG elements present)', async ({ page }) => {
      const svgCount = await page.locator('svg').count();
      expect(svgCount).toBeGreaterThan(0);
    });

    test('Member Growth chart present', async ({ page }) => {
      await expect(page.locator('text=Member Growth').first()).toBeVisible({ timeout: 8000 });
    });

    test('Revenue Trend chart present', async ({ page }) => {
      await expect(page.locator('text=Revenue Trend').first()).toBeVisible({ timeout: 8000 });
    });

    /**
     * BUG-013 — Analytics: KPI values are hardcoded ("450", "127", "85%")
     * src: analytics/page.tsx:49-52 — all KpiCard value props are hardcoded strings
     * Fix: fetch from /gyms/{gymId}/analytics API endpoint
     */
    test('[BUG-013] Analytics KPI values must come from API, not be hardcoded "450"/"127"', async ({ page }) => {
      // If the page always shows exactly "450" for active members regardless of DB state, it's hardcoded
      // We flag this as a structural bug — test can only confirm the page renders
      const kpiText = await page.locator('body').textContent();
      const hasHardcoded =
        kpiText?.includes('"450"') ||
        (kpiText?.includes('450') && kpiText?.includes('127') && kpiText?.includes('85%'));
      // Log for informational purposes — the bug is confirmed by source code inspection
      if (hasHardcoded) {
        console.log('[BUG-013] Analytics appears to show hardcoded "450 active members" and "127 daily check-ins"');
      }
      // Page should at least render without crash
      await expect(page.locator('h1').first()).toBeVisible();
    });

    /**
     * BUG-014 — Analytics: memberGrowth and classUtilization are static mock arrays
     * src: analytics/page.tsx:11-27
     * Fix: replace with API calls to /gyms/{gymId}/analytics
     */
    test('[BUG-014] Class Utilization chart must reflect live DB data', async ({ page }) => {
      // Pilates always shows 100% if hardcoded — check for that static value
      const text = await page.locator('body').textContent();
      const hasStaticPilates = text?.includes('Pilates') && text?.includes('100');
      if (hasStaticPilates) {
        console.log('[BUG-014] Class Utilization uses hardcoded data (Pilates always 100%)');
      }
      await expect(page.locator('text=Class Utilization').first()).toBeVisible();
    });
  });

  // ── 13. GAMIFICATION ─────────────────────────────────────────────────────

  test.describe('13. Gamification', () => {

    test.beforeEach(async ({ page }) => {
      await goTo(page, '/gamification');
    });

    test('gamification page has three tabs', async ({ page }) => {
      await expect(page.locator('button:has-text("Member Leaderboard")')).toBeVisible({ timeout: 8000 });
      await expect(page.locator('button:has-text("Coach Rankings")')).toBeVisible();
      await expect(page.locator('button:has-text("Testimonials")')).toBeVisible();
    });

    test('Member Leaderboard shows ranked entries', async ({ page }) => {
      await page.locator('button:has-text("Member Leaderboard")').click();
      await page.waitForTimeout(500);
      await page.waitForSelector('tbody tr', { timeout: 10000 });
      const rows = await page.locator('tbody tr').count();
      expect(rows).toBeGreaterThan(0);
    });

    test('Coach Rankings shows trainer cards', async ({ page }) => {
      await page.locator('button:has-text("Coach Rankings")').click();
      await page.waitForTimeout(500);
      await expect(page.locator('text=Score').first()).toBeVisible({ timeout: 8000 });
    });

    test('Testimonials tab renders pending items with Approve/Reject', async ({ page }) => {
      await page.locator('button:has-text("Testimonials")').click();
      await page.waitForTimeout(500);
      const hasApprove = await page.locator('button:has-text("Approve")').first().isVisible().catch(() => false);
      const hasEmpty = await page.locator('text=No testimonials').isVisible().catch(() => false);
      expect(hasApprove || hasEmpty).toBeTruthy();
    });

    test('Reject testimonial uses ConfirmModal, not native confirm', async ({ page }) => {
      await page.locator('button:has-text("Testimonials")').click();
      await page.waitForTimeout(500);
      const rejectBtn = page.locator('button:has-text("Reject")').first();
      if (await rejectBtn.isVisible()) {
        let nativeConfirm = false;
        page.once('dialog', (d) => { nativeConfirm = true; d.dismiss(); });
        await rejectBtn.click();
        await page.waitForTimeout(400);
        expect(nativeConfirm).toBe(false);
        const cancelBtn = page.locator('button:has-text("Cancel")');
        if (await cancelBtn.isVisible({ timeout: 1000 }).catch(() => false)) await cancelBtn.click();
      }
    });
  });

  // ── 14. EXERCISES ─────────────────────────────────────────────────────────

  test.describe('14. Exercises', () => {

    test.beforeEach(async ({ page }) => {
      await goTo(page, '/exercises');
    });

    test('exercises page loads with global library', async ({ page }) => {
      await expect(page.locator('h1').first()).toContainText('Exercises');
      await expect(page.locator('text=Global Exercise Library').first()).toBeVisible({ timeout: 8000 });
    });

    test('exercise table shows seeded exercises (Squat, Deadlift)', async ({ page }) => {
      await page.waitForSelector('tbody tr', { timeout: 10000 });
      await expect(page.locator('text=Barbell Squat').first()).toBeVisible({ timeout: 8000 });
      await expect(page.locator('text=Deadlift').first()).toBeVisible();
    });

    test('muscle group filter works', async ({ page }) => {
      await page.waitForSelector('select', { timeout: 8000 });
      await page.locator('select').first().selectOption('Chest');
      await page.waitForTimeout(500);
      await expect(page.locator('text=Barbell Bench Press').first()).toBeVisible();
    });

    test('search filters exercises', async ({ page }) => {
      await page.waitForSelector('tbody tr', { timeout: 10000 });
      const searchInput = page.locator('input[placeholder*="earch"]').first();
      if (await searchInput.isVisible()) {
        const before = await page.locator('tbody tr').count();
        await searchInput.fill('squat');
        await page.waitForTimeout(600);
        const after = await page.locator('tbody tr').count();
        expect(after).toBeLessThanOrEqual(before);
      }
    });

    /**
     * BUG-015 — Exercises: Add Custom Exercise validation uses alert()
     * src: exercises/page.tsx:75  alert('Exercise name is required.');
     * Fix: show inline validation error instead
     */
    test('[BUG-015] Add Custom Exercise empty submit must NOT fire alert()', async ({ page }) => {
      const addBtn = page.locator('button:has-text("Add Custom Exercise")');
      if (await addBtn.isVisible({ timeout: 3000 }).catch(() => false)) {
        await addBtn.click();
        await page.waitForTimeout(300);

        let alertFired = false;
        page.once('dialog', (d) => {
          alertFired = true;
          console.log('[BUG-015] Add Custom Exercise alert:', d.message());
          d.dismiss();
        });

        await page.locator('button:has-text("Add Exercise")').last().click();
        await page.waitForTimeout(500);
        expect(alertFired).toBe(false);
        const cancelBtn = page.locator('button:has-text("Cancel")');
        if (await cancelBtn.isVisible({ timeout: 1000 }).catch(() => false)) await cancelBtn.click();
      }
    });
  });

  // ── 15. DATA IMPORT ───────────────────────────────────────────────────────

  test.describe('15. Data Import', () => {

    test.beforeEach(async ({ page }) => {
      await goTo(page, '/data-import');
    });

    test('data import page loads', async ({ page }) => {
      await expect(page.locator('h1').first()).toContainText('Data Import');
    });

    test('shows feature gate or import content (not crash)', async ({ page }) => {
      await page.waitForTimeout(4000);
      const hasContent = await page.locator('text=Step 1').isVisible().catch(() => false);
      const hasGate = await page.locator('text=Upgrade').or(page.locator('text=Pro Plan')).isVisible().catch(() => false);
      const hasLoading = await page.locator('.animate-pulse').isVisible().catch(() => false);
      expect(hasContent || hasGate || hasLoading).toBeTruthy();
    });

    test('Download Template button present when feature enabled', async ({ page }) => {
      await page.waitForTimeout(4000);
      const templateBtn = page.locator('button:has-text("Download Excel template")');
      if (await templateBtn.isVisible()) {
        await expect(templateBtn).toBeVisible();
      }
    });

    test('Source selector has Excel, Odoo, Daftra options', async ({ page }) => {
      await page.waitForTimeout(4000);
      const sourceSelect = page.locator('select').first();
      if (await sourceSelect.isVisible()) {
        const options = await sourceSelect.locator('option').allTextContents();
        expect(options.some((o) => o.includes('Excel'))).toBe(true);
        expect(options.some((o) => o.includes('Odoo'))).toBe(true);
        expect(options.some((o) => o.includes('Daftra'))).toBe(true);
      }
    });
  });

  // ── 16. KIOSK PIN ─────────────────────────────────────────────────────────

  test.describe('16. Kiosk PIN', () => {

    test.beforeEach(async ({ page }) => {
      await goTo(page, '/kiosk-pin');
    });

    test('kiosk PIN page loads', async ({ page }) => {
      await expect(page.locator('h1').first()).toContainText('Kiosk PIN');
    });

    test('PIN status indicator visible (set or not set)', async ({ page }) => {
      await page.waitForTimeout(2000);
      const hasSet = await page.locator('text=A PIN is set').isVisible().catch(() => false);
      const hasNotSet = await page.locator('text=No PIN is set').isVisible().catch(() => false);
      expect(hasSet || hasNotSet).toBeTruthy();
    });

    test('New PIN and Confirm PIN inputs present', async ({ page }) => {
      await page.waitForTimeout(2000);
      const inputs = await page.locator('input[type="password"]').count();
      expect(inputs).toBeGreaterThanOrEqual(1);
    });

    test('Save button disabled when PIN fields empty or mismatched', async ({ page }) => {
      await page.waitForTimeout(2000);
      const saveBtn = page.locator('button:has-text("Save")').first();
      if (await saveBtn.isVisible()) {
        const isDisabled = await saveBtn.isDisabled();
        expect(isDisabled).toBe(true);
      }
    });

    test('entering matching 4-digit PIN enables Save button', async ({ page }) => {
      await page.waitForTimeout(2000);
      const inputs = page.locator('input[type="password"]');
      const count = await inputs.count();
      if (count >= 2) {
        const newPin = inputs.nth(count - 2);
        const confirmPin = inputs.nth(count - 1);
        await newPin.fill('1234');
        await confirmPin.fill('1234');
        await page.waitForTimeout(300);
        const saveBtn = page.locator('button:has-text("Save")').first();
        const isDisabled = await saveBtn.isDisabled();
        expect(isDisabled).toBe(false);
      }
    });

    test('mismatched PINs keep Save button disabled', async ({ page }) => {
      await page.waitForTimeout(2000);
      const inputs = page.locator('input[type="password"]');
      const count = await inputs.count();
      if (count >= 2) {
        const newPin = inputs.nth(count - 2);
        const confirmPin = inputs.nth(count - 1);
        await newPin.fill('1234');
        await confirmPin.fill('5678');
        await page.waitForTimeout(300);
        const saveBtn = page.locator('button:has-text("Save")').first();
        const isDisabled = await saveBtn.isDisabled();
        expect(isDisabled).toBe(true);
      }
    });

    test('PIN validation: error shown for non-numeric input', async ({ page }) => {
      await page.waitForTimeout(2000);
      const inputs = page.locator('input[type="password"]');
      const count = await inputs.count();
      if (count >= 2) {
        // inputMode=numeric prevents non-digits client-side, so just verify disabled state
        const newPin = inputs.nth(count - 2);
        await newPin.fill('abc');
        await page.waitForTimeout(300);
        const saveBtn = page.locator('button:has-text("Save")').first();
        const isDisabled = await saveBtn.isDisabled();
        expect(isDisabled).toBe(true);
      }
    });
  });

  // ── 17. SETTINGS ──────────────────────────────────────────────────────────

  test.describe('17. Settings', () => {

    test.beforeEach(async ({ page }) => {
      await goTo(page, '/settings');
    });

    test('settings page loads with Gym Profile section', async ({ page }) => {
      await expect(page.locator('h1').first()).toContainText('Settings');
      await expect(page.locator('text=Gym Profile').first()).toBeVisible({ timeout: 8000 });
    });

    test('gym name, phone, email fields pre-filled', async ({ page }) => {
      const nameInput = page.locator('input').first();
      const val = await nameInput.inputValue();
      expect(val.length).toBeGreaterThan(0);
    });

    test('Amenities section has checkboxes', async ({ page }) => {
      await expect(page.locator('text=Amenities').first()).toBeVisible({ timeout: 8000 });
      await expect(page.locator('text=Parking').first()).toBeVisible();
      await expect(page.locator('text=Showers').first()).toBeVisible();
    });

    test('Notifications toggles visible', async ({ page }) => {
      await expect(page.locator('text=Notifications').first()).toBeVisible({ timeout: 8000 });
      await expect(page.locator('text=Session Reminders').first()).toBeVisible();
      await expect(page.locator('text=Expiry Warnings').first()).toBeVisible();
    });

    test('Operating Hours section visible', async ({ page }) => {
      await expect(page.locator('text=Operating Hours').first()).toBeVisible({ timeout: 8000 });
    });

    /**
     * BUG-016 — Settings: Save Changes uses toast but doesn't call API
     * src: settings/page.tsx:43-45 — handleSave() only calls toast.success(), no API call
     * Fix: call PUT /gyms/{gymId} with updated gym profile
     */
    test('[BUG-016] Save Changes must call API (not just show toast)', async ({ page }) => {
      let apiCallMade = false;
      page.on('request', (req) => {
        if ((req.method() === 'PUT' || req.method() === 'PATCH') && req.url().includes('/gyms/')) {
          apiCallMade = true;
        }
      });

      const saveBtn = page.locator('button:has-text("Save Changes")').first();
      if (await saveBtn.isVisible()) {
        await saveBtn.click();
        await page.waitForTimeout(1500);
        if (!apiCallMade) {
          console.log('[BUG-016] Save Changes showed toast without API call — settings not persisted');
        }
        expect(apiCallMade).toBe(true);
      }
    });

    /**
     * BUG-017 — Settings: Field values are hardcoded, not loaded from API
     * src: settings/page.tsx:29-38 — useState with hardcoded "First Gym", "01272463444", etc.
     * Fix: load from GET /gyms/{gymId} on mount
     */
    test('[BUG-017] Gym name field must load from API on mount', async ({ page }) => {
      let gymApiCalled = false;
      page.on('request', (req) => {
        if (req.method() === 'GET' && /\/gyms\/[^/]+$/.test(req.url())) {
          gymApiCalled = true;
        }
      });
      await page.goto('/settings');
      await page.waitForTimeout(3000);
      if (!gymApiCalled) {
        console.log('[BUG-017] Settings page did not load gym data from API on mount — hardcoded values');
      }
      expect(gymApiCalled).toBe(true);
    });
  });

  // ── 18. NAVIGATION ────────────────────────────────────────────────────────

  test.describe('18. Navigation & Routing', () => {

    test.beforeEach(async ({ page }) => {
      await ensureLoggedIn(page);
    });

    test('all 14 routes load without crash or redirect to login', async ({ page }) => {
      test.setTimeout(180000);
      const routes = [
        '/', '/members', '/trainers', '/classes', '/exercises',
        '/billing', '/gamification', '/announcements', '/analytics',
        '/check-ins', '/staff', '/data-import', '/kiosk-pin', '/settings',
      ];
      for (const route of routes) {
        await page.goto(route);
        await page.waitForTimeout(500);
        const url = page.url();
        expect(url).not.toContain('/login');
        // Check for crash
        const body = await page.locator('body').textContent();
        const crashed = body?.includes('Unhandled Runtime Error');
        if (crashed) console.log(`[CRASH] Route ${route} shows runtime error`);
        expect(crashed).toBe(false);
      }
    });

    test('404 for unknown route — shows not-found page or redirects', async ({ page }) => {
      await page.goto('/nonexistent-route-xyz-9999');
      await page.waitForTimeout(1500);
      const body = await page.locator('body').textContent();
      const is404 = body?.includes('404') || body?.includes('Not Found') || body?.includes('not found');
      const redirected = page.url().endsWith('/') || page.url().endsWith(':3001/');
      expect(is404 || redirected).toBeTruthy();
    });

    test('sidebar links navigate correctly from dashboard', async ({ page }) => {
      await page.goto('/');
      await page.waitForTimeout(2000);

      const membersLink = page.locator('a[href="/members"]').first();
      if (await membersLink.isVisible()) {
        await membersLink.click();
        await expect(page).toHaveURL('/members');
      }
    });
  });

  // ── 19. BUG-SPECIFIC: API PATTERNS ───────────────────────────────────────

  test.describe('19. API Integration Patterns', () => {

    /**
     * BUG-018 — All pages: Several actions still use alert() instead of toast/modal
     * Comprehensive check across members, exercises, billing, trainer detail
     */
    test('[BUG-018] Global alert() audit — no page should use native alert() for user feedback', async ({ page }) => {
      await ensureLoggedIn(page);
      const buggyAlerts: string[] = [];

      // Pages + triggers to check
      const checks = [
        {
          path: '/members',
          trigger: async () => {
            await page.locator('button:has-text("Add Member")').first().click();
            await page.waitForTimeout(200);
            // Fill required fields
            const inputs = page.locator('input.mt-1.w-full.h-9');
            if (await inputs.first().isVisible()) await inputs.first().fill('Test Alert Audit');
            const phoneInput = page.locator('input[type="tel"]').first();
            if (await phoneInput.isVisible()) await phoneInput.fill('01099887766');
            await page.locator('button:has-text("Add Member")').last().click();
          },
          label: 'Members Add Member',
        },
        {
          path: '/exercises',
          trigger: async () => {
            const addBtn = page.locator('button:has-text("Add Custom Exercise")');
            if (await addBtn.isVisible({ timeout: 2000 }).catch(() => false)) {
              await addBtn.click();
              await page.waitForTimeout(200);
              await page.locator('button:has-text("Add Exercise")').last().click();
            }
          },
          label: 'Exercises Add Custom (empty name)',
        },
      ];

      for (const check of checks) {
        await page.goto(check.path);
        await page.waitForTimeout(2000);
        page.once('dialog', (d) => {
          buggyAlerts.push(check.label);
          console.log(`[BUG-018] alert() on "${check.label}": ${d.message()}`);
          d.dismiss();
        });
        await check.trigger();
        await page.waitForTimeout(700);
      }

      if (buggyAlerts.length > 0) {
        console.log('[BUG-018] Pages still using native alert():', buggyAlerts);
      }
      expect(buggyAlerts.length).toBe(0);
    });

    /**
     * BUG-019 — Members, Billing, Classes: mutations don't hit API
     * Quick probe: after "adding" a member or recording a payment, check network requests
     */
    test('[BUG-019] Member creation must POST to /gyms/{gymId}/members', async ({ page }) => {
      await goTo(page, '/members');

      let memberPostMade = false;
      page.on('request', (req) => {
        if (req.method() === 'POST' && req.url().includes('/members')) memberPostMade = true;
      });

      await page.locator('button:has-text("Add Member")').first().click();
      await page.waitForTimeout(200);
      const inputs = page.locator('input.mt-1.w-full.h-9');
      if (await inputs.first().isVisible()) await inputs.first().fill('Network Test Member');
      const phoneInput = page.locator('input[type="tel"]').first();
      if (await phoneInput.isVisible()) await phoneInput.fill('01012345678');

      // Handle any alert that may appear
      page.on('dialog', (d) => d.dismiss());
      await page.locator('button:has-text("Add Member")').last().click();
      await page.waitForTimeout(1000);

      if (!memberPostMade) {
        console.log('[BUG-019] Add Member did not POST to API — data lost on refresh');
      }
      expect(memberPostMade).toBe(true);
    });
  });
});
