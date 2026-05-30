import { test as setup, expect } from '@playwright/test';
import { mkdirSync } from 'fs';
import path from 'path';
import { uiLogin, ADMIN } from './helpers';

const authFile = path.join(__dirname, '.auth/admin.json');

setup('staging admin login', async ({ page }) => {
  mkdirSync(path.dirname(authFile), { recursive: true });
  await uiLogin(page, ADMIN.email, ADMIN.password);
  await expect(page.locator('main, h1, h2').first()).toBeVisible({ timeout: 20000 });
  await page.context().storageState({ path: authFile });
});
