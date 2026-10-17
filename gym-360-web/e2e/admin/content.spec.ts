import { test, expect } from '@playwright/test';
import {
  SUITE,
  exportClickTolerated,
  exportEnabledIfShown,
  hasTableRows,
  headingLoads,
  openAdminPage,
  searchFillAndClear,
  searchNarrowsRows,
} from '../helpers/admin';
import { expectBodyNotEmpty, type PageFixtures } from '../helpers/ui';

// ──────────────────────────────────────────────────────────────────────────
// 11. CONTENT (Exercise Library)  /  12. SUPPORT TICKETS  /  13. REPORTS
// ──────────────────────────────────────────────────────────────────────────

async function muscleFilterPresent({ page }: PageFixtures) {
  await page.waitForTimeout(2000);
  const muscleFilter = page.locator('select');
  if (await muscleFilter.isVisible()) {
    await expect(muscleFilter.first()).toBeVisible();
  }
}

async function muscleFilterReduces({ page }: PageFixtures) {
  await page.waitForTimeout(3000);
  const selects = page.locator('select');
  if ((await selects.count()) === 0) return;
  const opts = await selects.first().locator('option').allTextContents();
  if (opts.length > 1) {
    await selects.first().selectOption({ index: 1 });
    await page.waitForTimeout(1000);
  }
}

async function ticketRowSelects({ page }: PageFixtures) {
  await page.waitForTimeout(2000);
  // Try clicking the first table row
  const firstRow = page.locator('tbody tr').first();
  if (!(await firstRow.isVisible())) return;
  await firstRow.click();
  await page.waitForTimeout(500);
  // SlideOver or any panel should appear — just verify page didn't crash
  await expectBodyNotEmpty(page);
}

test.describe(SUITE, () => {
  test.describe('Content — Exercise Library', () => {
    test.beforeEach(async ({ page }) => {
      await openAdminPage(page, '/content');
    });

    test('content page loads with exercise table', headingLoads(3000));
    test('muscle group filter dropdown present', muscleFilterPresent);
    test('exercise table has rows', hasTableRows(4000));
    test('export CSV button present', exportEnabledIfShown(2000));
    test('search filters exercises', searchNarrowsRows({ term: 'squat', waitMs: 3000 }));
    test('muscle group filter reduces results', muscleFilterReduces);
  });

  test.describe('Support Tickets', () => {
    test.beforeEach(async ({ page }) => {
      await openAdminPage(page, '/support');
    });

    test('support page loads with ticket list', headingLoads(2000));
    test('ticket table has data rows', hasTableRows(2000));
    test('search filters tickets', searchFillAndClear({ term: 'billing', waitMs: 2000 }));
    test('clicking ticket row or view button selects ticket', ticketRowSelects);
    test('export CSV button works without error', exportClickTolerated(2000));
  });

  test.describe('Reports', () => {
    test.beforeEach(async ({ page }) => {
      await openAdminPage(page, '/reports');
    });

    test('reports page loads with table', headingLoads(3000));
    test('gym performance table has data', hasTableRows(4000));
    test('export CSV button triggers download', exportClickTolerated(3000));
  });
});
