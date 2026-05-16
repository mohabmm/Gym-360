/**
 * Phase 5 — Owner-driven staff web smoke test against the Loop 1 tenant.
 * Reads creds from test_logs/loop_<LOOP_N>_creds.json (defaults to loop 1).
 * Logs in as owner on :3001, walks every primary nav target, records HTTP
 * 4xx/5xx responses, and writes a per-loop report to test_logs/.
 *
 * Run: cd gymapp-web && npx playwright test e2e/phase5-owner-smoke.spec.ts --project=gym-staff
 */

import { test, expect } from '@playwright/test';
import { readFileSync, writeFileSync, mkdirSync } from 'fs';
import path from 'path';

const LOOP_N = process.env.LOOP_N ?? '1';
const CREDS_FILE = path.resolve(
  __dirname,
  `../../test_logs/loop_${LOOP_N}_creds.json`,
);
const REPORT_FILE = path.resolve(
  __dirname,
  `../../test_logs/loop_${LOOP_N}_phase5_report.json`,
);

// Fall back to demo seed creds when no fresh-loop json present, so the suite
// is runnable on a vanilla seeded DB without a phase-0 bootstrap.
const creds = (() => {
  try {
    return JSON.parse(readFileSync(CREDS_FILE, 'utf8'));
  } catch {
    return {
      owner: { email: 'owner@demo.local', password: '123456' },
      staff: { email: 'staff@demo.local', password: '123456' },
    };
  }
})();

const STAFF_BASE = process.env.STAFF_BASE_URL ?? 'http://localhost:3001';

const PAGES = [
  '/',
  '/staff',
  '/trainers',
  '/members',
  '/classes',
  '/check-ins',
  '/announcements',
  '/billing',
  '/analytics',
  '/data-import',
  '/exercises',
  '/gamification',
  '/kiosk-pin',
  '/settings',
];

test.describe.configure({ mode: 'serial' });
test.use({ storageState: { cookies: [], origins: [] } });

test('Phase 5: owner staff web smoke', async ({ page }) => {
  test.setTimeout(180_000);
  const errors: any[] = [];
  const consoleErrors: string[] = [];
  const apiResponses: { url: string; status: number; page: string }[] = [];

  page.on('console', (msg) => {
    if (msg.type() === 'error') consoleErrors.push(msg.text());
  });
  page.on('response', (resp) => {
    const url = resp.url();
    if (url.includes('/api/v1/') && resp.status() >= 400) {
      apiResponses.push({ url, status: resp.status(), page: page.url() });
    }
  });

  // Login
  await page.goto(`${STAFF_BASE}/login`, { waitUntil: 'domcontentloaded' });
  await page.fill('input[type="email"]', creds.owner.email);
  await page.fill('input[type="password"]', creds.owner.password);
  await page.click('button[type="submit"]');
  await page
    .waitForURL((u) => !u.pathname.startsWith('/login'), { timeout: 30000 })
    .catch(() => errors.push({ stage: 'login_redirect_timeout', url: page.url() }));

  // If gym chooser appeared, pick the only gym
  if (page.url().includes('/select-gym')) {
    await page.locator('button, a').filter({ hasText: creds.gymName }).first().click().catch(() => {});
    await page.waitForURL((u) => !u.pathname.startsWith('/select-gym'), { timeout: 15000 }).catch(() => {});
  }

  // Walk pages
  for (const route of PAGES) {
    try {
      await page.goto(`${STAFF_BASE}${route}`, { waitUntil: 'domcontentloaded', timeout: 20000 });
      await page.waitForTimeout(800);
      // assert page didn't redirect back to /login
      if (page.url().includes('/login')) {
        errors.push({ route, problem: 'redirected_to_login' });
      }
    } catch (e: any) {
      errors.push({ route, problem: 'navigation_error', message: e.message });
    }
  }

  const report = {
    loop: LOOP_N,
    owner: creds.owner.email,
    gymId: creds.gymId,
    pageErrors: errors,
    consoleErrors,
    failedApiCalls: apiResponses,
    timestamp: new Date().toISOString(),
  };
  mkdirSync(path.dirname(REPORT_FILE), { recursive: true });
  writeFileSync(REPORT_FILE, JSON.stringify(report, null, 2));
  console.log(`Phase 5 report → ${REPORT_FILE}`);
  console.log(JSON.stringify(report, null, 2));
});
