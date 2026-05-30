import { defineConfig, devices } from '@playwright/test';

/**
 * STAGING e2e config — drives the deployed admin + staff webs against
 * staging-api.gym-365.com. Kept fully separate from the localhost
 * `playwright.config.ts` so neither run interferes with the other.
 *
 * Run:
 *   npx playwright test --config=playwright.staging.config.ts
 *   npx playwright test --config=playwright.staging.config.ts --project=admin
 *   npx playwright test --config=playwright.staging.config.ts --project=staff
 *
 * Override targets via env (defaults point at staging):
 *   STAGING_ADMIN, STAGING_STAFF, STAGING_API
 */
const ADMIN = process.env.STAGING_ADMIN || 'https://staging-admin.gym-365.com';
const STAFF = process.env.STAGING_STAFF || 'https://staging-staff.gym-365.com';

export default defineConfig({
  testDir: './e2e/staging',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: 1,
  workers: 3,
  timeout: 60000,
  expect: { timeout: 15000 },
  reporter: [['list'], ['html', { outputFolder: 'playwright-report-staging', open: 'never' }]],
  use: {
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'off',
    actionTimeout: 15000,
    navigationTimeout: 40000,
    // Real Desktop Chrome UA — staging WAF blocks non-browser UAs.
    ...devices['Desktop Chrome'],
  },
  projects: [
    { name: 'admin-setup', testMatch: /admin-auth\.setup\.ts/, use: { baseURL: ADMIN } },
    { name: 'staff-setup', testMatch: /staff-auth\.setup\.ts/, use: { baseURL: STAFF } },
    {
      name: 'admin',
      testMatch: /admin-flows\.spec\.ts/,
      dependencies: ['admin-setup'],
      use: { baseURL: ADMIN, storageState: 'e2e/staging/.auth/admin.json' },
    },
    {
      name: 'staff',
      testMatch: /staff-flows\.spec\.ts/,
      dependencies: ['staff-setup'],
      use: { baseURL: STAFF, storageState: 'e2e/staging/.auth/staff.json' },
    },
  ],
});
