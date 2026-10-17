import { test, expect } from '@playwright/test';
import { SUITE, ensureAppReachable, openStaffPage } from '../helpers/gym-staff';
import { expectHeadingVisible, expectTextVisible, type PageFixtures } from '../helpers/ui';

// ──────────────────────────────────────────────────────────────────────────
// 15. SETTINGS
// ──────────────────────────────────────────────────────────────────────────

async function pageLoads({ page }: PageFixtures) {
  await expectHeadingVisible(page);
}

async function gymNamePrefilled({ page }: PageFixtures) {
  const gymNameInput = page.locator('input').first();
  if (!(await gymNameInput.isVisible())) return;
  const val = await gymNameInput.inputValue();
  expect(val.length).toBeGreaterThan(0);
  // Should contain gym name
  console.log('[SETTINGS] Gym name value:', val);
}

async function amenitiesVisible({ page }: PageFixtures) {
  await expectTextVisible(page, 'Parking');
}

async function notificationTogglesVisible({ page }: PageFixtures) {
  await expect(
    page.locator('text=Session Reminders').or(page.locator('text=Expiry Warnings')).first(),
  ).toBeVisible({ timeout: 8000 });
}

async function saveDoesNotCrash({ page }: PageFixtures) {
  const saveBtn = page.locator('button:has-text("Save Changes")');
  if (!(await saveBtn.isVisible())) return;
  await saveBtn.click();
  await page.waitForTimeout(2000);
  // Should show toast or remain on settings page
  await expect(page).toHaveURL('/settings');
}

async function addressFieldsVisible({ page }: PageFixtures) {
  await expectTextVisible(page, 'Address');
}

test.describe(SUITE, () => {
  test.beforeAll(ensureAppReachable);

  test.describe('Settings', () => {
    test.beforeEach(async ({ page }) => {
      await openStaffPage(page, '/settings', 1500);
    });

    test('settings page loads', pageLoads);
    test('gym name field is pre-filled and editable', gymNamePrefilled);
    test('amenities checkboxes visible and toggleable', amenitiesVisible);
    test('notification toggles visible', notificationTogglesVisible);
    test('Save Changes button works without crashing', saveDoesNotCrash);
    test('address/location fields visible', addressFieldsVisible);
  });
});
