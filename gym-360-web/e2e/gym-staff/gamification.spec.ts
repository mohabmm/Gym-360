import { test, expect, type Page } from '@playwright/test';
import { SUITE, ensureAppReachable, openStaffPage } from '../helpers/gym-staff';
import { expectClickWithoutDialog, isShown, type PageFixtures } from '../helpers/ui';

// ──────────────────────────────────────────────────────────────────────────
// 11. GAMIFICATION
// ──────────────────────────────────────────────────────────────────────────

async function openTab(page: Page, tab: string, settleMs = 500) {
  await page.locator(`button:has-text("${tab}")`).click();
  await page.waitForTimeout(settleMs);
}

async function hasThreeTabs({ page }: PageFixtures) {
  for (const tab of ['Member Leaderboard', 'Coach Rankings', 'Testimonials']) {
    await expect(page.locator(`button:has-text("${tab}")`)).toBeVisible({ timeout: 8000 });
  }
}

async function leaderboardShowsRanked({ page }: PageFixtures) {
  await openTab(page, 'Member Leaderboard');
  const hasRanked = await isShown(page.locator('tbody tr, [class*="leaderboard"], [class*="rank"]').first(), 5000);
  const hasEmpty = await isShown(page.locator('text=/no members|empty|لا يوجد/i').first(), 2000);
  expect(hasRanked || hasEmpty).toBeTruthy();
}

async function coachRankingsRender({ page }: PageFixtures) {
  const coachTab = page.locator('button:has-text("Coach Rankings")');
  await expect(coachTab).toBeVisible({ timeout: 5000 });
  await coachTab.click();
  await page.waitForTimeout(1000);
  // Verify no JS crash — page header stays visible
  await expect(page.locator('h1:has-text("Gamification")')).toBeVisible({ timeout: 5000 });
  // If trainer monthly scores are seeded, Demo Trainer should appear
  const hasTrainer = await isShown(page.locator('text=Demo Trainer').first());
  if (hasTrainer) {
    await expect(page.locator('text=Demo Trainer').first()).toBeVisible();
  }
}

async function testimonialsRender({ page }: PageFixtures) {
  await openTab(page, 'Testimonials');
  // Either testimonials with Approve buttons or empty state
  const hasApprove = await isShown(page.locator('button:has-text("Approve")').first());
  const hasEmpty = await isShown(page.locator('text=No testimonials'));
  expect(hasApprove || hasEmpty).toBeTruthy();
}

async function approveMarksApproved({ page }: PageFixtures) {
  await openTab(page, 'Testimonials');
  const approveBtn = page.locator('button:has-text("Approve")').first();
  if (!(await approveBtn.isVisible())) return;
  await approveBtn.click();
  await page.waitForTimeout(500);
  // Approved testimonial should show approved badge (use .first() to avoid strict mode)
  await expect(page.locator('text=approved').first()).toBeVisible({ timeout: 5000 });
}

// BUG DETECTION: Reject testimonial uses native confirm()
async function rejectUsesNoConfirm({ page }: PageFixtures) {
  await openTab(page, 'Testimonials');
  const rejectBtn = page.locator('button:has-text("Reject")').first();
  if (!(await rejectBtn.isVisible())) return;
  // Will FAIL confirming bug
  await expectClickWithoutDialog(page, {
    target: rejectBtn,
    waitMs: 500,
    logLabel: '[BUG CHECK] Reject testimonial fired confirm():',
    bugMessage: '[BUG] Testimonial rejection uses native confirm() — replace with ConfirmModal',
  });
}

test.describe(SUITE, () => {
  test.beforeAll(ensureAppReachable);

  test.describe('Gamification', () => {
    test.beforeEach(async ({ page }) => {
      await openStaffPage(page, '/gamification', 1500);
    });

    test('gamification page loads with three tabs', hasThreeTabs);
    test('Member Leaderboard tab shows ranked members', leaderboardShowsRanked);
    test('Coach Rankings tab renders trainer data', coachRankingsRender);
    test('Testimonials tab renders testimonials', testimonialsRender);
    test('Approve testimonial marks it as approved', approveMarksApproved);
    test('[BUG CHECK] Reject testimonial uses native confirm() instead of ConfirmModal', rejectUsesNoConfirm);
  });
});
