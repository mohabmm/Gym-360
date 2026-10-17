import { test, expect, type Page } from '@playwright/test';
import { BASE_URL, SUITE, ensureLoggedIn, warmupStaffLogin } from '../helpers/gym-staff-comprehensive';
import {
  bodyText,
  collectPageErrors,
  collectServerErrors,
  expectNoErrors,
  expectTextVisible,
  type PageFixtures,
} from '../helpers/ui';

test.use({ baseURL: BASE_URL });

// ── 2. DASHBOARD ─────────────────────────────────────────────────────────

/** First entry waits for the dashboard data (12s); the rest use 8s. */
async function expectAllVisible(page: Page, selectors: string[]) {
  for (const [index, selector] of selectors.entries()) {
    await expect(page.locator(selector).first()).toBeVisible({ timeout: index === 0 ? 12000 : 8000 });
  }
}

async function reloadDashboard(page: Page) {
  await page.goto('/');
  await page.waitForTimeout(4000);
}

/**
 * BUG-001 — Dashboard crashes with runtime error
 * Symptom: "Unhandled Runtime Error" heading visible instead of dashboard
 * Root cause: components call .split(' ') on potentially undefined name fields
 *   e.g. trainerRankings[0].trainerName.split(' ')
 *   or   formatTime(cls.startTime) when startTime is undefined
 */
async function noRuntimeError({ page }: PageFixtures) {
  const text = await bodyText(page);
  const crashed = text?.includes('Unhandled Runtime Error') ||
                  text?.includes('Application error');
  if (crashed) {
    console.log('[BUG-001] Dashboard crash confirmed — body contains runtime error text');
  }
  expect(crashed).toBe(false);
}

/**
 * BUG-002 — Dashboard: "Cannot read properties of undefined (reading 'split')"
 * Multiple JS exceptions thrown, all from undefined.split()
 */
async function noSplitExceptions({ page }: PageFixtures) {
  const errs = collectPageErrors(page);
  await reloadDashboard(page);
  const splitErrs = errs.filter((e) => e.includes('split'));
  expectNoErrors(splitErrs, '[BUG-002] Split errors on dashboard:');
}

async function kpiCardsRender({ page }: PageFixtures) {
  await expectAllVisible(page, [
    'text=Active Members', 'text=Monthly Revenue', "text=Today's Check-ins", 'text=Active Trainers',
  ]);
}

async function quickActionsPresent({ page }: PageFixtures) {
  await expectAllVisible(page, [
    'text=Add Member', 'text=Record Payment', 'text=Create Class', 'text=Send Announcement',
  ]);
}

async function sidebarLinksPresent({ page }: PageFixtures) {
  await expectAllVisible(page, [
    'a[href="/members"]', 'a[href="/trainers"]', 'a[href="/classes"]', 'a[href="/billing"]',
  ]);
}

/** Returns a test body asserting a dashboard section heading renders. */
function sectionRenders(text: string) {
  return async ({ page }: PageFixtures) => expectTextVisible(page, text, 12000);
}

async function noServerErrors({ page }: PageFixtures) {
  const serverErrors = collectServerErrors(page);
  await reloadDashboard(page);
  expectNoErrors(serverErrors, '[500 ERRORS]');
}

test.describe(SUITE, () => {
  test.beforeAll(warmupStaffLogin);

  test.describe('2. Dashboard', () => {
    test.beforeEach(async ({ page }) => {
      await ensureLoggedIn(page);
      await page.goto('/');
      await page.waitForTimeout(3000);
    });

    test('[BUG-001] Dashboard must NOT show "Unhandled Runtime Error"', noRuntimeError);
    test('[BUG-002] No JS exceptions on dashboard (split on undefined)', noSplitExceptions);
    test('KPI cards render (Active Members, Monthly Revenue, Check-ins, Trainers)', kpiCardsRender);
    test("Today's Schedule section visible", sectionRenders("Today's Schedule"));
    test('Quick Actions present and linked', quickActionsPresent);
    test('Sidebar nav links present', sidebarLinksPresent);
    test('Top Classes section renders', sectionRenders('Top Classes'));
    test('Needs Attention section renders', sectionRenders('Needs Attention'));
    test('Coach of the Month section renders', sectionRenders('Coach of the Month'));
    test('Recent Activity section renders', sectionRenders('Recent Activity'));
    test('No 500 API errors on dashboard load', noServerErrors);
  });
});
