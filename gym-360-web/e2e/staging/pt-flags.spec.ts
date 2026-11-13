import { test, expect, type Page } from '@playwright/test';
import { uiLogin, ADMIN } from './helpers';

/**
 * Validation_1_PrivateSessions — deployed staff web + admin dashboard with a pt_module gym.
 * PT_STAFF_EMAIL / PT_STAFF_PASSWORD = a desk (manager) of a pt_module gym; PT_GYM_ID = that gym.
 * Run: npx playwright test -c playwright.staging-pt.config.ts
 */
const STAFF_URL = process.env.STAGING_STAFF || 'https://staging-staff.gym-365.com';
const ADMIN_URL = process.env.STAGING_ADMIN || 'https://staging-admin.gym-365.com';
const DESK = { email: process.env.PT_STAFF_EMAIL ?? '', password: process.env.PT_STAFF_PASSWORD ?? 'Test@1234' };
const GYM_ID = process.env.PT_GYM_ID ?? '';

const OFF_ROUTES = ['/classes', '/exercises', '/messages', '/billing', '/gamification', '/announcements', '/analytics', '/check-ins', '/kiosk-pin', '/data-import'];
const ON_ROUTES = ['/', '/members', '/trainers', '/pt/trials', '/pt/review', '/pt/dashboard', '/staff', '/settings'];

async function sidebarHrefs(page: Page): Promise<string[]> {
  await expect(page.locator('aside a[href="/members"], nav a[href="/members"]').first()).toBeVisible({ timeout: 30000 });
  return page.locator('aside a[href^="/"], nav a[href^="/"]').evaluateAll((els) => els.map((e) => e.getAttribute('href') ?? ''));
}

const switches = (page: Page) => page.locator('[role="switch"], input[type="checkbox"]');

test.describe('staff web — pt_module gym', () => {
  test.skip(!DESK.email, 'PT_STAFF_EMAIL not set');
  test.use({ baseURL: STAFF_URL });

  test.beforeEach(async ({ page }) => {
    await uiLogin(page, DESK);
  });

  test('sidebar shows only always-on + PT items', async ({ page }) => {
    const hrefs = await sidebarHrefs(page);
    for (const r of ON_ROUTES) expect(hrefs, `nav has ${r}`).toContain(r);
    for (const r of OFF_ROUTES) expect(hrefs, `nav hides ${r}`).not.toContain(r);
  });

  test('dashboard is PT-focused (desk cards, no revenue/check-ins)', async ({ page }) => {
    await page.goto(`${STAFF_URL}/`);
    await expect(page.locator('main a[href="/pt/trials"]').first()).toBeVisible({ timeout: 30000 });
    await expect(page.getByText('Monthly Revenue')).toHaveCount(0);
    await expect(page.getByText("Today's Check-ins")).toHaveCount(0);
  });

  for (const route of OFF_ROUTES) {
    test(`deep link ${route} is sent back to the dashboard`, async ({ page }) => {
      await page.goto(`${STAFF_URL}${route}`);
      await expect(page).toHaveURL(new RegExp(`${STAFF_URL.replace(/[.]/g, '\\.')}/?$`), { timeout: 30000 });
    });
  }

  test('PT desk pages render', async ({ page }) => {
    for (const route of ['/pt/trials', '/pt/review', '/pt/dashboard']) {
      await page.goto(`${STAFF_URL}${route}`);
      await expect(page).toHaveURL(new RegExp(route));
      await expect(page.locator('h1').first()).toBeVisible({ timeout: 30000 });
    }
  });
});

test.describe('admin dashboard — Feature Flags (super admin)', () => {
  test.use({ baseURL: ADMIN_URL });

  test.beforeEach(async ({ page }) => {
    await uiLogin(page, ADMIN);
  });

  test('menu item, gyms list, gym page, presets and audit render', async ({ page }) => {
    await expect(page.locator('a[href="/feature-flags"]').first()).toBeVisible({ timeout: 30000 });
    // Arabic is the default locale; assert on structure, not on English text.
    await page.goto(`${ADMIN_URL}/feature-flags`);
    await expect(page.locator('table tbody tr').first()).toBeVisible({ timeout: 30000 });
    await expect(page.locator('select option')).toHaveCount(3); // all + business + pt_module
    if (GYM_ID) {
      await page.goto(`${ADMIN_URL}/feature-flags/gyms/${GYM_ID}`);
      await expect(switches(page).nth(20)).toBeAttached({ timeout: 30000 });
    }
    await page.goto(`${ADMIN_URL}/feature-flags/presets`);
    await expect(switches(page).nth(20)).toBeAttached({ timeout: 30000 });
    await page.goto(`${ADMIN_URL}/feature-flags/audit`);
    await expect(page.locator('table tbody tr').first()).toBeVisible({ timeout: 30000 });
  });
});
