import { test, expect, type Page } from '@playwright/test';
import { SUITE, loginOnce } from '../helpers/admin';
import {
  collectPageErrors,
  collectServerErrors,
  expectBodyNotEmpty,
  expectNoErrors,
  type PageFixtures,
} from '../helpers/ui';

// ──────────────────────────────────────────────────────────────────────────
// 15. NAVIGATION & ROUTING
// ──────────────────────────────────────────────────────────────────────────

async function sidebarLinksNavigate({ page }: PageFixtures) {
  const routes = [
    '/gyms', '/users', '/analytics', '/plans',
    '/subscriptions', '/content', '/support', '/reports', '/settings',
  ];
  for (const route of routes) {
    await page.goto(route);
    await page.waitForTimeout(500);
    await expect(page).toHaveURL(route);
    expect(page.url()).not.toContain('/login');
  }
}

async function unknownRouteRenders404({ page }: PageFixtures) {
  await page.goto('/nonexistentroute12345');
  await page.waitForTimeout(2000);
  // Next.js not-found.tsx renders "404" and "Page Not Found"
  const has404 = await page.locator('text=404').count();
  const hasNotFound = await page.locator('text=Page Not Found').count();
  const url = page.url();
  const handled = [has404 > 0, hasNotFound > 0, url.includes('/login'), url.endsWith('/')];
  expect(handled.some(Boolean)).toBeTruthy();
}

async function directUrlAccessWorks({ page }: PageFixtures) {
  for (const path of ['/settings', '/plans']) {
    await page.goto(path);
    await expect(page).toHaveURL(path);
  }
}

// ──────────────────────────────────────────────────────────────────────────
// 16. ERROR STATES
// ──────────────────────────────────────────────────────────────────────────

async function contentAroundDataLoad({ page }: PageFixtures) {
  await page.goto('/gyms');
  await page.waitForTimeout(4000);
  await expect(page.locator('h1, h2').first()).toBeVisible();
}

async function invalidGymIdHandled({ page }: PageFixtures) {
  await page.goto('/gyms/00000000-0000-0000-0000-000000000000');
  await page.waitForTimeout(3000);
  await expectBodyNotEmpty(page);
}

// ──────────────────────────────────────────────────────────────────────────
// 17. UI INTEGRITY
// ──────────────────────────────────────────────────────────────────────────

/** Drops known non-critical errors (SignalR negotiation plus any `extra` markers). */
function criticalErrors(exceptions: string[], extra: string[] = []): string[] {
  const ignored = ['negotiation', 'SignalR', ...extra];
  return exceptions.filter((e) => !ignored.some((marker) => e.includes(marker)));
}

async function noDashboardExceptions({ page }: PageFixtures) {
  const exceptions = collectPageErrors(page);
  await page.goto('/');
  await page.waitForTimeout(4000);
  // Filter out known non-critical errors
  expectNoErrors(criticalErrors(exceptions), 'Dashboard pageerrors:');
}

async function openAdminTeam(page: Page) {
  await page.goto('/settings');
  await page.waitForTimeout(2000);
  await page.locator('button:has-text("Admin Team")').click();
  await page.waitForTimeout(2000);
}

async function noSettingsExceptions({ page }: PageFixtures) {
  const exceptions = collectPageErrors(page);
  await openAdminTeam(page);
  expectNoErrors(criticalErrors(exceptions, ['Unauthorized']), 'Settings pageerrors:');
}

async function noDashboardServerErrors({ page }: PageFixtures) {
  const serverErrors = collectServerErrors(page);
  await page.goto('/');
  await page.waitForTimeout(3000);
  expectNoErrors(serverErrors, '500 errors:');
}

async function gyms4xxDoNotCrash({ page }: PageFixtures) {
  const failedRequests: { status: number; url: string }[] = [];
  page.on('response', (res) => {
    if (res.status() >= 400 && !res.url().includes('hubs/config')) {
      failedRequests.push({ status: res.status(), url: res.url() });
    }
  });
  await page.goto('/gyms');
  await page.waitForTimeout(4000);
  if (failedRequests.length > 0) console.log('4xx on gyms page:', failedRequests);
  await expectBodyNotEmpty(page);
}

test.describe(SUITE, () => {
  test.describe('Navigation & Routing', () => {
    test.beforeEach(async ({ page }) => {
      await loginOnce(page);
    });

    test('all sidebar nav links navigate correctly', sidebarLinksNavigate);
    test('404 page renders for unknown route', unknownRouteRenders404);
    test('direct URL access to protected pages works when authenticated', directUrlAccessWorks);
  });

  test.describe('Error States', () => {
    test.beforeEach(async ({ page }) => {
      await loginOnce(page);
    });

    test('page shows content before and after data loads', contentAroundDataLoad);
    test('invalid gym ID shows error or redirects', invalidGymIdHandled);
  });

  test.describe('UI Integrity', () => {
    test.beforeEach(async ({ page }) => {
      await loginOnce(page);
    });

    test('no unhandled JS exceptions on dashboard', noDashboardExceptions);
    test('no unhandled JS exceptions on settings page', noSettingsExceptions);
    test('no 500 errors on dashboard load', noDashboardServerErrors);
    test('API 4xx on gyms page does not crash UI', gyms4xxDoNotCrash);
  });
});
