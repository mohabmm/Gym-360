import { test, expect, type Page } from '@playwright/test';
import { SUITE, openAdminPage } from '../helpers/admin';
import { clickCancel, isShown, type PageFixtures } from '../helpers/ui';

// ──────────────────────────────────────────────────────────────────────────
// 14. SETTINGS
// ──────────────────────────────────────────────────────────────────────────

async function openTab(page: Page, tab: string, settleMs: number) {
  await page.waitForTimeout(2000);
  await page.locator(`button:has-text("${tab}")`).click();
  await page.waitForTimeout(settleMs);
}

async function loadsWithTabs({ page }: PageFixtures) {
  await page.waitForTimeout(2000);
  await expect(page.locator('h1, h2').first()).toBeVisible({ timeout: 10000 });
  await expect(page.locator('button:has-text("Platform Config")').first()).toBeVisible({ timeout: 10000 });
}

/** Returns a test body that opens `tab` and expects `text` within 5s. */
function tabShowsText(tab: string, text: string) {
  return async ({ page }: PageFixtures) => {
    await openTab(page, tab, 500);
    await expect(page.locator(`text=${text}`).first()).toBeVisible({ timeout: 5000 });
  };
}

async function adminTeamNoCrash({ page }: PageFixtures) {
  await openTab(page, 'Admin Team', 2000);
  // Should NOT show Unhandled Runtime Error overlay
  const hasError = await isShown(page.locator('text=Unhandled Runtime Error'));
  expect(hasError).toBe(false);
  // Should show admin table
  await expect(page.locator('text=Add Admin').first()).toBeVisible({ timeout: 5000 });
}

async function addAdminOpens({ page }: PageFixtures) {
  await openTab(page, 'Admin Team', 2000);
  const addAdminBtn = page.locator('button:has-text("Add Admin")');
  if (!(await addAdminBtn.isVisible())) return;
  await addAdminBtn.click();
  await expect(page.locator('text=First Name').first()).toBeVisible({ timeout: 5000 });
  await clickCancel(page);
}

async function savePlatformConfig({ page }: PageFixtures) {
  await openTab(page, 'Platform Config', 500);
  const saveBtn = page.locator('button:has-text("Save Changes")');
  if (!(await saveBtn.isVisible())) return;
  await saveBtn.click();
  await page.waitForTimeout(2000);
  await expect(page).toHaveURL('/settings');
}

test.describe(SUITE, () => {
  test.describe('Settings', () => {
    test.beforeEach(async ({ page }) => {
      await openAdminPage(page, '/settings');
    });

    test('settings page loads with tabs', loadsWithTabs);
    test('Platform Config tab shows form fields', tabShowsText('Platform Config', 'Platform Name'));
    test('Admin Team tab shows admin list without crash', adminTeamNoCrash);
    test('Add Admin modal opens', addAdminOpens);
    test('Security tab shows toggles', tabShowsText('Security', 'Two-Factor'));
    test('PDPL tab shows compliance section', tabShowsText('PDPL Compliance', 'Consent'));
    test('save platform config does not crash', savePlatformConfig);
  });
});
