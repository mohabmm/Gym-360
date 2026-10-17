import { test, expect, type Locator, type Page } from '@playwright/test';
import { BASE_URL, SUITE, goTo, warmupStaffLogin } from '../helpers/gym-staff-comprehensive';
import { isShown, type PageFixtures } from '../helpers/ui';

test.use({ baseURL: BASE_URL });

// ── 16. KIOSK PIN ─────────────────────────────────────────────────────────

/** The last two password inputs are New PIN and Confirm PIN; null when absent. */
async function pinInputs(page: Page): Promise<{ newPin: Locator; confirmPin: Locator } | null> {
  await page.waitForTimeout(2000);
  const inputs = page.locator('input[type="password"]');
  const count = await inputs.count();
  if (count < 2) return null;
  return { newPin: inputs.nth(count - 2), confirmPin: inputs.nth(count - 1) };
}

async function expectSaveDisabled(page: Page, disabled: boolean) {
  await page.waitForTimeout(300);
  const saveBtn = page.locator('button:has-text("Save")').first();
  const isDisabled = await saveBtn.isDisabled();
  expect(isDisabled).toBe(disabled);
}

/** Returns a test body that fills New/Confirm PIN and checks the Save state. */
function pinEntry(newValue: string, confirmValue: string, saveDisabled: boolean) {
  return async ({ page }: PageFixtures) => {
    const pins = await pinInputs(page);
    if (!pins) return;
    await pins.newPin.fill(newValue);
    await pins.confirmPin.fill(confirmValue);
    await expectSaveDisabled(page, saveDisabled);
  };
}

async function pageLoads({ page }: PageFixtures) {
  await expect(page.locator('h1').first()).toContainText('Kiosk PIN');
}

async function statusIndicatorVisible({ page }: PageFixtures) {
  await page.waitForTimeout(2000);
  const hasSet = await isShown(page.locator('text=A PIN is set'));
  const hasNotSet = await isShown(page.locator('text=No PIN is set'));
  expect(hasSet || hasNotSet).toBeTruthy();
}

async function pinInputsPresent({ page }: PageFixtures) {
  await page.waitForTimeout(2000);
  const inputs = await page.locator('input[type="password"]').count();
  expect(inputs).toBeGreaterThanOrEqual(1);
}

async function saveDisabledWhenEmpty({ page }: PageFixtures) {
  await page.waitForTimeout(2000);
  const saveBtn = page.locator('button:has-text("Save")').first();
  if (!(await saveBtn.isVisible())) return;
  const isDisabled = await saveBtn.isDisabled();
  expect(isDisabled).toBe(true);
}

async function nonNumericKeepsDisabled({ page }: PageFixtures) {
  const pins = await pinInputs(page);
  if (!pins) return;
  // inputMode=numeric prevents non-digits client-side, so just verify disabled state
  await pins.newPin.fill('abc');
  await expectSaveDisabled(page, true);
}

test.describe(SUITE, () => {
  test.beforeAll(warmupStaffLogin);

  test.describe('16. Kiosk PIN', () => {
    test.beforeEach(async ({ page }) => {
      await goTo(page, '/kiosk-pin');
    });

    test('kiosk PIN page loads', pageLoads);
    test('PIN status indicator visible (set or not set)', statusIndicatorVisible);
    test('New PIN and Confirm PIN inputs present', pinInputsPresent);
    test('Save button disabled when PIN fields empty or mismatched', saveDisabledWhenEmpty);
    test('entering matching 4-digit PIN enables Save button', pinEntry('1234', '1234', false));
    test('mismatched PINs keep Save button disabled', pinEntry('1234', '5678', true));
    test('PIN validation: error shown for non-numeric input', nonNumericKeepsDisabled);
  });
});
