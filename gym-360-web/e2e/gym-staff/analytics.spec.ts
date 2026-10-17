import { test, expect } from '@playwright/test';
import { SUITE, ensureAppReachable, openStaffPage } from '../helpers/gym-staff';
import { expectHeadingVisible, expectTextVisible, type PageFixtures } from '../helpers/ui';

// ──────────────────────────────────────────────────────────────────────────
// 10. ANALYTICS
// ──────────────────────────────────────────────────────────────────────────

async function loadsWithKpis({ page }: PageFixtures) {
  await expectHeadingVisible(page);
}

async function chartsRender({ page }: PageFixtures) {
  const svgCount = await page.locator('svg').count();
  expect(svgCount).toBeGreaterThan(0);
}

async function activeMembersVisible({ page }: PageFixtures) {
  await expectTextVisible(page, 'Active Members');
}

async function monthlyRevenueVisible({ page }: PageFixtures) {
  await expect(page.locator('text=Monthly Revenue').or(page.locator('text=Revenue')).first()).toBeVisible({ timeout: 8000 });
}

async function memberGrowthPresent({ page }: PageFixtures) {
  await expectTextVisible(page, 'Member Growth');
}

// BUG DETECTION: analytics uses hardcoded mock data, not live API
async function showsGymContext({ page }: PageFixtures) {
  await expectTextVisible(page, 'First Gym');
}

test.describe(SUITE, () => {
  test.beforeAll(ensureAppReachable);

  test.describe('Analytics', () => {
    test.beforeEach(async ({ page }) => {
      await openStaffPage(page, '/analytics', 2000);
    });

    test('analytics page loads with KPI cards', loadsWithKpis);
    test('charts render (SVG elements present)', chartsRender);
    test('active members KPI visible', activeMembersVisible);
    test('monthly revenue KPI visible', monthlyRevenueVisible);
    test('member growth chart data present', memberGrowthPresent);
    test('[BUG CHECK] Analytics shows "First Gym" context, not generic placeholder', showsGymContext);
  });
});
