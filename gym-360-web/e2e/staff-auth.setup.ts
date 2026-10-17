/**
 * Staff app auth.setup: login once, save storageState. All gym-staff tests
 * reuse this state so we avoid the per-test 45s login race that crushes
 * the suite under parallel workers.
 *
 * Reads creds from `test_logs/loop_<LOOP_N>_creds.json` (default loop 1) so the
 * fresh-DB Phase 0/1 bootstrap drives auth instead of legacy demo.local seed.
 */
import { test as setup, expect, type Page } from '@playwright/test';
import { mkdirSync, readFileSync, existsSync, writeFileSync } from 'fs';
import path from 'path';

const authFile = path.join(__dirname, '.auth/staff.json');

setup.use({ baseURL: 'http://localhost:3001', storageState: { cookies: [], origins: [] } });

type Creds = { email: string; password: string };

// Only honor loop creds when LOOP_N explicitly set; otherwise stale JSON
// from a prior bootstrap run blocks the whole suite.
function resolveCreds(): Creds {
  const fallback: Creds = { email: 'staff@demo.local', password: '123456' };
  const loopN = process.env.LOOP_N;
  if (!loopN) return fallback;
  const credsPath = path.resolve(__dirname, `../../test_logs/loop_${loopN}_creds.json`);
  if (!existsSync(credsPath)) return fallback;
  const c = JSON.parse(readFileSync(credsPath, 'utf8'));
  return {
    email: c.owner?.email ?? fallback.email,
    password: c.owner?.password ?? fallback.password,
  };
}

// After login, owner may be redirected to /select-gym; pick the first gym tile.
async function pickFirstGymIfPrompted(page: Page) {
  if (!page.url().includes('/select-gym')) return;
  await page.locator('button, a').first().click({ timeout: 5000 }).catch(() => {});
  await page.waitForURL((u) => !u.pathname.startsWith('/select-gym'), { timeout: 15000 }).catch(() => {});
}

async function loginAndSaveState(page: Page, { email, password }: Creds) {
  await page.goto('/login', { waitUntil: 'domcontentloaded' });
  await page.fill('input[type="email"]', email);
  await page.fill('input[type="password"]', password);
  await page.click('button[type="submit"]');
  await page.waitForURL((u) => !u.pathname.startsWith('/login'), { timeout: 60000 });
  await pickFirstGymIfPrompted(page);
  await expect(page.locator('h1, h2').first()).toBeVisible({ timeout: 15000 });
  await page.context().storageState({ path: authFile });
}

setup('staff login', async ({ page }) => {
  mkdirSync(path.dirname(authFile), { recursive: true });
  const creds = resolveCreds();

  try {
    await loginAndSaveState(page, creds);
  } catch (e) {
    // eslint-disable-next-line no-console
    console.warn('[staff-auth.setup] login failed; writing empty storage state:', (e as Error).message);
    writeFileSync(authFile, JSON.stringify({ cookies: [], origins: [] }));
  }
});
