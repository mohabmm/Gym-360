/**
 * Staff app auth.setup: login once, save storageState. All gym-staff tests
 * reuse this state so we avoid the per-test 45s login race that crushes
 * the suite under parallel workers.
 *
 * Reads creds from `test_logs/loop_<LOOP_N>_creds.json` (default loop 1) so the
 * fresh-DB Phase 0/1 bootstrap drives auth instead of legacy demo.local seed.
 */
import { test as setup, expect } from '@playwright/test';
import { mkdirSync, readFileSync, existsSync } from 'fs';
import path from 'path';

const authFile = path.join(__dirname, '.auth/staff.json');

setup.use({ baseURL: 'http://localhost:3001', storageState: { cookies: [], origins: [] } });

// Staging API (https://staging-api.gym-365.com) is materially slower than local.
// The config-level test timeout is 60s; an inner waitForURL of 60s can therefore
// never report (the test dies first), which is the root of the 60s flake. Give
// this setup its own 2-minute budget so the (now 45s) post-login wait fits
// inside the test.
setup.setTimeout(120_000);

setup('staff login', async ({ page }) => {
  mkdirSync(path.dirname(authFile), { recursive: true });

  // Only honor loop creds when LOOP_N explicitly set; otherwise stale JSON
  // from a prior bootstrap run blocks the whole suite.
  const loopN = process.env.LOOP_N;
  let email = 'staff@demo.local';
  let password = '123456';
  if (loopN) {
    const credsPath = path.resolve(__dirname, `../../test_logs/loop_${loopN}_creds.json`);
    if (existsSync(credsPath)) {
      const c = JSON.parse(readFileSync(credsPath, 'utf8'));
      email = c.owner?.email ?? email;
      password = c.owner?.password ?? password;
    }
  }

  await page.goto('/login', { waitUntil: 'domcontentloaded' });
  await page.fill('input[type="email"]', email);
  await page.fill('input[type="password"]', password);
  await page.click('button[type="submit"]');
  // 45s (< the 120s test budget) accommodates staging latency while still
  // leaving room for the post-login assertion to report a real failure.
  await page.waitForURL((u) => !u.pathname.startsWith('/login'), { timeout: 45000 });
  // After login, owner may be redirected to /select-gym; pick the first gym tile.
  if (page.url().includes('/select-gym')) {
    await page.locator('button, a').first().click({ timeout: 5000 }).catch(() => {});
    await page.waitForURL((u) => !u.pathname.startsWith('/select-gym'), { timeout: 15000 }).catch(() => {});
  }
  await expect(page.locator('h1, h2').first()).toBeVisible({ timeout: 15000 });

  // Guard against the silent-auth-failure cascade: a NextAuth session must
  // exist before we persist storageState. A landed-on-dashboard URL alone is
  // not proof of auth (an unauthenticated SPA shell can still render an
  // <h1>), so assert the actual session cookie. Without this, an empty auth
  // state would silently cascade into ~29 downstream gym-staff failures.
  const state = await page.context().storageState();
  const hasSession = state.cookies.some(
    (c) =>
      (c.name === 'next-auth.session-token' ||
        c.name === '__Secure-next-auth.session-token') &&
      !!c.value,
  );
  expect(
    hasSession,
    'No NextAuth session cookie after staff login — auth state would be empty ' +
      'and silently cascade into downstream gym-staff failures',
  ).toBe(true);

  await page.context().storageState({ path: authFile });
});
