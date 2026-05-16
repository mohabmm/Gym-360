import { test, expect } from '@playwright/test';
import config from '../playwright.config';

/**
 * Regression guard for the staging-loop harness contract.
 *
 * scripts/staging_loop/run_web_e2e.sh invokes Playwright with
 * `--project admin` (admin app) and `--project gym-staff` (staff app),
 * each chained to its auth setup project (`setup` / `staff-setup`).
 *
 * If any of these project names drift, the sweep produces no results.json
 * and the orchestrator reports "no results.json produced, exit 1".
 * This test fails loudly the moment a project is renamed/removed.
 */
test.describe('playwright config harness contract', () => {
  const projectNames = (config.projects ?? []).map((p) => p.name);

  for (const required of ['setup', 'staff-setup', 'admin', 'gym-staff']) {
    test(`exposes project "${required}" (required by run_web_e2e.sh)`, () => {
      expect(projectNames).toContain(required);
    });
  }

  test('admin project depends on setup auth project', () => {
    const admin = (config.projects ?? []).find((p) => p.name === 'admin');
    expect(admin).toBeTruthy();
    expect(admin?.dependencies ?? []).toContain('setup');
  });

  test('gym-staff project depends on staff-setup auth project', () => {
    const staff = (config.projects ?? []).find((p) => p.name === 'gym-staff');
    expect(staff).toBeTruthy();
    expect(staff?.dependencies ?? []).toContain('staff-setup');
  });
});
