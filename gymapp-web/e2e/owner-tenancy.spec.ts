import { test, expect, request as playwrightRequest } from '@playwright/test';

/**
 * Owner Hard-mode tenancy flow on gym-staff (:3001).
 *
 * - Owner login surfaces ownedGyms[] and an empty gymId.
 * - Middleware redirects /-> /select-gym for an Owner with no gym chosen.
 * - Picking a gym calls POST /api/select-gym, which proxies to the backend
 *   and returns a fresh gym-scoped JWT (gymId + schemaPrefix claims).
 * - Switching gyms via the GymSwitcher header dropdown re-issues the JWT
 *   and hits per-gym endpoints with the new schema.
 *
 * Backend prereq: API on :8080 with Tenancy:Mode=Hard, gyms_registry holding
 * at least two gyms for owner@demo.local. The provisioning + bootstrap from
 * step 4/8 already set this up.
 */

const STAFF_URL = process.env.STAFF_URL ?? 'http://localhost:3001';
const API_URL = process.env.API_URL ?? 'http://localhost:8080';

test.describe.configure({ mode: 'serial' });

test.describe('Owner Hard-mode tenancy', () => {
  test('Owner login → chooser → switch g1 ↔ g2 → unified dashboard', async ({ browser }) => {
    const ctx = await browser.newContext({ baseURL: STAFF_URL });
    const page = await ctx.newPage();

    // 1. Login as Owner
    await page.goto('/login');
    await page.fill('input[type="email"]', 'owner@demo.local');
    await page.fill('input[type="password"]', '123456');
    await page.click('button[type="submit"]');

    // 2. Owner has 2+ gyms → middleware redirects to /select-gym.
    await page.waitForURL('/select-gym', { timeout: 30000 });
    await expect(page.getByRole('heading', { name: /select gym/i })).toBeVisible();

    // Both owned gyms render as choices.
    const firstGymBtn = page.getByRole('button', { name: /First Gym/ });
    const formaMaadiBtn = page.getByRole('button', { name: /Forma Maadi/ });
    await expect(firstGymBtn).toBeVisible();
    await expect(formaMaadiBtn).toBeVisible();

    // 3. Pick g1 (First Gym).
    await firstGymBtn.click();
    await page.waitForURL('/', { timeout: 30000 });

    // After selection, NextAuth session should carry gymId + schemaPrefix=g1.
    let session = await page.evaluate(() => fetch('/api/auth/session').then((r) => r.json()));
    expect(session.user.gymId).toBeTruthy();
    expect(session.user.schemaPrefix).toBe('g1');
    expect(session.user.gymName).toBe('First Gym');

    // 4. Switch to g2 via the header GymSwitcher.
    // The component renders a haspopup=listbox button showing the current gym
    // name; clicking opens a role=listbox with role=option items.
    const switcher = page.locator('button[aria-haspopup="listbox"]').first();
    await switcher.click();
    await page.getByRole('option', { name: /Forma Maadi/ }).click();

    // After the switch the page refreshes with new session.
    await page.waitForLoadState('networkidle');
    session = await page.evaluate(() => fetch('/api/auth/session').then((r) => r.json()));
    expect(session.user.schemaPrefix).toBe('g2');
    expect(session.user.gymName).toBe('Forma Maadi');

    // 5. Owner unified dashboard rollup — call backend directly with the
    // session's accessToken. Should aggregate across g1 + g2.
    const ownerId = session.user.ownerId;
    const accessToken = session.accessToken;
    expect(ownerId).toBeTruthy();
    expect(accessToken).toBeTruthy();

    const apiCtx = await playwrightRequest.newContext({ baseURL: API_URL });
    const dashRes = await apiCtx.get(`/api/v1/owners/${ownerId}/dashboard`, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    expect(dashRes.ok()).toBeTruthy();
    const dash = await dashRes.json();
    expect(dash.totalGyms).toBeGreaterThanOrEqual(2);
    expect(dash.byGym.map((g: any) => g.schemaPrefix).sort()).toEqual(
      expect.arrayContaining(['g1', 'g2'])
    );
    await apiCtx.dispose();

    await ctx.close();
  });

  test('Direct /api/select-gym rejects unowned gymId', async ({ browser }) => {
    const ctx = await browser.newContext({ baseURL: STAFF_URL });
    const page = await ctx.newPage();

    await page.goto('/login');
    await page.fill('input[type="email"]', 'owner@demo.local');
    await page.fill('input[type="password"]', '123456');
    await page.click('button[type="submit"]');
    await page.waitForURL('/select-gym');

    const res = await page.request.post('/api/select-gym', {
      data: { gymId: '00000000-0000-0000-0000-000000000099' },
    });
    expect(res.status()).toBe(404);
    await ctx.close();
  });

  test('/api/select-gym rejects unauthenticated', async ({ browser }) => {
    const ctx = await browser.newContext({ baseURL: STAFF_URL });
    const res = await ctx.request.post('/api/select-gym', {
      data: { gymId: '41f84aa0-5341-4427-ae52-a992f46b3c10' },
    });
    expect(res.status()).toBe(401);
    await ctx.close();
  });
});
