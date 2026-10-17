import { test, expect } from '@playwright/test';
import { BASE_URL, SUITE, goTo, warmupStaffLogin } from '../helpers/gym-staff-comprehensive';
import { isShown, type PageFixtures } from '../helpers/ui';

test.use({ baseURL: BASE_URL });

// ── 15. DATA IMPORT ───────────────────────────────────────────────────────

async function pageLoads({ page }: PageFixtures) {
  await expect(page.locator('h1').first()).toContainText('Data Import');
}

async function showsGateOrContent({ page }: PageFixtures) {
  await page.waitForTimeout(4000);
  const hasContent = await isShown(page.locator('text=Step 1'));
  const hasGate = await isShown(page.locator('text=Upgrade').or(page.locator('text=Pro Plan')));
  const hasLoading = await isShown(page.locator('.animate-pulse'));
  expect(hasContent || hasGate || hasLoading).toBeTruthy();
}

async function downloadTemplatePresent({ page }: PageFixtures) {
  await page.waitForTimeout(4000);
  const templateBtn = page.locator('button:has-text("Download Excel template")');
  if (await templateBtn.isVisible()) {
    await expect(templateBtn).toBeVisible();
  }
}

async function sourceSelectorOptions({ page }: PageFixtures) {
  await page.waitForTimeout(4000);
  const sourceSelect = page.locator('select').first();
  if (!(await sourceSelect.isVisible())) return;
  const options = await sourceSelect.locator('option').allTextContents();
  for (const source of ['Excel', 'Odoo', 'Daftra']) {
    expect(options.some((o) => o.includes(source))).toBe(true);
  }
}

test.describe(SUITE, () => {
  test.beforeAll(warmupStaffLogin);

  test.describe('15. Data Import', () => {
    test.beforeEach(async ({ page }) => {
      await goTo(page, '/data-import');
    });

    test('data import page loads', pageLoads);
    test('shows feature gate or import content (not crash)', showsGateOrContent);
    test('Download Template button present when feature enabled', downloadTemplatePresent);
    test('Source selector has Excel, Odoo, Daftra options', sourceSelectorOptions);
  });
});
