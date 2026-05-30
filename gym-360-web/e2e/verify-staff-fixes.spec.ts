/**
 * Verify the two FE fixes against the LOCAL staff dev server (→ staging API):
 *   1. /analytics no longer throws "reduce is not a function"
 *   2. /gamification testimonials request carries gymId (no 400; 403 gating ok)
 *
 * Run:
 *   npx playwright test e2e/verify-staff-fixes.spec.ts --project=chromium --headed --workers=1 --no-deps
 */
import { test, expect, type Page } from '@playwright/test';

const BASE = process.env.STAFF_LOCAL ?? 'http://localhost:3001';
const EMAIL = 'staff@demo.local';
const PASS = '123456';

async function login(page: Page) {
  await page.goto(`${BASE}/login`, { waitUntil: 'domcontentloaded' });
  await page.fill('input[type="email"]', EMAIL);
  await page.fill('input[type="password"]', PASS);
  await page.click('button[type="submit"]');
  await page.waitForURL((u) => !u.pathname.startsWith('/login'), { timeout: 40000 });
}

test('staff fixes verify', async ({ page }) => {
  test.setTimeout(180000);
  const jsErrors: string[] = [];
  const testimonialStatuses: number[] = [];
  page.on('pageerror', (e) => jsErrors.push(e.message));
  page.on('console', (m) => { if (m.type() === 'error' && /reduce is not a function/.test(m.text())) jsErrors.push(m.text()); });
  page.on('response', (r) => {
    if (r.url().includes('/gamification/testimonials/pending')) testimonialStatuses.push(r.status());
  });

  await login(page);

  // 1) analytics must render without the reduce crash
  await page.goto(`${BASE}/analytics`, { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(4000);
  const reduceCrash = jsErrors.filter((e) => /reduce is not a function/.test(e));
  console.log('[verify] analytics JS errors:', JSON.stringify(jsErrors));
  expect(reduceCrash, 'analytics should not throw reduce error').toHaveLength(0);
  await expect(page.locator('text=Analytics').first()).toBeVisible({ timeout: 8000 });

  // 2) gamification testimonials request should send gymId → 403 (gated) not 400 (malformed)
  await page.goto(`${BASE}/gamification`, { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(4000);
  console.log('[verify] testimonials statuses:', JSON.stringify(testimonialStatuses));
  expect(testimonialStatuses, 'testimonials must fire').not.toHaveLength(0);
  expect(testimonialStatuses.every((s) => s !== 400), 'no 400 (gymId now sent)').toBe(true);
});
