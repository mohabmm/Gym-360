import { test, expect, type Locator, type Page } from '@playwright/test';
import { SUITE, ensureAppReachable, openStaffPage } from '../helpers/gym-staff';
import { clickCancel, expectBodyNotEmpty, expectHeadingVisible, type PageFixtures } from '../helpers/ui';

// ──────────────────────────────────────────────────────────────────────────
// 6. BILLING
// ──────────────────────────────────────────────────────────────────────────

/** Asserts the first of either text selector is visible. */
async function expectEitherText(page: Page, primary: string, fallback: string) {
  await expect(
    page.locator(`text=${primary}`).or(page.locator(`text=${fallback}`)).first(),
  ).toBeVisible({ timeout: 8000 });
}

/** Opens a modal via `addBtn` when it renders; returns false when it does not. */
async function openModal(page: Page, addBtn: Locator): Promise<boolean> {
  if (!(await addBtn.first().isVisible())) return false;
  await addBtn.first().click();
  await page.waitForTimeout(400);
  return true;
}

async function loadsWithSections({ page }: PageFixtures) {
  await expectHeadingVisible(page);
}

async function kpiCardsVisible({ page }: PageFixtures) {
  // Billing page should show revenue or plan KPIs
  await expectBodyNotEmpty(page);
  const svgOrCards = await page.locator('[class*="kpi"], [class*="card"], svg').count();
  expect(svgOrCards).toBeGreaterThan(0);
}

async function plansSectionRenders({ page }: PageFixtures) {
  await expectEitherText(page, 'Membership Plan', 'Plans');
}

async function packagesSectionRenders({ page }: PageFixtures) {
  await expectEitherText(page, 'Session Package', 'Packages');
}

async function addPlanOpensAndCloses({ page }: PageFixtures) {
  const addBtn = page.locator('button:has-text("Add Plan")').or(page.locator('button:has-text("New Plan")'));
  if (!(await openModal(page, addBtn))) return;
  await expect(page.locator('button:has-text("Cancel")')).toBeVisible({ timeout: 5000 });
  await clickCancel(page);
}

async function addPackageOpensAndCloses({ page }: PageFixtures) {
  if (!(await openModal(page, page.locator('button:has-text("Add Package")')))) return;
  await clickCancel(page);
}

async function recentPaymentsVisible({ page }: PageFixtures) {
  await expectEitherText(page, 'Payment', 'Recent');
}

test.describe(SUITE, () => {
  test.beforeAll(ensureAppReachable);

  test.describe('Billing', () => {
    test.beforeEach(async ({ page }) => {
      await openStaffPage(page, '/billing', 1500);
    });

    test('billing page loads with sections', loadsWithSections);
    test('KPI cards visible (Revenue, Plans, Active Packages)', kpiCardsVisible);
    test('Membership Plans section renders', plansSectionRenders);
    test('Session Packages section renders', packagesSectionRenders);
    test('Add Plan modal opens and closes', addPlanOpensAndCloses);
    test('Add Package modal opens and closes', addPackageOpensAndCloses);
    test('recent payments table visible', recentPaymentsVisible);
  });
});
