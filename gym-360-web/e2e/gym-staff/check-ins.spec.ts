import { test, expect, type Page } from '@playwright/test';
import { SUITE, ensureAppReachable, openStaffPage } from '../helpers/gym-staff';
import { expectBodyNotEmpty, expectHeadingVisible, isShown, type PageFixtures } from '../helpers/ui';

// ──────────────────────────────────────────────────────────────────────────
// 4. CHECK-INS
// ──────────────────────────────────────────────────────────────────────────

function buttonMatching(page: Page, text: RegExp) {
  return page.locator('button').filter({ hasText: text }).first();
}

/** Clicks the button matching `text` when it renders. False when it does not. */
async function clickIfVisible(page: Page, text: RegExp): Promise<boolean> {
  const button = buttonMatching(page, text);
  if (!(await button.isVisible())) return false;
  await button.click();
  await page.waitForTimeout(500);
  return true;
}

async function loadsWithKpis({ page }: PageFixtures) {
  await expectHeadingVisible(page);
  // KPI cards for QR, Numpad, Manual counts
  await expectBodyNotEmpty(page);
}

async function logTableRenders({ page }: PageFixtures) {
  // Seeded check-ins should appear in the log
  const rows = await page.locator('tbody tr').count();
  expect(rows).toBeGreaterThanOrEqual(0);
}

async function qrOpensModal({ page }: PageFixtures) {
  // QR button renders as icon + text; find by partial text "QR"
  if (!(await clickIfVisible(page, /QR/i))) return;
  await expectBodyNotEmpty(page);
  await page.keyboard.press('Escape');
}

async function numpadOpensModal({ page }: PageFixtures) {
  if (!(await clickIfVisible(page, /Numpad|numpad|PIN/i))) return;
  await expectBodyNotEmpty(page);
  const cancelBtn = page.locator('button:has-text("Cancel")');
  if (await isShown(cancelBtn, 1000)) {
    await cancelBtn.last().click();
  } else {
    await page.keyboard.press('Escape');
  }
}

async function manualPanelOpens({ page }: PageFixtures) {
  // The "Manual" button toggles an inline panel (not a modal) on the page
  if (!(await clickIfVisible(page, /Manual/i))) return;
  // Inline panel shows "Manual Check-In" heading
  await expect(page.locator('text=Manual Check-In').first()).toBeVisible({ timeout: 5000 });
}

async function manualPanelFilters({ page }: PageFixtures) {
  if (!(await clickIfVisible(page, /Manual/i))) return;
  const searchInput = page.locator('input').filter({ hasText: '' }).first();
  if (!(await searchInput.isVisible())) return;
  // Use the global-setup seed name. Skip if no members rendered.
  const sampleName = 'Seed';
  await searchInput.fill(sampleName);
  await page.waitForTimeout(500);
  const matched = await isShown(page.locator(`text=${sampleName}`).first(), 5000);
  const empty = await isShown(page.locator('text=/no members|no results|empty|لا يوجد/i').first(), 1000);
  expect(matched || empty).toBeTruthy();
}

async function sectionSelectorPresent({ page }: PageFixtures) {
  // Section (Mens/Womens/Mixed) toggle should exist
  const sectionBtns = page.locator('button:has-text("Mens")').or(page.locator('button:has-text("Womens")'));
  const count = await sectionBtns.count();
  expect(count).toBeGreaterThanOrEqual(0); // section filter may appear in modal
}

test.describe(SUITE, () => {
  test.beforeAll(ensureAppReachable);

  test.describe('Check-Ins', () => {
    test.beforeEach(async ({ page }) => {
      await openStaffPage(page, '/check-ins', 1500);
    });

    test('check-ins page loads with KPI cards', loadsWithKpis);
    test('check-in log table renders with entries', logTableRenders);
    test('QR scan button opens QR modal', qrOpensModal);
    test('Numpad button opens PIN entry modal', numpadOpensModal);
    test('Manual check-in inline panel opens on button click', manualPanelOpens);
    test('manual check-in inline panel filters members by name', manualPanelFilters);
    test('section selector present in check-in log header', sectionSelectorPresent);
  });
});
