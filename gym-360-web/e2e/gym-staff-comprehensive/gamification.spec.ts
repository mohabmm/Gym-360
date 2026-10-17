import { test, expect, type Page } from '@playwright/test';
import { BASE_URL, SUITE, goTo, warmupStaffLogin } from '../helpers/gym-staff-comprehensive';
import { cancelIfShown, isShown, watchDialog, type PageFixtures } from '../helpers/ui';

test.use({ baseURL: BASE_URL });

// ── 13. GAMIFICATION ─────────────────────────────────────────────────────

async function openTab(page: Page, tab: string) {
  await page.locator(`button:has-text("${tab}")`).click();
  await page.waitForTimeout(500);
}

async function hasThreeTabs({ page }: PageFixtures) {
  await expect(page.locator('button:has-text("Member Leaderboard")')).toBeVisible({ timeout: 8000 });
  await expect(page.locator('button:has-text("Coach Rankings")')).toBeVisible();
  await expect(page.locator('button:has-text("Testimonials")')).toBeVisible();
}

async function leaderboardRanked({ page }: PageFixtures) {
  await openTab(page, 'Member Leaderboard');
  await page.waitForSelector('tbody tr', { timeout: 10000 });
  const rows = await page.locator('tbody tr').count();
  expect(rows).toBeGreaterThan(0);
}

async function coachRankingsCards({ page }: PageFixtures) {
  await openTab(page, 'Coach Rankings');
  await expect(page.locator('text=Score').first()).toBeVisible({ timeout: 8000 });
}

async function testimonialsPending({ page }: PageFixtures) {
  await openTab(page, 'Testimonials');
  const hasApprove = await isShown(page.locator('button:has-text("Approve")').first());
  const hasEmpty = await isShown(page.locator('text=No testimonials'));
  expect(hasApprove || hasEmpty).toBeTruthy();
}

async function rejectUsesConfirmModal({ page }: PageFixtures) {
  await openTab(page, 'Testimonials');
  const rejectBtn = page.locator('button:has-text("Reject")').first();
  if (!(await rejectBtn.isVisible())) return;
  const nativeConfirm = watchDialog(page);
  await rejectBtn.click();
  await page.waitForTimeout(400);
  expect(nativeConfirm.fired).toBe(false);
  await cancelIfShown(page);
}

test.describe(SUITE, () => {
  test.beforeAll(warmupStaffLogin);

  test.describe('13. Gamification', () => {
    test.beforeEach(async ({ page }) => {
      await goTo(page, '/gamification');
    });

    test('gamification page has three tabs', hasThreeTabs);
    test('Member Leaderboard shows ranked entries', leaderboardRanked);
    test('Coach Rankings shows trainer cards', coachRankingsCards);
    test('Testimonials tab renders pending items with Approve/Reject', testimonialsPending);
    test('Reject testimonial uses ConfirmModal, not native confirm', rejectUsesConfirmModal);
  });
});
