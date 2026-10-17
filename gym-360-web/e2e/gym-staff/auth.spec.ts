import { test, expect, type Page } from '@playwright/test';
import { SUITE, STAFF_EMAIL, STAFF_PASSWORD, ensureAppReachable, login } from '../helpers/gym-staff';
import { submitLoginForm, type PageFixtures } from '../helpers/ui';

// ──────────────────────────────────────────────────────────────────────────
// 1. AUTHENTICATION
// ──────────────────────────────────────────────────────────────────────────

async function openLogin(page: Page) {
  await page.goto('/login', { waitUntil: 'domcontentloaded' });
}

async function rendersBranding({ page }: PageFixtures) {
  await openLogin(page);
  await expect(page.locator('h1')).toContainText('GymApp Staff Portal');
  await expect(page.locator('input[type="email"]')).toBeVisible();
  await expect(page.locator('input[type="password"]')).toBeVisible();
  await expect(page.locator('button[type="submit"]')).toBeVisible();
}

async function expectRedirectToLogin(page: Page, path: string) {
  await page.context().clearCookies();
  await page.goto(path, { waitUntil: 'domcontentloaded' });
  await expect(page).toHaveURL(/\/login/);
}

async function rootRedirectsToLogin({ page }: PageFixtures) {
  await expectRedirectToLogin(page, '/');
}

async function protectedPageRedirectsToLogin({ page }: PageFixtures) {
  await expectRedirectToLogin(page, '/members');
}

async function wrongCredentialsShowError({ page }: PageFixtures) {
  await openLogin(page);
  await submitLoginForm(page, 'wrong@email.com', 'wrongpass');
  await expect(
    page.locator('text=Invalid email or password').or(page.locator('text=Invalid')),
  ).toBeVisible({ timeout: 20000 });
}

async function correctCredentialsRedirect({ page }: PageFixtures) {
  await login(page);
  await expect(page).toHaveURL('/');
  await expect(page.locator('input[type="email"]')).not.toBeVisible();
}

async function passwordToggleWorks({ page }: PageFixtures) {
  await openLogin(page);
  await expect(page.locator('input[type="password"]')).toBeVisible();
  // eye icon is the only button inside the password input wrapper
  await page.locator('form button[type="button"]').click();
  await expect(page.locator('input[type="text"]')).toBeVisible();
  await page.locator('form button[type="button"]').click();
  await expect(page.locator('input[type="password"]')).toBeVisible();
}

async function emptyFormStays({ page }: PageFixtures) {
  await openLogin(page);
  await page.click('button[type="submit"]');
  await expect(page).toHaveURL(/\/login/);
}

async function submitShowsSpinner({ page }: PageFixtures) {
  await openLogin(page);
  await submitLoginForm(page, STAFF_EMAIL, STAFF_PASSWORD);
  // After submission, should eventually land on dashboard
  await page.waitForURL('/', { timeout: 20000 });
  await expect(page).toHaveURL('/');
}

test.describe(SUITE, () => {
  test.beforeAll(ensureAppReachable);

  test.describe('Authentication', () => {
    // Auth tests need a fresh, unauthenticated context so login flows can be
    // exercised. The shared staff session would otherwise short-circuit them.
    test.use({ storageState: { cookies: [], origins: [] } });
    // Serialise auth tests — when several worker contexts hit /login + the
    // NextAuth credentials endpoint at the same time, the dev server's CSRF/
    // session state collides and login times out.
    test.describe.configure({ mode: 'serial' });

    test('login page renders GymApp Staff Portal branding', rendersBranding);
    test('unauthenticated user redirected to /login', rootRedirectsToLogin);
    test('unauthenticated access to any protected page redirects to /login', protectedPageRedirectsToLogin);
    test('wrong credentials show error message', wrongCredentialsShowError);
    test('correct credentials redirect to dashboard', correctCredentialsRedirect);
    test('password visibility toggle works', passwordToggleWorks);
    test('empty form stays on login page', emptyFormStays);
    test('submit button shows loading spinner while signing in', submitShowsSpinner);
  });
});
