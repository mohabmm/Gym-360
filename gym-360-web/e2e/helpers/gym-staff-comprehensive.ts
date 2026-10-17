/**
 * Helpers for the bug-detection suite under e2e/gym-staff-comprehensive/.
 *
 * Scope: all 14 routes + auth flow + bug-specific regression tests.
 * Base URL: http://localhost:3001  (gym-staff Next.js app)
 * Backend API: http://localhost:8080
 *
 * Run:
 *   npx playwright test e2e/gym-staff-comprehensive --project=gym-staff --reporter=list
 */
import type { Browser, Page } from '@playwright/test';
import { submitLoginForm } from './ui';

export const SUITE = 'Gym Staff App — Comprehensive Tests';
export const BASE_URL = 'http://localhost:3001';

const EMAIL = 'staff@demo.local';
const PASS = '123456';

export async function login(page: Page): Promise<void> {
  await page.goto('/login');
  await submitLoginForm(page, EMAIL, PASS);
  await page.waitForURL('/', { timeout: 60000 });
}

export async function ensureLoggedIn(page: Page): Promise<void> {
  await page.goto('/');
  if (page.url().includes('/login')) await login(page);
}

export async function goTo(page: Page, path: string): Promise<void> {
  await ensureLoggedIn(page);
  await page.goto(path);
  await page.waitForTimeout(2000);
}

async function tryWarmLogin(page: Page): Promise<boolean> {
  try {
    await page.goto('/login', { timeout: 15000 });
    await submitLoginForm(page, EMAIL, PASS);
    await page.waitForURL('/', { timeout: 60000 });
    await page.close();
    return true;
  } catch {
    await page.waitForTimeout(5000);
    return false;
  }
}

/** Warms the backend with a real login before the suite runs. */
export async function warmupStaffLogin({ browser }: { browser: Browser }): Promise<void> {
  const page = await browser.newPage();
  for (let i = 0; i < 5; i++) {
    if (await tryWarmLogin(page)) return;
  }
  throw new Error('Backend warmup failed — is the app running on :3001?');
}

/** Opens the members list and clicks into the first member's detail page. */
export async function openFirstMember(page: Page): Promise<void> {
  await goTo(page, '/members');
  await page.waitForSelector('tbody tr', { timeout: 15000 });
  const firstRow = page.locator('tbody tr').first();
  if (await firstRow.isVisible()) {
    await firstRow.click();
    await page.waitForURL(/\/members\/[^/]+$/, { timeout: 10000 });
  }
}

/** Opens the trainers list and clicks into the first trainer's detail page. */
export async function openFirstTrainer(page: Page): Promise<void> {
  await goTo(page, '/trainers');
  await page.waitForSelector('a[href*="/trainers/"]', { timeout: 10000 });
  await page.locator('a[href*="/trainers/"]').first().click();
  await page.waitForURL(/\/trainers\/[^/]+$/, { timeout: 8000 });
  await page.waitForTimeout(1500);
}

/** Opens the Add Member modal and fills name + phone where those inputs render. */
export async function fillAddMemberForm(page: Page, name: string, phone: string): Promise<void> {
  await page.locator('button:has-text("Add Member")').first().click();
  await page.waitForTimeout(200);
  const inputs = page.locator('input.mt-1.w-full.h-9');
  if (await inputs.first().isVisible()) await inputs.first().fill(name);
  const phoneInput = page.locator('input[type="tel"]').first();
  if (await phoneInput.isVisible()) await phoneInput.fill(phone);
}
