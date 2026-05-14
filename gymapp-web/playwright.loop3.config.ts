import { defineConfig, devices } from '@playwright/test';

// Standalone config for Loop 3 bootstrap. Runs the loop3 spec only,
// skips staff-setup project (no staff user exists yet at boot time).
export default defineConfig({
  testDir: './e2e',
  testMatch: /loop3-mohab-bootstrap\.spec\.ts/,
  fullyParallel: false,
  retries: 0,
  workers: 1,
  timeout: 180_000,
  reporter: [['list']],
  use: {
    baseURL: 'http://localhost:3000',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'off',
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
});
