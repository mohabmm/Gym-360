import { test, expect, type Page } from '@playwright/test';
test.use({ baseURL: 'http://localhost:3001', storageState: { cookies: [], origins: [] } });

async function login(page: Page) {
  await page.goto('/login', { waitUntil: 'domcontentloaded' });
  await page.fill('input[type="email"]', 'staff@demo.local');
  await page.fill('input[type="password"]', '123456');
  await page.click('button[type="submit"]');
  await page.waitForURL('/', { timeout: 45000 });
}

async function loginOnce(page: Page) {
  await page.goto('/', { waitUntil: 'domcontentloaded' });
  console.log('after goto / url=', page.url());
  if (page.url().includes('/login')) {
    await login(page);
  }
}

test('probe loginOnce', async ({ page }) => {
  await loginOnce(page);
  console.log('final url=', page.url());
});
