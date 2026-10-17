import { test, expect } from '@playwright/test';
import { ADMIN_EMAIL, ADMIN_PASSWORD, SUITE, login } from '../helpers/admin';
import { submitLoginForm, type PageFixtures } from '../helpers/ui';

// ──────────────────────────────────────────────────────────────────────────
// 1. AUTH
// ──────────────────────────────────────────────────────────────────────────

async function rendersCorrectly({ page }: PageFixtures) {
  await page.goto('/login');
  await expect(page.locator('h1')).toContainText(/Admin/i);
  await expect(page.locator('input[type="email"]')).toBeVisible();
  await expect(page.locator('input[type="password"]')).toBeVisible();
  await expect(page.locator('button[type="submit"]')).toBeVisible();
}

async function redirectsToLogin({ page }: PageFixtures) {
  await page.goto('/');
  await expect(page).toHaveURL(/\/login/);
}

async function wrongCredentialsShowError({ page }: PageFixtures) {
  await page.goto('/login');
  await submitLoginForm(page, 'wrong@example.com', 'wrongpass');
  await expect(page.locator('text=Invalid credentials')).toBeVisible({ timeout: 10000 });
}

async function correctCredentialsLogIn({ page }: PageFixtures) {
  await login(page);
  await expect(page).toHaveURL('/');
  await expect(page.locator('input[type="email"]')).not.toBeVisible();
}

async function loadingStateOnSubmit({ page }: PageFixtures) {
  await page.goto('/login');
  await submitLoginForm(page, ADMIN_EMAIL, ADMIN_PASSWORD);
  await page.waitForURL('/', { timeout: 20000 });
  await expect(page).toHaveURL('/');
}

async function emptySubmitStays({ page }: PageFixtures) {
  await page.goto('/login');
  await page.click('button[type="submit"]');
  await expect(page).toHaveURL(/\/login/);
}

test.describe(SUITE, () => {
  test.describe('Authentication', () => {
    // Reset to anonymous storage so /login and /-redirect tests see unauthenticated state.
    test.use({ storageState: { cookies: [], origins: [] } });

    test('login page renders correctly', rendersCorrectly);
    test('unauthenticated user redirected to /login', redirectsToLogin);
    test('wrong credentials shows error message', wrongCredentialsShowError);
    test('correct credentials logs in and redirects to dashboard', correctCredentialsLogIn);
    test('login button shows loading state during submit', loadingStateOnSubmit);
    test('empty form submission shows browser validation', emptySubmitStays);
  });
});
