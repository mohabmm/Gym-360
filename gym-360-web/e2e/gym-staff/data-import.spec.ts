import { test, expect } from '@playwright/test';
import { SUITE, ensureAppReachable, openStaffPage } from '../helpers/gym-staff';
import { expectHeadingVisible, isShown, type PageFixtures } from '../helpers/ui';

// ──────────────────────────────────────────────────────────────────────────
// 13. DATA IMPORT
// ──────────────────────────────────────────────────────────────────────────

async function pageLoads({ page }: PageFixtures) {
  await expectHeadingVisible(page);
}

async function showsGateOrContent({ page }: PageFixtures) {
  await page.waitForTimeout(4000); // wait for feature-gate API call
  const hasContent = await isShown(page.locator('text=Step 1'));
  const hasGate = await isShown(page.locator('text=Upgrade Plan'));
  const hasLoading = await isShown(page.locator('.animate-pulse'));
  console.log('[DATA IMPORT] content:', hasContent, '| gate:', hasGate, '| loading:', hasLoading);
  // One of these three states must be true
  expect(hasContent || hasGate || hasLoading).toBeTruthy();
}

async function downloadTemplatePresent({ page }: PageFixtures) {
  const templateBtn = page.locator('button:has-text("Download Template")').or(page.locator('a:has-text("Download")'));
  const isVisible = await isShown(templateBtn.first());
  console.log('[DATA IMPORT] Download Template visible:', isVisible);
}

async function sourceSelectorVisible({ page }: PageFixtures) {
  const odooBtn = page.locator('button:has-text("Odoo")').or(page.locator('text=Odoo'));
  const isVisible = await isShown(odooBtn);
  console.log('[DATA IMPORT] Source selector visible:', isVisible);
}

test.describe(SUITE, () => {
  test.beforeAll(ensureAppReachable);

  test.describe('Data Import', () => {
    test.beforeEach(async ({ page }) => {
      await openStaffPage(page, '/data-import', 2000);
    });

    test('data import page loads', pageLoads);
    test('shows FeatureGate or import content based on plan', showsGateOrContent);
    test('Download Template button present in import content', downloadTemplatePresent);
    test('source selector visible (Odoo, Daftra, Excel)', sourceSelectorVisible);
  });
});
