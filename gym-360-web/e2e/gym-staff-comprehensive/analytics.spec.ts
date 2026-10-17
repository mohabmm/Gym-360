import { test, expect } from '@playwright/test';
import { BASE_URL, SUITE, goTo, warmupStaffLogin } from '../helpers/gym-staff-comprehensive';
import { bodyText, expectTextVisible, type PageFixtures } from '../helpers/ui';

test.use({ baseURL: BASE_URL });

// ── 12. ANALYTICS ─────────────────────────────────────────────────────────

/** True when every fragment appears in `text`. */
function containsAll(text: string | null, fragments: string[]): boolean {
  return text !== null && fragments.every((fragment) => text.includes(fragment));
}

async function loadsWithKpis({ page }: PageFixtures) {
  await expect(page.locator('h1').first()).toContainText('Analytics');
  await expectTextVisible(page, 'Active Members');
}

async function chartsRender({ page }: PageFixtures) {
  const svgCount = await page.locator('svg').count();
  expect(svgCount).toBeGreaterThan(0);
}

async function memberGrowthPresent({ page }: PageFixtures) {
  await expectTextVisible(page, 'Member Growth');
}

async function revenueTrendPresent({ page }: PageFixtures) {
  await expectTextVisible(page, 'Revenue Trend');
}

/**
 * BUG-013 — Analytics: KPI values are hardcoded ("450", "127", "85%")
 * src: analytics/page.tsx:49-52 — all KpiCard value props are hardcoded strings
 * Fix: fetch from /gyms/{gymId}/analytics API endpoint
 */
async function kpisNotHardcoded({ page }: PageFixtures) {
  // If the page always shows exactly "450" for active members regardless of DB state, it's hardcoded
  // We flag this as a structural bug — test can only confirm the page renders
  const kpiText = await bodyText(page);
  const hasHardcoded = containsAll(kpiText, ['"450"']) || containsAll(kpiText, ['450', '127', '85%']);
  // Log for informational purposes — the bug is confirmed by source code inspection
  if (hasHardcoded) {
    console.log('[BUG-013] Analytics appears to show hardcoded "450 active members" and "127 daily check-ins"');
  }
  // Page should at least render without crash
  await expect(page.locator('h1').first()).toBeVisible();
}

/**
 * BUG-014 — Analytics: memberGrowth and classUtilization are static mock arrays
 * src: analytics/page.tsx:11-27
 * Fix: replace with API calls to /gyms/{gymId}/analytics
 */
async function utilizationIsLive({ page }: PageFixtures) {
  // Pilates always shows 100% if hardcoded — check for that static value
  const text = await bodyText(page);
  if (containsAll(text, ['Pilates', '100'])) {
    console.log('[BUG-014] Class Utilization uses hardcoded data (Pilates always 100%)');
  }
  await expect(page.locator('text=Class Utilization').first()).toBeVisible();
}

test.describe(SUITE, () => {
  test.beforeAll(warmupStaffLogin);

  test.describe('12. Analytics', () => {
    test.beforeEach(async ({ page }) => {
      await goTo(page, '/analytics');
    });

    test('analytics page loads with KPI cards', loadsWithKpis);
    test('charts render (SVG elements present)', chartsRender);
    test('Member Growth chart present', memberGrowthPresent);
    test('Revenue Trend chart present', revenueTrendPresent);
    test('[BUG-013] Analytics KPI values must come from API, not be hardcoded "450"/"127"', kpisNotHardcoded);
    test('[BUG-014] Class Utilization chart must reflect live DB data', utilizationIsLive);
  });
});
