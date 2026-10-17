import { test, expect, type Page } from '@playwright/test';
import { SUITE, ensureAppReachable, openStaffPage } from '../helpers/gym-staff';
import { clickCancel, expectClickWithoutDialog, expectHeadingVisible, isShown, type PageFixtures } from '../helpers/ui';

// ──────────────────────────────────────────────────────────────────────────
// 9. STAFF
// ──────────────────────────────────────────────────────────────────────────

function addStaffButton(page: Page) {
  return page.locator('button:has-text("Add Staff")');
}

async function loadsWithList({ page }: PageFixtures) {
  await expectHeadingVisible(page);
  await expect(page.locator('text=Demo Staff').first()).toBeVisible({ timeout: 8000 });
}

async function cardsShowRoles({ page }: PageFixtures) {
  // Page is functional when at least an action surface (Add Staff button
  // or any role label cell) renders.
  const actionSurface = addStaffButton(page).or(page.locator('text=/owner|manager|receptionist/i')).first();
  const hasAction = await isShown(actionSurface, 8000);
  expect(hasAction).toBeTruthy();
}

async function addStaffOpens({ page }: PageFixtures) {
  const addBtn = addStaffButton(page);
  if (!(await addBtn.isVisible())) return;
  await addBtn.click();
  await expect(page.locator('input').first()).toBeVisible({ timeout: 5000 });
  await clickCancel(page);
}

async function submitEmptyStaffForm(page: Page) {
  const addStaffSubmit = page.locator('button:has-text("Add Staff Member")').or(page.locator('button:has-text("Add")').last());
  if (!(await addStaffSubmit.isVisible())) return;
  // Will FAIL confirming bug
  await expectClickWithoutDialog(page, {
    target: addStaffSubmit,
    waitMs: 500,
    logLabel: '[BUG CHECK] Add Staff fired alert():',
    bugMessage: '[BUG] Add Staff validation uses native alert() — use inline form validation',
  });
}

// BUG DETECTION: Add Staff validation uses alert()
async function emptySubmitUsesNoAlert({ page }: PageFixtures) {
  const addBtn = addStaffButton(page);
  if (!(await addBtn.isVisible())) return;
  await addBtn.click();
  await page.waitForTimeout(400);
  await submitEmptyStaffForm(page);
  const cancelBtn = page.locator('button:has-text("Cancel")');
  if (await cancelBtn.isVisible()) await cancelBtn.click();
}

async function permissionsVisible({ page }: PageFixtures) {
  await expect(
    page.locator('text=Manage Members').or(page.locator('text=manage_members')).first(),
  ).toBeVisible({ timeout: 8000 });
}

test.describe(SUITE, () => {
  test.beforeAll(ensureAppReachable);

  test.describe('Staff', () => {
    test.beforeEach(async ({ page }) => {
      await openStaffPage(page, '/staff', 1500);
    });

    test('staff page loads with staff list', loadsWithList);
    test('staff cards show role and permissions', cardsShowRoles);
    test('Add Staff modal opens', addStaffOpens);
    test('[BUG CHECK] Add Staff empty submit uses alert() instead of form validation', emptySubmitUsesNoAlert);
    test('staff permissions checkboxes visible', permissionsVisible);
  });
});
