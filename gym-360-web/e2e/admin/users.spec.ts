import { expect, test } from '@playwright/test';
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

// Records any status-change / delete call so the confirm dialogs can be proven non-destructive on Cancel.
function recordUserMutations({ page }: PageFixtures): string[] {
  const calls: string[] = [];
  page.on('request', (req) => {
    if (/\/api\/v1\/admin\/users\/[^/]+(\/status)?$/.test(req.url()) && ['PATCH', 'DELETE'].includes(req.method())) {
      calls.push(`${req.method()} ${req.url()}`);
    }
  });
  return calls;
}

async function suspendAsksForConfirmation({ page }: PageFixtures) {
  const calls = recordUserMutations({ page });
  const suspend = page.getByRole('button', { name: /^Suspend / }).first();
  if (!(await suspend.isVisible({ timeout: 4000 }).catch(() => false))) {
    test.skip(true, 'No active user row (other than you) to suspend');
  }
  await suspend.click();
  await expect(page.getByText(/signed out right away on every device/)).toBeVisible();
  await page.getByRole('button', { name: 'Cancel' }).click();
  expect(calls).toEqual([]);
}

async function deleteRequiresTypedEmail({ page }: PageFixtures) {
  const calls = recordUserMutations({ page });
  const del = page.getByRole('button', { name: /^Delete / }).first();
  if (!(await del.isVisible({ timeout: 4000 }).catch(() => false))) {
    test.skip(true, 'No deletable user row');
  }
  await del.click();
  const confirm = page.getByRole('button', { name: 'Delete forever' });
  await expect(confirm).toBeDisabled();
  await page.getByRole('textbox').last().fill('not-the-email@example.com');
  await expect(confirm).toBeDisabled();
  await page.getByRole('button', { name: 'Cancel' }).click();
  expect(calls).toEqual([]);
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
    test('suspend asks for confirmation and cancel sends nothing', suspendAsksForConfirmation);
    test('delete stays disabled until the email is typed', deleteRequiresTypedEmail);
  });
});
