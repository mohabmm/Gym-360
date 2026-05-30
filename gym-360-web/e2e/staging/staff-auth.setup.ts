import { test as setup, expect } from '@playwright/test';
import { mkdirSync } from 'fs';
import path from 'path';
import { uiLogin, STAFF } from './helpers';

const authFile = path.join(__dirname, '.auth/staff.json');

setup('staging staff login', async ({ page }) => {
  mkdirSync(path.dirname(authFile), { recursive: true });
  await uiLogin(page, STAFF.email, STAFF.password);
  await expect(page.locator('main, h1, h2').first()).toBeVisible({ timeout: 20000 });
  await page.context().storageState({ path: authFile });
});
