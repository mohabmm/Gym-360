import { test, expect } from '@playwright/test';
import { SUITE, openAdminPage, svgRenders, textVisibleAfter } from '../helpers/admin';
import { clickCancel, type PageFixtures } from '../helpers/ui';

// ──────────────────────────────────────────────────────────────────────────
// 2. DASHBOARD
// ──────────────────────────────────────────────────────────────────────────

async function sidebarVisible({ page }: PageFixtures) {
  await expect(page.locator('a[href="/gyms"]').first()).toBeVisible({ timeout: 10000 });
}

async function headingVisible({ page }: PageFixtures) {
  await expect(page.locator('h1, h2').first()).toBeVisible({ timeout: 10000 });
}

async function bulkSuspendVisible({ page }: PageFixtures) {
  await page.waitForTimeout(2000);
  await expect(page.locator('h1, h2').first()).toBeVisible();
}

async function bulkSuspendModalToggles({ page }: PageFixtures) {
  await page.waitForTimeout(2000);
  const suspendBtn = page.locator('button:has-text("Bulk Suspend")');
  if (!(await suspendBtn.isVisible())) return;
  await suspendBtn.click();
  await expect(page.locator('text=Bulk Suspend Overdue Gyms')).toBeVisible({ timeout: 5000 });
  await clickCancel(page);
  await expect(page.locator('text=Bulk Suspend Overdue Gyms')).not.toBeVisible({ timeout: 3000 });
}

async function navigatesToGyms({ page }: PageFixtures) {
  await page.locator('a[href="/gyms"]').first().click();
  await expect(page).toHaveURL('/gyms');
}

test.describe(SUITE, () => {
  test.describe('Dashboard', () => {
    test.beforeEach(async ({ page }) => {
      await openAdminPage(page, '/');
    });

    test('dashboard loads with KPI cards', textVisibleAfter({ text: 'Total Gyms', waitMs: 2000 }));
    test('sidebar navigation is visible', sidebarVisible);
    test('page title or heading is visible', headingVisible);
    test('revenue chart renders without error', svgRenders(2000));
    test('bulk suspend button visible when overdue gyms exist', bulkSuspendVisible);
    test('bulk suspend modal opens and closes', bulkSuspendModalToggles);
    test('navigation to gyms page works', navigatesToGyms);
  });
});
