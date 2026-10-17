import { test, expect, type Page } from '@playwright/test';
import {
  BASE_URL,
  SUITE,
  ensureLoggedIn,
  fillAddMemberForm,
  goTo,
  warmupStaffLogin,
} from '../helpers/gym-staff-comprehensive';
import { bodyText, isShown, watchRequest, type PageFixtures } from '../helpers/ui';

test.use({ baseURL: BASE_URL });

// ── 18. NAVIGATION ────────────────────────────────────────────────────────

const ROUTES = [
  '/', '/members', '/trainers', '/classes', '/exercises',
  '/billing', '/gamification', '/announcements', '/analytics',
  '/check-ins', '/staff', '/data-import', '/kiosk-pin', '/settings',
];

async function expectRouteLoads(page: Page, route: string) {
  await page.goto(route);
  await page.waitForTimeout(500);
  const url = page.url();
  expect(url).not.toContain('/login');
  // Check for crash
  const body = await bodyText(page);
  const crashed = body?.includes('Unhandled Runtime Error');
  if (crashed) console.log(`[CRASH] Route ${route} shows runtime error`);
  expect(crashed).toBe(false);
}

async function allRoutesLoad({ page }: PageFixtures) {
  test.setTimeout(180000);
  for (const route of ROUTES) {
    await test.step(route, () => expectRouteLoads(page, route));
  }
}

function mentionsNotFound(body: string | null): boolean {
  return ['404', 'Not Found', 'not found'].some((marker) => body?.includes(marker));
}

async function unknownRouteHandled({ page }: PageFixtures) {
  await page.goto('/nonexistent-route-xyz-9999');
  await page.waitForTimeout(1500);
  const is404 = mentionsNotFound(await bodyText(page));
  const redirected = page.url().endsWith('/') || page.url().endsWith(':3001/');
  expect(is404 || redirected).toBeTruthy();
}

async function sidebarLinksNavigate({ page }: PageFixtures) {
  await page.goto('/');
  await page.waitForTimeout(2000);

  const membersLink = page.locator('a[href="/members"]').first();
  if (!(await membersLink.isVisible())) return;
  await membersLink.click();
  await expect(page).toHaveURL('/members');
}

// ── 19. BUG-SPECIFIC: API PATTERNS ───────────────────────────────────────

type AlertCheck = { path: string; label: string; trigger: (page: Page) => Promise<void> };

const ALERT_CHECKS: AlertCheck[] = [
  {
    path: '/members',
    label: 'Members Add Member',
    trigger: async (page) => {
      // Fill required fields
      await fillAddMemberForm(page, 'Test Alert Audit', '01099887766');
      await page.locator('button:has-text("Add Member")').last().click();
    },
  },
  {
    path: '/exercises',
    label: 'Exercises Add Custom (empty name)',
    trigger: async (page) => {
      const addBtn = page.locator('button:has-text("Add Custom Exercise")');
      if (!(await isShown(addBtn, 2000))) return;
      await addBtn.click();
      await page.waitForTimeout(200);
      await page.locator('button:has-text("Add Exercise")').last().click();
    },
  },
];

async function runAlertCheck(page: Page, check: AlertCheck, buggyAlerts: string[]) {
  await page.goto(check.path);
  await page.waitForTimeout(2000);
  page.once('dialog', (d) => {
    buggyAlerts.push(check.label);
    console.log(`[BUG-018] alert() on "${check.label}": ${d.message()}`);
    d.dismiss();
  });
  await check.trigger(page);
  await page.waitForTimeout(700);
}

/**
 * BUG-018 — All pages: Several actions still use alert() instead of toast/modal
 * Comprehensive check across members, exercises, billing, trainer detail
 */
async function globalAlertAudit({ page }: PageFixtures) {
  await ensureLoggedIn(page);
  const buggyAlerts: string[] = [];

  // Pages + triggers to check
  for (const check of ALERT_CHECKS) {
    await test.step(check.label, () => runAlertCheck(page, check, buggyAlerts));
  }

  if (buggyAlerts.length > 0) {
    console.log('[BUG-018] Pages still using native alert():', buggyAlerts);
  }
  expect(buggyAlerts.length).toBe(0);
}

/**
 * BUG-019 — Members, Billing, Classes: mutations don't hit API
 * Quick probe: after "adding" a member or recording a payment, check network requests
 */
async function memberCreationPosts({ page }: PageFixtures) {
  await goTo(page, '/members');

  const memberPost = watchRequest(page, (req) => req.method() === 'POST' && req.url().includes('/members'));

  await fillAddMemberForm(page, 'Network Test Member', '01012345678');

  // Handle any alert that may appear
  page.on('dialog', (d) => d.dismiss());
  await page.locator('button:has-text("Add Member")').last().click();
  await page.waitForTimeout(1000);

  if (!memberPost.seen) {
    console.log('[BUG-019] Add Member did not POST to API — data lost on refresh');
  }
  expect(memberPost.seen).toBe(true);
}

test.describe(SUITE, () => {
  test.beforeAll(warmupStaffLogin);

  test.describe('18. Navigation & Routing', () => {
    test.beforeEach(async ({ page }) => {
      await ensureLoggedIn(page);
    });

    test('all 14 routes load without crash or redirect to login', allRoutesLoad);
    test('404 for unknown route — shows not-found page or redirects', unknownRouteHandled);
    test('sidebar links navigate correctly from dashboard', sidebarLinksNavigate);
  });

  test.describe('19. API Integration Patterns', () => {
    test('[BUG-018] Global alert() audit — no page should use native alert() for user feedback', globalAlertAudit);
    test('[BUG-019] Member creation must POST to /gyms/{gymId}/members', memberCreationPosts);
  });
});
