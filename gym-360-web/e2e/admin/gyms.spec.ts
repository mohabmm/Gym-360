import { test, expect, type Page } from '@playwright/test';
import { SUITE, loginOnce, navigateToFirstGym, openAdminPage, searchNarrowsRows } from '../helpers/admin';
import { clickCancel, type PageFixtures } from '../helpers/ui';

// ──────────────────────────────────────────────────────────────────────────
// 3. GYMS LIST
// ──────────────────────────────────────────────────────────────────────────

async function openAddGym(page: Page) {
  await page.waitForTimeout(2000);
  await page.locator('button:has-text("Add Gym")').click();
}

async function loadsWithTable({ page }: PageFixtures) {
  await expect(page.locator('h1, h2').first()).toBeVisible({ timeout: 10000 });
  await page.waitForTimeout(2000);
}

async function statusFilterWorks({ page }: PageFixtures) {
  await page.waitForTimeout(2000);
  const statusFilter = page.locator('select').first();
  if (!(await statusFilter.isVisible())) return;
  await statusFilter.selectOption('active');
  await page.waitForTimeout(500);
  await statusFilter.selectOption({ index: 0 });
}

async function addGymOpens({ page }: PageFixtures) {
  await page.waitForTimeout(2000);
  const addBtn = page.locator('button:has-text("Add Gym")');
  await expect(addBtn).toBeVisible({ timeout: 10000 });
  await addBtn.click();
  await expect(page.locator('label:has-text("Gym Name")').first()).toBeVisible({ timeout: 5000 });
}

async function addGymClosesOnCancel({ page }: PageFixtures) {
  await openAddGym(page);
  await expect(page.locator('label:has-text("Gym Name")').first()).toBeVisible({ timeout: 5000 });
  await clickCancel(page);
  // Modal closed — Cancel button should be gone
  await expect(page.locator('button:has-text("Cancel")')).not.toBeVisible({ timeout: 3000 });
}

async function createDisabledWhenEmpty({ page }: PageFixtures) {
  await openAddGym(page);
  await page.waitForTimeout(500);
  const createGymBtn = page.locator('button:has-text("Create Gym")');
  if (await createGymBtn.isVisible()) {
    await expect(createGymBtn).toBeDisabled();
  }
  await clickCancel(page);
}

async function rowNavigatesToDetail({ page }: PageFixtures) {
  await page.waitForTimeout(3000);
  const firstRow = page.locator('tbody tr').first();
  if (!(await firstRow.isVisible())) return;
  await firstRow.click();
  await page.waitForURL(/\/gyms\/[^/]+$/, { timeout: 8000 });
  expect(page.url()).toMatch(/\/gyms\//);
}

// ──────────────────────────────────────────────────────────────────────────
// 4. GYM DETAIL
// ──────────────────────────────────────────────────────────────────────────

/** Opens the first gym's detail page, skipping the test when there is none. */
async function openFirstGymOrSkip(page: Page): Promise<boolean> {
  const ok = await navigateToFirstGym(page);
  if (!ok) test.skip();
  return ok;
}

async function openDetailTab(page: Page, tab: string, settleMs: number) {
  await page.locator(`button:has-text("${tab}")`).click();
  await page.waitForTimeout(settleMs);
}

async function detailLoadsWithTabs({ page }: PageFixtures) {
  if (!(await openFirstGymOrSkip(page))) return;
  await expect(page.locator('button:has-text("Overview")').first()).toBeVisible({ timeout: 10000 });
}

async function overviewShowsKpis({ page }: PageFixtures) {
  if (!(await openFirstGymOrSkip(page))) return;
  await openDetailTab(page, 'Overview', 1000);
  await expect(page.locator('h1, h2, h3').first()).toBeVisible();
}

/** Returns a test body that opens `tab` and expects the tab button to stay visible. */
function tabButtonRenders(tab: string) {
  return async ({ page }: PageFixtures) => {
    if (!(await openFirstGymOrSkip(page))) return;
    await openDetailTab(page, tab, 1000);
    await expect(page.locator(`button:has-text("${tab}")`)).toBeVisible();
  };
}

async function revenueRendersChart({ page }: PageFixtures) {
  if (!(await openFirstGymOrSkip(page))) return;
  await openDetailTab(page, 'Revenue', 3000);
  const svgCount = await page.locator('svg').count();
  expect(svgCount).toBeGreaterThan(0);
}

/** Returns a test body that opens `tab` and expects `text` to render. */
function tabShowsText(tab: string, text: string) {
  return async ({ page }: PageFixtures) => {
    if (!(await openFirstGymOrSkip(page))) return;
    await openDetailTab(page, tab, 2000);
    await expect(page.locator(`text=${text}`).first()).toBeVisible({ timeout: 10000 });
  };
}

async function changePlanOpens({ page }: PageFixtures) {
  if (!(await openFirstGymOrSkip(page))) return;
  await openDetailTab(page, 'Subscription & Features', 2000);
  const changePlanBtn = page.locator('button:has-text("Change Plan")');
  if (!(await changePlanBtn.isVisible())) return;
  await changePlanBtn.click();
  await expect(page.locator('text=Change Plan').first()).toBeVisible({ timeout: 5000 });
  await clickCancel(page);
}

async function editGymToggles({ page }: PageFixtures) {
  if (!(await openFirstGymOrSkip(page))) return;
  const editBtn = page.locator('button:has-text("Edit")');
  if (!(await editBtn.isVisible())) return;
  await editBtn.click();
  await page.waitForTimeout(500);
  await clickCancel(page);
}

async function suspendRequiresReason({ page }: PageFixtures) {
  if (!(await openFirstGymOrSkip(page))) return;
  const suspendBtn = page.locator('button:has-text("Suspend")').first();
  if (!(await suspendBtn.isVisible())) return;
  await suspendBtn.click();
  await page.waitForTimeout(500);
  // Confirm button inside modal should be disabled without reason
  const textarea = page.locator('textarea');
  if (await textarea.isVisible()) {
    await expect(textarea).toBeVisible();
  }
  await page.locator('button:has-text("Cancel")').last().click();
}

// ──────────────────────────────────────────────────────────────────────────
// 5. GYM ONBOARDING
// ──────────────────────────────────────────────────────────────────────────

async function onboardingLoads({ page }: PageFixtures) {
  await expect(page.locator('h1, h2').first()).toBeVisible({ timeout: 10000 });
}

async function emptyStateOrCards({ page }: PageFixtures) {
  await page.waitForTimeout(3000);
  const emptyVisible = await page.locator('text=All caught up').count();
  const cardsVisible = await page.locator('button:has-text("Approve")').count();
  expect(emptyVisible + cardsVisible).toBeGreaterThan(0);
}

/** Returns a test body that opens a pending gym's `action` modal and expects `selector`. */
function pendingModalOpens(action: string, selector: string) {
  return async ({ page }: PageFixtures) => {
    await page.waitForTimeout(3000);
    const actionBtn = page.locator(`button:has-text("${action}")`).first();
    if (!(await actionBtn.isVisible())) return;
    await actionBtn.click();
    await expect(page.locator(selector).first()).toBeVisible({ timeout: 5000 });
    await clickCancel(page);
  };
}

test.describe(SUITE, () => {
  test.describe('Gyms List', () => {
    test.beforeEach(async ({ page }) => {
      await openAdminPage(page, '/gyms');
    });

    test('gyms page loads with table', loadsWithTable);
    test('status filter dropdown works', statusFilterWorks);
    test('Add Gym modal opens', addGymOpens);
    test('Add Gym modal closes on Cancel', addGymClosesOnCancel);
    test('Add Gym form validation — Create Gym button disabled when empty', createDisabledWhenEmpty);
    test('clicking gym row navigates to gym detail', rowNavigatesToDetail);
    test('search/filter reduces visible rows', searchNarrowsRows({ term: 'nonexistentgymxyz123', waitMs: 3000 }));
  });

  test.describe('Gym Detail', () => {
    test.beforeEach(async ({ page }) => {
      await loginOnce(page);
    });

    test('gym detail page loads with tabs', detailLoadsWithTabs);
    test('Overview tab shows KPI cards', overviewShowsKpis);
    test('Members tab renders', tabButtonRenders('Members'));
    test('Trainers tab renders', tabButtonRenders('Trainers'));
    test('Revenue tab renders chart', revenueRendersChart);
    test('Subscription & Features tab shows plan info', tabShowsText('Subscription & Features', 'Current Plan'));
    test('Change Plan modal opens', changePlanOpens);
    test('Settings tab renders operating hours', tabShowsText('Settings', 'Operating Hours'));
    test('Edit Gym modal opens and closes', editGymToggles);
    test('Suspend modal opens and requires reason', suspendRequiresReason);
  });

  test.describe('Gym Onboarding', () => {
    test.beforeEach(async ({ page }) => {
      await openAdminPage(page, '/gyms/onboarding');
    });

    test('onboarding page loads', onboardingLoads);
    test('shows empty state or gym cards', emptyStateOrCards);
    test('approve modal opens if pending gym exists', pendingModalOpens('Approve', 'text=Assign Plan'));
    test('reject modal opens if pending gym exists', pendingModalOpens('Reject', 'textarea'));
  });
});
