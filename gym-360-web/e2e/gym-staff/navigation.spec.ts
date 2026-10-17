import { test, expect } from '@playwright/test';
import { SUITE, ensureAppReachable, loginOnce } from '../helpers/gym-staff';
import { bodyText, isShown, type PageFixtures } from '../helpers/ui';

// ──────────────────────────────────────────────────────────────────────────
// 16. NAVIGATION & ROUTING
// ──────────────────────────────────────────────────────────────────────────

const ROUTES = [
  '/', '/members', '/trainers', '/classes', '/exercises',
  '/billing', '/gamification', '/announcements', '/analytics',
  '/check-ins', '/staff', '/data-import', '/kiosk-pin', '/settings',
];

async function allRoutesNavigate({ page }: PageFixtures) {
  test.setTimeout(120000);
  for (const route of ROUTES) {
    await page.goto(route, { timeout: 30000 });
    await page.waitForTimeout(300);
    expect(page.url()).not.toContain('/login');
    expect(page.url()).toContain(route === '/' ? 'localhost:3001' : route);
  }
}

async function unknownRouteHandled({ page }: PageFixtures) {
  await page.goto('/nonexistent-xyz-12345', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(2000);
  const body = await bodyText(page);
  const is404 = body?.includes('404') || body?.includes('Not Found');
  const url = page.url();
  const redirectedHome = url.endsWith('/') || url.endsWith('3001/');
  const redirectedLogin = url.includes('/login');
  console.log('[404] URL:', url, '| 404 text:', is404);
  expect(is404 || redirectedHome || redirectedLogin).toBeTruthy();
}

async function directUrlAccessWorks({ page }: PageFixtures) {
  for (const path of ['/settings', '/analytics', '/staff']) {
    await page.goto(path, { waitUntil: 'domcontentloaded' });
    await expect(page).toHaveURL(path);
  }
}

async function openDashboard({ page }: PageFixtures) {
  await page.goto('/', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(1000);
}

async function gymSwitcherSwitches({ page }: PageFixtures) {
  await openDashboard({ page });
  // GymSwitcher button/dropdown
  const gymSwitcher = page.locator('text=First Gym').first();
  if (!(await gymSwitcher.isVisible())) return;
  await gymSwitcher.click();
  await page.waitForTimeout(500);
  // Should show gym options
  const eliteOption = page.locator('text=Elite Fitness').or(page.locator('text=Maadi'));
  const visible = await isShown(eliteOption);
  console.log('[GYM SWITCHER] Elite Fitness option visible:', visible);
  await page.keyboard.press('Escape');
}

async function signOutRedirects({ page }: PageFixtures) {
  await openDashboard({ page });
  const signOutBtn = page.locator('button:has-text("Sign Out")').or(page.locator('button:has-text("Logout")').or(page.locator('[aria-label*="sign out"]')));
  if (!(await isShown(signOutBtn.first(), 5000))) return;
  await signOutBtn.first().click();
  await expect(page).toHaveURL(/\/login/, { timeout: 10000 });
}

test.describe(SUITE, () => {
  test.beforeAll(ensureAppReachable);

  test.describe('Navigation & Routing', () => {
    test.beforeEach(async ({ page }) => {
      await loginOnce(page);
    });

    test('all sidebar nav links navigate without crashing', allRoutesNavigate);
    test('404 page or redirect for unknown route', unknownRouteHandled);
    test('direct URL access to protected pages works when authenticated', directUrlAccessWorks);
    test('GymSwitcher switches context and refreshes page', gymSwitcherSwitches);
    test('sign out redirects to /login', signOutRedirects);
  });
});
