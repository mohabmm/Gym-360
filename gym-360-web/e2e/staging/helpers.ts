/**
 * Shared helpers for the STAGING e2e suite.
 *
 * Credentials default to the staging demo accounts (verified reachable
 * 2026-05-30). Override via env if a fresh tenant was provisioned:
 *   ADMIN_EMAIL/ADMIN_PASSWORD, STAFF_EMAIL/STAFF_PASSWORD.
 */
import { Page, APIRequestContext, expect, request } from '@playwright/test';

export const API = process.env.STAGING_API || 'https://staging-api.gym-365.com';
// Full v1 base. Build request URLs as `${V1}/path` — do NOT lean on
// APIRequestContext baseURL, since a leading-slash path resets to the origin
// root and silently drops the `/api/v1` prefix (→ 404).
export const V1 = `${API}/api/v1`;
export const ADMIN = {
  email: process.env.ADMIN_EMAIL || 'admin@demo.local',
  password: process.env.ADMIN_PASSWORD || '123456',
};
export const STAFF = {
  email: process.env.STAFF_EMAIL || 'staff@demo.local',
  password: process.env.STAFF_PASSWORD || '123456',
};

// Staging WAF rejects non-browser UAs — every API call must look like Chrome.
export const BROWSER_UA =
  'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 ' +
  '(KHTML, like Gecko) Chrome/120.0 Safari/537.36';

/** Unique, identifiable tag so created records never collide and are easy to spot/clean. */
export function e2eTag(): string {
  // Date.now is fine inside Playwright (not the workflow sandbox).
  return `e2e${Date.now().toString(36)}${Math.floor(Math.random() * 1e4)}`;
}

/** UI login via the NextAuth credentials form. Lands on a non-/login route. */
export async function uiLogin(page: Page, email: string, password: string): Promise<void> {
  await page.goto('/login', { waitUntil: 'domcontentloaded' });
  await page.fill('input[type="email"]', email);
  await page.fill('input[type="password"]', password);
  await page.click('button[type="submit"]');
  await page.waitForURL((u) => !u.pathname.startsWith('/login'), { timeout: 45000 });
  // Staff owners may bounce to /select-gym — pick the first gym tile.
  if (page.url().includes('/select-gym')) {
    await page.locator('button, a[href]').first().click({ timeout: 8000 }).catch(() => {});
    await page
      .waitForURL((u) => !u.pathname.startsWith('/select-gym'), { timeout: 15000 })
      .catch(() => {});
  }
}

/** An API context that carries the browser UA so the WAF lets it through. */
export async function apiContext(): Promise<APIRequestContext> {
  return request.newContext({
    extraHTTPHeaders: { 'User-Agent': BROWSER_UA, 'Content-Type': 'application/json' },
  });
}

/** Login through the API and return a gym-scoped bearer token + gymId. */
export async function apiLogin(
  ctx: APIRequestContext,
  email: string,
  password: string,
): Promise<{ token: string; gymId?: string }> {
  const res = await ctx.post(`${V1}/identity/login`, { data: { email, password } });
  if (!res.ok()) throw new Error(`api login ${email} -> ${res.status()}`);
  let token = (await res.json())?.data?.accessToken as string;
  let gymId = decodeJwt(token)?.gymId as string | undefined;
  // Pick up a gym scope if the login token has none (owners).
  if (!gymId) {
    const me = await ctx.get(`${V1}/identity/me`, { headers: auth(token) });
    if (me.ok()) {
      const data = (await me.json())?.data ?? {};
      gymId = (data.ownedGyms ?? data.gyms ?? [])?.[0]?.gymId;
    }
  }
  if (gymId) {
    const sel = await ctx.post(`${V1}/identity/select-gym`, {
      headers: auth(token),
      data: { gymId },
    });
    if (sel.ok()) token = (await sel.json())?.data?.accessToken ?? token;
  }
  return { token, gymId };
}

export const auth = (token: string) => ({ Authorization: `Bearer ${token}` });

export function decodeJwt(token: string): Record<string, any> | undefined {
  try {
    const p = token.split('.')[1];
    return JSON.parse(Buffer.from(p, 'base64').toString('utf-8'));
  } catch {
    return undefined;
  }
}

/**
 * Assert a dashboard route actually rendered: URL settled off /login, no
 * Next error boundary, and the app shell (<main> / heading) is present.
 */
export async function expectRendered(page: Page, route: string): Promise<void> {
  await page.goto(route, { waitUntil: 'domcontentloaded' });
  // Not kicked back to login (session valid).
  expect(page.url(), `route ${route} bounced to /login`).not.toContain('/login');
  // No unhandled Next.js error overlay / 500 boundary.
  const body = await page.locator('body').innerText().catch(() => '');
  expect(body, `route ${route} showed an error boundary`).not.toMatch(
    /Application error: a (client|server)-side exception|Internal Server Error|This page could not be found/i,
  );
  // App shell present.
  await expect(page.locator('main, h1, h2').first()).toBeVisible({ timeout: 20000 });
}

/** True if the visible page indicates the feature is plan-gated (skip writes then). */
export async function isPlanGated(page: Page): Promise<boolean> {
  const txt = await page.locator('body').innerText().catch(() => '');
  return /upgrade|not (available|included)|locked|premium|requires .* plan/i.test(txt);
}
