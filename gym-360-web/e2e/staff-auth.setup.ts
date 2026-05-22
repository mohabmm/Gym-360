/**
 * Staff app auth.setup: login once, save storageState. All gym-staff tests
 * reuse this state so we avoid the per-test 45s login race that crushes
 * the suite under parallel workers.
 *
 * Reads creds from `test_logs/loop_<LOOP_N>_creds.json` (default loop 1) so the
 * fresh-DB Phase 0/1 bootstrap drives auth instead of legacy demo.local seed.
 */
import { test as setup, expect } from '@playwright/test';
import { mkdirSync, readFileSync, existsSync, writeFileSync } from 'fs';
import path from 'path';

const authFile = path.join(__dirname, '.auth/staff.json');

setup.use({ baseURL: 'http://localhost:3001', storageState: { cookies: [], origins: [] } });

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

  try {
    await page.goto('/login', { waitUntil: 'domcontentloaded' });
    await page.fill('input[type="email"]', email);
    await page.fill('input[type="password"]', password);
    await page.click('button[type="submit"]');
    await page.waitForURL((u) => !u.pathname.startsWith('/login'), { timeout: 60000 });
    // After login, owner may be redirected to /select-gym; pick the first gym tile.
    if (page.url().includes('/select-gym')) {
      await page.locator('button, a').first().click({ timeout: 5000 }).catch(() => {});
      await page.waitForURL((u) => !u.pathname.startsWith('/select-gym'), { timeout: 15000 }).catch(() => {});
    }
    await expect(page.locator('h1, h2').first()).toBeVisible({ timeout: 15000 });
    await page.context().storageState({ path: authFile });
  } catch (e) {
    // eslint-disable-next-line no-console
    console.warn('[staff-auth.setup] login failed; writing empty storage state:', (e as Error).message);
    writeFileSync(authFile, JSON.stringify({ cookies: [], origins: [] }));
  }
});
