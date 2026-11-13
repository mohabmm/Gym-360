import { defineConfig } from '@playwright/test';

/** Pure-logic tests (no browser, no server): `npx playwright test -c playwright.unit.config.ts`. */
export default defineConfig({
  testDir: '.',
  testMatch: /(packages|apps)\/.*\/__tests__\/.*\.test\.ts$/,
  testIgnore: /node_modules/,
  reporter: [['list']],
  workers: 2,
});
