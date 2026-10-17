/**
 * Helpers for the admin suite under e2e/admin/. Most admin tests share a
 * "wait for data, then assert" shape, so the factories below return ready
 * test bodies parameterised by settle time and target.
 */
import { expect, type Page } from '@playwright/test';
import type { PageFixtures } from './ui';

export const SUITE = 'Admin App — Comprehensive Tests';

export const ADMIN_EMAIL = 'admin@demo.local';
export const ADMIN_PASSWORD = '123456';

export async function login(page: Page): Promise<void> {
  await page.goto('/login');
  await page.fill('input[type="email"]', ADMIN_EMAIL);
  await page.fill('input[type="password"]', ADMIN_PASSWORD);
  await page.click('button[type="submit"]');
  await page.waitForURL('/', { timeout: 20000 });
}

export async function loginOnce(page: Page): Promise<void> {
  await page.goto('/');
  if (page.url().includes('/login')) {
    await login(page);
  }
}

/** Ensures a session and opens `path`. */
export async function openAdminPage(page: Page, path: string): Promise<void> {
  await loginOnce(page);
  await page.goto(path);
}

// Navigate to gyms list and click first row; returns whether navigation succeeded
export async function navigateToFirstGym(page: Page): Promise<boolean> {
  await page.goto('/gyms');
  await page.waitForTimeout(3000);
  const firstRow = page.locator('tbody tr').first();
  if (!(await firstRow.isVisible())) return false;
  await firstRow.click();
  try {
    await page.waitForURL(/\/gyms\/[^/]+$/, { timeout: 8000 });
    return true;
  } catch {
    return false;
  }
}

/** Test body: after `waitMs`, the first h1/h2 is visible. */
export function headingLoads(waitMs: number) {
  return async ({ page }: PageFixtures) => {
    await page.waitForTimeout(waitMs);
    await expect(page.locator('h1, h2').first()).toBeVisible({ timeout: 10000 });
  };
}

export type TextCheck = { text: string; waitMs: number; timeout?: number };

/** Test body: after `waitMs`, the first element matching `text=<text>` is visible. */
export function textVisibleAfter({ text, waitMs, timeout = 10000 }: TextCheck) {
  return async ({ page }: PageFixtures) => {
    await page.waitForTimeout(waitMs);
    await expect(page.locator(`text=${text}`).first()).toBeVisible({ timeout });
  };
}

/** Test body: after `waitMs`, the table has at least one row. */
export function hasTableRows(waitMs: number) {
  return async ({ page }: PageFixtures) => {
    await page.waitForTimeout(waitMs);
    const rows = await page.locator('tbody tr').count();
    expect(rows).toBeGreaterThan(0);
  };
}

/** Test body: after `waitMs`, at least one SVG (chart) is on the page. */
export function svgRenders(waitMs: number) {
  return async ({ page }: PageFixtures) => {
    await page.waitForTimeout(waitMs);
    const svgCount = await page.locator('svg').count();
    expect(svgCount).toBeGreaterThan(0);
  };
}

/** Test body: when the Export button renders, it is enabled. */
export function exportEnabledIfShown(waitMs: number) {
  return async ({ page }: PageFixtures) => {
    await page.waitForTimeout(waitMs);
    const exportBtn = page.locator('button:has-text("Export")');
    if (await exportBtn.isVisible()) {
      await expect(exportBtn).toBeEnabled();
    }
  };
}

/** Test body: clicking Export (when shown) is tolerated with or without a download. */
export function exportClickTolerated(waitMs: number) {
  return async ({ page }: PageFixtures) => {
    await page.waitForTimeout(waitMs);
    const exportBtn = page.locator('button:has-text("Export")');
    if (!(await exportBtn.isVisible())) return;
    await Promise.all([
      page.waitForEvent('download', { timeout: 5000 }).catch(() => null),
      exportBtn.click(),
    ]);
  };
}

export type SearchCheck = { term: string; waitMs: number };

/** Test body: types `term` into the search box (when shown) and clears it. */
export function searchFillAndClear({ term, waitMs }: SearchCheck) {
  return async ({ page }: PageFixtures) => {
    await page.waitForTimeout(waitMs);
    const searchInput = page.locator('input[placeholder*="earch"]').first();
    if (!(await searchInput.isVisible())) return;
    await searchInput.fill(term);
    await page.waitForTimeout(500);
    await searchInput.clear();
  };
}

/** Test body: searching for `term` never increases the row count. */
export function searchNarrowsRows({ term, waitMs }: SearchCheck) {
  return async ({ page }: PageFixtures) => {
    await page.waitForTimeout(waitMs);
    const searchInput = page.locator('input[placeholder*="earch"]').first();
    if (!(await searchInput.isVisible())) return;
    const rowsBefore = await page.locator('tbody tr').count();
    await searchInput.fill(term);
    await page.waitForTimeout(1000);
    const rowsAfter = await page.locator('tbody tr').count();
    expect(rowsAfter).toBeLessThanOrEqual(rowsBefore);
  };
}
