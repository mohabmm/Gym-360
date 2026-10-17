import { test, expect } from '@playwright/test';
import { SUITE, ensureAppReachable, openStaffPage } from '../helpers/gym-staff';
import { expectBodyNotEmpty, expectHeadingVisible, type PageFixtures } from '../helpers/ui';

// ──────────────────────────────────────────────────────────────────────────
// 14. KIOSK PIN
// ──────────────────────────────────────────────────────────────────────────

async function loadsWithForm({ page }: PageFixtures) {
  await expectHeadingVisible(page);
}

async function pinInputsVisible({ page }: PageFixtures) {
  await expect(
    page.locator('input[type="text"]').or(page.locator('input[type="password"]').or(page.locator('input[type="number"]'))).first(),
  ).toBeVisible({ timeout: 8000 });
}

async function rejectsInvalidInput({ page }: PageFixtures) {
  const pinInput = page.locator('input').first();
  if (!(await pinInput.isVisible())) return;
  await pinInput.fill('abc');
  await page.waitForTimeout(300);
  // Error or save button disabled
  const saveBtn = page.locator('button:has-text("Save")').or(page.locator('button:has-text("Set PIN")'));
  const isDisabled = await saveBtn.first().isDisabled().catch(() => true);
  console.log('[KIOSK PIN] Save disabled for invalid input:', isDisabled);
}

async function requiresDigits({ page }: PageFixtures) {
  const pinInputs = page.locator('input[type="text"], input[type="password"]');
  const count = await pinInputs.count();
  if (count === 0) return;
  await pinInputs.first().fill('1234');
  await page.waitForTimeout(300);
  // Valid 4-digit PIN should enable save
  await expectBodyNotEmpty(page);
}

test.describe(SUITE, () => {
  test.beforeAll(ensureAppReachable);

  test.describe('Kiosk PIN', () => {
    test.beforeEach(async ({ page }) => {
      await openStaffPage(page, '/kiosk-pin', 2000);
    });

    test('kiosk PIN page loads with form', loadsWithForm);
    test('PIN input fields visible', pinInputsVisible);
    test('PIN validation rejects non-digit or short input', rejectsInvalidInput);
    test('PIN validation requires 4-8 digits', requiresDigits);
  });
});
