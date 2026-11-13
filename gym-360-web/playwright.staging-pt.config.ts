import { defineConfig, devices } from '@playwright/test';

/** Staging browser checks for the PT module + feature flags (see e2e/staging/pt-flags.spec.ts). */
export default defineConfig({
  testDir: './e2e/staging',
  testMatch: /pt-flags\.spec\.ts/,
  workers: 3,
  retries: 1,
  timeout: 90000,
  expect: { timeout: 20000 },
  reporter: [['list']],
  use: { ...devices['Desktop Chrome'], screenshot: 'only-on-failure', navigationTimeout: 45000, actionTimeout: 20000 },
});
