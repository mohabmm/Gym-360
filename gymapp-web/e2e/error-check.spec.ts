import { test } from '@playwright/test';
test.use({ baseURL: 'http://localhost:3001' });
test('capture 2 errors content', async ({ page }) => {
  const jsErrors: string[] = [];
  page.on('pageerror', e => jsErrors.push(e.message));
  await page.goto('/login');
  await page.fill('input[type="email"]', 'staff@demo.local');
  await page.fill('input[type="password"]', '123456');
  await page.click('button[type="submit"]');
  await page.waitForURL('/', { timeout: 60000 });
  await page.waitForTimeout(3000);
  // Click the "2 errors" badge to see error details
  const badge = page.locator('text=2 errors').first();
  if (await badge.isVisible()) {
    await badge.click();
    await page.waitForTimeout(1000);
    await page.screenshot({ path: '/tmp/errors-open.png', fullPage: true });
  }
  console.log('JS errors:', jsErrors);
  // Also check console for error details
  page.on('console', msg => {
    if (msg.type() === 'error') console.log('CONSOLE ERROR:', msg.text());
  });
  await page.goto('/');
  await page.waitForTimeout(3000);
});
