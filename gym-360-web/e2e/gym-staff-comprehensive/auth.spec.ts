import { test, expect, type Page } from '@playwright/test';
import { BASE_URL, SUITE, login, warmupStaffLogin } from '../helpers/gym-staff-comprehensive';
import { submitLoginForm, type PageFixtures } from '../helpers/ui';

test.use({ baseURL: BASE_URL });

// ── 1. AUTHENTICATION ─────────────────────────────────────────────────────

async function expectRedirectToLogin(page: Page, path: string) {
  await page.goto(path);
  await expect(page).toHaveURL(/\/login/);
}

async function rendersBranding({ page }: PageFixtures) {
  await page.goto('/login');
  await expect(page.locator('h1')).toContainText('GymApp Staff Portal');
  await expect(page.locator('input[type="email"]')).toBeVisible();
  await expect(page.locator('input[type="password"]')).toBeVisible();
  await expect(page.locator('button[type="submit"]')).toBeVisible();
}

async function rootRedirects({ page }: PageFixtures) {
  await expectRedirectToLogin(page, '/');
}

async function membersRedirects({ page }: PageFixtures) {
  await expectRedirectToLogin(page, '/members');
}

async function wrongCredentialsShowError({ page }: PageFixtures) {
  await page.goto('/login');
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

async function passwordToggleSwitchesType({ page }: PageFixtures) {
  await page.goto('/login');
  await expect(page.locator('input[type="password"]')).toBeVisible();
  await page.locator('form button[type="button"]').click();
  await expect(page.locator('input[type="text"]')).toBeVisible();
}

async function emptySubmitStays({ page }: PageFixtures) {
  await page.goto('/login');
  await page.click('button[type="submit"]');
  await expect(page).toHaveURL(/\/login/);
}

test.describe(SUITE, () => {
  test.beforeAll(warmupStaffLogin);

  test.describe('1. Authentication', () => {
    test('login page renders GymApp Staff Portal branding', rendersBranding);
    test('unauthenticated root redirects to /login', rootRedirects);
    test('unauthenticated /members redirects to /login', membersRedirects);
    test('wrong credentials show error', wrongCredentialsShowError);
    test('correct credentials redirect to dashboard', correctCredentialsRedirect);
    test('password visibility toggle switches input type', passwordToggleSwitchesType);
    test('empty form submission stays on login', emptySubmitStays);
  });
});
