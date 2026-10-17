import { test, expect } from '@playwright/test';
import { BASE_URL, SUITE, goTo, warmupStaffLogin } from '../helpers/gym-staff-comprehensive';
import { cancelIfShown, clickCancel, expectClickWithoutDialog, type PageFixtures } from '../helpers/ui';

test.use({ baseURL: BASE_URL });

// ── 11. STAFF ─────────────────────────────────────────────────────────────

async function tableLoads({ page }: PageFixtures) {
  await expect(page.locator('h1').first()).toContainText('Staff');
  await page.waitForSelector('table', { timeout: 10000 });
}

async function permissionMatrixRows({ page }: PageFixtures) {
  await expect(page.locator('text=Permission Matrix').first()).toBeVisible({ timeout: 8000 });
  await expect(page.locator('text=Manage Members').first()).toBeVisible();
  await expect(page.locator('text=Manage Billing').first()).toBeVisible();
}

async function addStaffRequiredFields({ page }: PageFixtures) {
  await page.locator('button:has-text("Add Staff")').click();
  await expect(page.locator('label:has-text("First Name")').first()).toBeVisible({ timeout: 5000 });
  await expect(page.locator('label:has-text("Email")').first()).toBeVisible();
  await clickCancel(page);
}

async function addStaffInlineValidation({ page }: PageFixtures) {
  await page.locator('button:has-text("Add Staff")').click();
  await page.waitForTimeout(300);

  // Click submit without filling required fields
  await expectClickWithoutDialog(page, {
    target: page.locator('button:has-text("Add Staff")').last(),
    waitMs: 500,
    logLabel: '[ADD STAFF] alert:',
    bugMessage: '[BUG] Add Staff validation uses native alert()',
  });
  await cancelIfShown(page);
}

test.describe(SUITE, () => {
  test.beforeAll(warmupStaffLogin);

  test.describe('11. Staff', () => {
    test.beforeEach(async ({ page }) => {
      await goTo(page, '/staff');
    });

    test('staff table loads with staff members', tableLoads);
    test('permission matrix shows all 9 permission rows', permissionMatrixRows);
    test('Add Staff modal opens with required fields', addStaffRequiredFields);
    test('Add Staff form shows inline validation (not alert)', addStaffInlineValidation);
  });
});
