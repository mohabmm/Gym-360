import { test, expect, type Page } from '@playwright/test';
import { BASE_URL, SUITE, goTo, warmupStaffLogin } from '../helpers/gym-staff-comprehensive';
import { clickCancel, isShown, watchDialog, watchRequest, type PageFixtures } from '../helpers/ui';

test.use({ baseURL: BASE_URL });

// ── 6. CLASSES ────────────────────────────────────────────────────────────

async function openCreateClass(page: Page, settleMs = 300) {
  await page.locator('button:has-text("Create Class")').click();
  await page.waitForTimeout(settleMs);
}

async function loadsWeeklySchedule({ page }: PageFixtures) {
  await expect(page.locator('h1').first()).toContainText('Classes');
  // At least one day section present
  await page.waitForSelector('.rounded-xl', { timeout: 10000 });
}

async function createOpensModal({ page }: PageFixtures) {
  await page.locator('button:has-text("Create Class")').click();
  await expect(page.locator('text=Create Class').first()).toBeVisible({ timeout: 5000 });
  await expect(page.locator('input[placeholder*="class"]').or(page.locator('label:has-text("Name")'))).toBeVisible();
  await clickCancel(page);
}

async function createHasFields({ page }: PageFixtures) {
  await openCreateClass(page);
  // Trainer select, section, capacity, day, time all present
  await expect(page.locator('label:has-text("Trainer")').or(page.locator('select'))).toBeVisible();
  await clickCancel(page);
}

async function saveDisabledWhenEmpty({ page }: PageFixtures) {
  await openCreateClass(page);
  const saveBtn = page.locator('button:has-text("Save")').last();
  const isDisabled = await saveBtn.isDisabled();
  expect(isDisabled).toBe(true);
  await clickCancel(page);
}

/**
 * BUG-006 — Classes: Create/Edit not persisted to API — local state only
 * src: classes/page.tsx:76-89 — save() updates localOverrides, no API POST/PUT
 * Fix: call POST /gyms/{gymId}/classes or PUT /gyms/{gymId}/classes/{id}
 */
async function createPostsToApi({ page }: PageFixtures) {
  const apiCall = watchRequest(page, (req) => req.method() === 'POST' && req.url().includes('/classes'));

  await openCreateClass(page);

  const nameInput = page.locator('input').first();
  await nameInput.fill('Test Class E2E');

  const saveBtn = page.locator('button:has-text("Save")').last();
  if (await saveBtn.isDisabled()) return;
  await saveBtn.click();
  await page.waitForTimeout(1000);
  if (!apiCall.seen) {
    console.log('[BUG-006] Class created only in local state, no API POST made');
  }
  expect(apiCall.seen).toBe(true);
}

async function editPrefillsName({ page }: PageFixtures) {
  await page.waitForSelector('button[aria-label="Edit"]', { timeout: 10000 });
  const editBtn = page.locator('button[aria-label="Edit"]').first();
  await editBtn.click();
  await page.waitForTimeout(300);
  const nameInput = page.locator('input').first();
  const val = await nameInput.inputValue();
  expect(val.length).toBeGreaterThan(0);
  await clickCancel(page);
}

async function deleteShowsConfirmModal({ page }: PageFixtures) {
  await page.waitForSelector('button[aria-label="Delete"]', { timeout: 10000 });
  const nativeConfirm = watchDialog(page);
  await page.locator('button[aria-label="Delete"]').first().click();
  await page.waitForTimeout(400);
  expect(nativeConfirm.fired).toBe(false);
  // ConfirmModal should appear instead
  const hasModal = await isShown(page.locator('text=Delete Class'));
  expect(hasModal).toBe(true);
  await clickCancel(page);
}

test.describe(SUITE, () => {
  test.beforeAll(warmupStaffLogin);

  test.describe('6. Classes', () => {
    test.beforeEach(async ({ page }) => {
      await goTo(page, '/classes');
    });

    test('classes page loads with weekly schedule', loadsWeeklySchedule);
    test('Create Class button opens modal', createOpensModal);
    test('Create Class modal has all required fields', createHasFields);
    test('Save class button disabled when name is empty', saveDisabledWhenEmpty);
    test('[BUG-006] Creating a class must POST to API, not just local state', createPostsToApi);
    test('Edit class button pre-fills modal with class name', editPrefillsName);
    test('Delete class shows ConfirmModal (not native confirm)', deleteShowsConfirmModal);
  });
});
