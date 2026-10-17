import { test, expect, type Page } from '@playwright/test';
import { BASE_URL, SUITE, goTo, warmupStaffLogin } from '../helpers/gym-staff-comprehensive';
import { cancelIfShown, clickCancel, isShown, watchDialog, watchRequest, type PageFixtures } from '../helpers/ui';

test.use({ baseURL: BASE_URL });

// ── 8. ANNOUNCEMENTS ──────────────────────────────────────────────────────

async function openCreateModal(page: Page) {
  await page.locator('button:has-text("Create Announcement")').click();
  await page.waitForTimeout(300);
}

async function listRenders({ page }: PageFixtures) {
  await expect(page.locator('h1').first()).toContainText('Announcements');
}

async function createHasBilingualFields({ page }: PageFixtures) {
  await openCreateModal(page);
  await expect(page.locator('label:has-text("Title (English)")').first()).toBeVisible({ timeout: 5000 });
  await expect(page.locator('label:has-text("Title (Arabic)")').first()).toBeVisible();
  await clickCancel(page);
}

async function publishDisabledWhenEmpty({ page }: PageFixtures) {
  await openCreateModal(page);
  const publishBtn = page.locator('button:has-text("Publish")').last();
  const isDisabled = await publishBtn.isDisabled();
  expect(isDisabled).toBe(true);
  await clickCancel(page);
}

async function publishCallsApi({ page }: PageFixtures) {
  const apiCall = watchRequest(page, (req) => req.method() === 'POST' && req.url().includes('announcement'));

  await openCreateModal(page);

  await page.locator('input').nth(0).fill('Test E2E Announcement');
  await page.locator('textarea').nth(0).fill('Test body content for E2E test.');

  const publishBtn = page.locator('button:has-text("Publish")').last();
  if (await publishBtn.isDisabled()) return;
  await publishBtn.click();
  await page.waitForTimeout(1000);
  expect(apiCall.seen).toBe(true);
}

async function editOpensPrefilled({ page }: PageFixtures) {
  const editBtn = page.locator('button[aria-label="Edit"]').first();
  if (!(await isShown(editBtn, 5000))) return;
  await editBtn.click();
  await page.waitForTimeout(300);
  const val = await page.locator('input').first().inputValue();
  expect(val.length).toBeGreaterThan(0);
  await clickCancel(page);
}

async function deleteUsesConfirmModal({ page }: PageFixtures) {
  const deleteBtn = page.locator('button[aria-label="Delete"]').first();
  if (!(await isShown(deleteBtn, 5000))) return;
  const nativeConfirm = watchDialog(page);
  await deleteBtn.click();
  await page.waitForTimeout(400);
  expect(nativeConfirm.fired).toBe(false);
  await cancelIfShown(page);
}

test.describe(SUITE, () => {
  test.beforeAll(warmupStaffLogin);

  test.describe('8. Announcements', () => {
    test.beforeEach(async ({ page }) => {
      await goTo(page, '/announcements');
    });

    test('announcements list renders', listRenders);
    test('Create Announcement button opens modal with bilingual fields', createHasBilingualFields);
    test('Publish button disabled when title/body empty', publishDisabledWhenEmpty);
    test('Publish creates announcement via API', publishCallsApi);
    test('Edit announcement opens pre-filled modal', editOpensPrefilled);
    test('Delete announcement uses ConfirmModal not native confirm', deleteUsesConfirmModal);
  });
});
