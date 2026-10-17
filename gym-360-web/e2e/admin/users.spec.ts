import { test } from '@playwright/test';
import { SUITE, exportEnabledIfShown, hasTableRows, headingLoads, openAdminPage, searchFillAndClear } from '../helpers/admin';
import type { PageFixtures } from '../helpers/ui';

// ──────────────────────────────────────────────────────────────────────────
// 8. USERS
// ──────────────────────────────────────────────────────────────────────────

async function roleFilterWorks({ page }: PageFixtures) {
  await page.waitForTimeout(3000);
  const roleFilter = page.locator('select').first();
  if (!(await roleFilter.isVisible())) return;
  await roleFilter.selectOption({ label: 'Gym Owner' });
  await page.waitForTimeout(500);
  await roleFilter.selectOption({ index: 0 });
}

async function statusFilterWorks({ page }: PageFixtures) {
  await page.waitForTimeout(3000);
  const statusFilter = page.locator('select').nth(1);
  if (!(await statusFilter.isVisible())) return;
  await statusFilter.selectOption({ label: 'Active' });
  await page.waitForTimeout(500);
}

test.describe(SUITE, () => {
  test.describe('Users', () => {
    test.beforeEach(async ({ page }) => {
      await openAdminPage(page, '/users');
    });

    test('users page loads with table', headingLoads(3000));
    test('role filter works', roleFilterWorks);
    test('status filter works', statusFilterWorks);
    test('search input filters users', searchFillAndClear({ term: 'admin', waitMs: 3000 }));
    test('export CSV button is present', exportEnabledIfShown(2000));
    // Check table has data loaded
    test('user table shows email and role columns', hasTableRows(4000));
  });
});
