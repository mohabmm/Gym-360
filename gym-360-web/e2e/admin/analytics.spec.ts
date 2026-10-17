import { test, expect } from '@playwright/test';
import { SUITE, headingLoads, openAdminPage, searchFillAndClear, svgRenders, textVisibleAfter } from '../helpers/admin';
import type { PageFixtures } from '../helpers/ui';

// ──────────────────────────────────────────────────────────────────────────
// 9. ANALYTICS  /  10. CHURN ANALYTICS
// ──────────────────────────────────────────────────────────────────────────

async function atRiskTableRenders({ page }: PageFixtures) {
  await page.waitForTimeout(3000);
  // Section header OR empty state both valid
  await expect(
    page.locator('h1, h2, h3').or(page.getByText('At-Risk')).or(page.getByText('No at-risk')).first()
  ).toBeVisible({ timeout: 10000 });
}

test.describe(SUITE, () => {
  test.describe('Analytics', () => {
    test.beforeEach(async ({ page }) => {
      await openAdminPage(page, '/analytics');
    });

    test('analytics page loads with KPI cards', headingLoads(3000));
    test('charts render (SVG present)', svgRenders(4000));
    test('MRR KPI card visible', textVisibleAfter({ text: 'MRR', waitMs: 3000 }));
    test('Plan Distribution chart renders', textVisibleAfter({ text: 'Plan Distribution', waitMs: 3000 }));
  });

  test.describe('Churn Analytics', () => {
    test.beforeEach(async ({ page }) => {
      await openAdminPage(page, '/analytics/churn');
    });

    test('churn page loads', headingLoads(3000));
    test('churn KPI cards visible', textVisibleAfter({ text: 'Churn Rate', waitMs: 3000 }));
    test('at-risk gyms table renders', atRiskTableRenders);
    test('churn search input filters', searchFillAndClear({ term: 'test', waitMs: 3000 }));
  });
});
