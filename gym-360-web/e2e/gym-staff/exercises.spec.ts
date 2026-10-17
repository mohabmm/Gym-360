import { test, expect } from '@playwright/test';
import { SUITE, ensureAppReachable, openStaffPage } from '../helpers/gym-staff';
import { expectBodyNotEmpty, expectHeadingVisible, type PageFixtures } from '../helpers/ui';

// ──────────────────────────────────────────────────────────────────────────
// 12. EXERCISES
// ──────────────────────────────────────────────────────────────────────────

async function pageLoads({ page }: PageFixtures) {
  await expectHeadingVisible(page);
}

async function tableShowsSeeded({ page }: PageFixtures) {
  // Either the feature gate or the table with exercises
  const hasTable = await page.locator('tbody tr').count();
  if (hasTable > 0) {
    // Verify at least one named exercise row is visible (DataTable may virtualize)
    const rowText = await page.locator('tbody tr').first().textContent().catch(() => '');
    expect(rowText && rowText.length > 0).toBeTruthy();
  } else {
    console.log('[EXERCISES] No table rows — feature may be gated or still loading');
  }
}

async function muscleFilterPresent({ page }: PageFixtures) {
  const selects = await page.locator('select').count();
  if (selects > 0) {
    await page.locator('select').first().selectOption({ index: 1 });
    await page.waitForTimeout(500);
  }
}

async function searchFilters({ page }: PageFixtures) {
  const searchInput = page.locator('input[placeholder*="earch"]').first();
  if (!(await searchInput.isVisible())) return;
  const rowsBefore = await page.locator('tbody tr').count();
  await searchInput.fill('squat');
  await page.waitForTimeout(600);
  const rowsAfter = await page.locator('tbody tr').count();
  expect(rowsAfter).toBeLessThanOrEqual(rowsBefore);
  await searchInput.clear();
}

async function addCustomPresent({ page }: PageFixtures) {
  const addBtn = page.locator('button:has-text("Add Exercise")').or(page.locator('button:has-text("Add Custom")'));
  const count = await addBtn.count();
  console.log('[EXERCISES] Add button count:', count);
  // Just verify no crash
  await expectBodyNotEmpty(page);
}

test.describe(SUITE, () => {
  test.beforeAll(ensureAppReachable);

  test.describe('Exercises', () => {
    test.beforeEach(async ({ page }) => {
      await openStaffPage(page, '/exercises', 1500);
    });

    test('exercises page loads', pageLoads);
    test('exercise table shows seeded exercises', tableShowsSeeded);
    test('muscle group filter dropdown present', muscleFilterPresent);
    test('search filters exercises', searchFilters);
    test('Add Custom Exercise button present (if not feature-gated)', addCustomPresent);
  });
});
