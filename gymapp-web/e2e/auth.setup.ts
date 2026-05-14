import { test as setup } from '@playwright/test';
import { mkdirSync, writeFileSync } from 'fs';
import path from 'path';

const authFile = path.join(__dirname, '.auth/user.json');

setup('authenticate as admin', async ({ page }) => {
  mkdirSync(path.dirname(authFile), { recursive: true });

  // Owner tenancy specs (gym-staff on :3001) handle their own login per-test.
  // If the admin app (:3000) isn't reachable, fall back to an empty storage
  // state so dependent specs can still execute.
  try {
    await page.goto('/login', { waitUntil: 'domcontentloaded', timeout: 5000 });
    await page.fill('input[type="email"]', 'admin@demo.local');
    await page.fill('input[type="password"]', '123456');
    await page.click('button[type="submit"]');
    await page.waitForURL('/', { timeout: 30000 });
    await page.context().storageState({ path: authFile });
  } catch (e) {
    // eslint-disable-next-line no-console
    console.warn('[auth.setup] admin app unavailable; writing empty storage state:', (e as Error).message);
    writeFileSync(authFile, JSON.stringify({ cookies: [], origins: [] }));
  }
});
