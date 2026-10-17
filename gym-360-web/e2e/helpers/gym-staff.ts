/**
 * Helpers for the gym-staff suite under e2e/gym-staff/.
 *
 * Gym-staff app runs on port 3001. Auth state is set by staff-auth.setup.ts via
 * project dependency, so tests share a warm session and skip the per-test login.
 */
import type { Browser, Page } from '@playwright/test';
import { isShown, submitLoginForm } from './ui';

export const SUITE = 'Gym Staff App — Comprehensive Tests';

export const STAFF_EMAIL = 'staff@demo.local';
export const STAFF_PASSWORD = '123456';

export async function login(page: Page): Promise<void> {
  await page.goto('/login', { waitUntil: 'domcontentloaded' });
  await submitLoginForm(page, STAFF_EMAIL, STAFF_PASSWORD);
  // Under parallel worker contention against the dev server, the credentials
  // callback can take >45s on first hit. Wait for any non-/login URL.
  await page.waitForURL((url) => !url.pathname.startsWith('/login'), { timeout: 90000 });
}

export async function loginOnce(page: Page): Promise<void> {
  await page.goto('/', { waitUntil: 'domcontentloaded' });
  if (page.url().includes('/login')) {
    await login(page);
  }
}

/** Ensures a session, opens `path` and lets the page settle for `settleMs`. */
export async function openStaffPage(page: Page, path: string, settleMs: number): Promise<void> {
  await loginOnce(page);
  await page.goto(path, { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(settleMs);
}

async function tryOpenLogin(page: Page): Promise<boolean> {
  try {
    await page.goto('/login', { waitUntil: 'domcontentloaded', timeout: 15000 });
    return true;
  } catch {
    // Use native setTimeout — page.waitForTimeout throws when page is closed
    await new Promise<void>((r) => setTimeout(r, 5000));
    return false;
  }
}

/** Verify the app is reachable before running tests (cold-start check). */
export async function ensureAppReachable({ browser }: { browser: Browser }): Promise<void> {
  const ctx = await browser.newContext();
  const page = await ctx.newPage();
  let reachable = false;
  for (let attempt = 0; attempt < 5 && !reachable; attempt++) {
    reachable = await tryOpenLogin(page);
  }
  await ctx.close();
  if (!reachable) throw new Error('App not reachable at localhost:3001 — start the dev server first');
}

/** Clicks the first member row and waits for its detail page. False when no row renders. */
export async function openFirstMemberDetail(page: Page): Promise<boolean> {
  const firstRow = page.locator('tbody tr').first();
  if (!(await firstRow.isVisible())) return false;
  await firstRow.click();
  await page.waitForURL(/\/members\/[^/]+$/, { timeout: 8000 });
  return true;
}

/** Clicks the first trainer link and waits for its detail page. False when none renders. */
export async function openFirstTrainerDetail(page: Page): Promise<boolean> {
  const firstLink = page.locator('a[href*="/trainers/"]').first();
  if (!(await isShown(firstLink, 5000))) return false;
  await firstLink.click();
  await page.waitForURL(/\/trainers\/[^/]+$/, { timeout: 10000 });
  return true;
}
