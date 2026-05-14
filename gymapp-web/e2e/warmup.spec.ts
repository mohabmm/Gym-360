/**
 * Warm-up: navigates to each admin page once so Next.js compiles them
 * before the real test sweep. Reduces first-hit timeouts on cold dev server.
 */
import { test } from '@playwright/test';

const PAGES = [
  '/',
  '/gyms',
  '/users',
  '/analytics',
  '/analytics/churn',
  '/plans',
  '/subscriptions',
  '/content',
  '/support',
  '/reports',
  '/settings',
  '/gyms/onboarding',
];

test.describe.configure({ mode: 'serial' });

test('warm-up admin pages', async ({ page }) => {
  test.setTimeout(360_000);
  for (const p of PAGES) {
    await page.goto(p, { waitUntil: 'domcontentloaded', timeout: 90_000 }).catch(() => {});
    await page.waitForTimeout(500);
  }
});
