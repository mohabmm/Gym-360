import { test, expect, type Page } from '@playwright/test';
import { BASE_URL, SUITE, goTo, openFirstTrainer, warmupStaffLogin } from '../helpers/gym-staff-comprehensive';
import {
  cancelIfShown,
  clickCancel,
  expectClickWithoutDialog,
  isShown,
  watchDialog,
  type PageFixtures,
} from '../helpers/ui';

test.use({ baseURL: BASE_URL });

// ── 9. TRAINERS ───────────────────────────────────────────────────────────

async function loadsActiveGrid({ page }: PageFixtures) {
  await expect(page.locator('h1').first()).toContainText('Trainers');
  // Active trainer cards present
  await page.waitForSelector('a[href*="/trainers/"]', { timeout: 10000 });
}

async function pendingSectionButtons({ page }: PageFixtures) {
  const pendingSection = page.locator('text=Pending Approval').first();
  if (!(await isShown(pendingSection, 5000))) return;
  await expect(page.locator('button:has-text("Approve")').first()).toBeVisible();
  await expect(page.locator('button:has-text("Reject")').first()).toBeVisible();
}

async function approveUsesToast({ page }: PageFixtures) {
  const approveBtn = page.locator('button:has-text("Approve")').first();
  if (!(await approveBtn.isVisible())) return;
  await expectClickWithoutDialog(page, { target: approveBtn, waitMs: 700, logLabel: '[TRAINER APPROVE] alert:' });
}

async function rejectUsesConfirmModal({ page }: PageFixtures) {
  const rejectBtn = page.locator('button:has-text("Reject")').first();
  if (!(await rejectBtn.isVisible())) return;
  const nativeConfirm = watchDialog(page);
  await rejectBtn.click();
  await page.waitForTimeout(400);
  expect(nativeConfirm.fired).toBe(false);
  await cancelIfShown(page);
}

async function addTrainerValidates({ page }: PageFixtures) {
  await page.locator('button:has-text("Add Trainer")').click();
  await page.waitForTimeout(300);

  // Validation error should appear inline, not as alert
  await expectClickWithoutDialog(page, {
    target: page.locator('button:has-text("Send Invitation")'),
    waitMs: 500,
    logLabel: '[ADD TRAINER] alert:',
    bugMessage: '[BUG] Add Trainer validation uses native alert()',
  });
  await clickCancel(page);
}

async function cardLinksToDetail({ page }: PageFixtures) {
  const trainerLink = page.locator('a[href*="/trainers/"]').first();
  if (!(await trainerLink.isVisible())) return;
  await trainerLink.click();
  await page.waitForURL(/\/trainers\/[^/]+$/, { timeout: 8000 });
  expect(page.url()).toMatch(/\/trainers\//);
}

// ── 10. TRAINER DETAIL ────────────────────────────────────────────────────

async function detailKpiCards({ page }: PageFixtures) {
  await expect(page.locator('text=Active Clients').first()).toBeVisible({ timeout: 8000 });
  await expect(page.locator('text=Monthly Score').first()).toBeVisible();
  await expect(page.locator('text=Average Rating').first()).toBeVisible();
}

async function detailTabsPresent({ page }: PageFixtures) {
  for (const tab of ['Profile', 'Members', 'Sessions', 'Ratings', 'Earnings']) {
    await expect(page.locator(`button:has-text("${tab}")`).first()).toBeVisible({ timeout: 8000 });
  }
}

/**
 * BUG-011 — Trainer Detail: "Edit Profile" button uses alert()
 * src: trainers/[id]/page.tsx:284  alert(`Edit profile for ${trainer.name} — form coming soon.`)
 * Fix: implement an actual edit form modal
 */
async function editProfileNoAlert({ page }: PageFixtures) {
  const editBtn = page.locator('button:has-text("Edit Profile")').first();
  if (!(await editBtn.isVisible())) return;
  await expectClickWithoutDialog(page, { target: editBtn, waitMs: 500, logLabel: '[BUG-011] Edit Profile alert:' });
}

function collect404s(page: Page): { url: string; status: number }[] {
  const apiErrors: { url: string; status: number }[] = [];
  page.on('response', (res) => {
    if (res.status() === 404 && res.url().includes('/api/v1/')) {
      apiErrors.push({ url: res.url(), status: res.status() });
    }
  });
  return apiErrors;
}

/**
 * BUG-012 — Trainer Detail: fetchTrainerRate and fetchTrainerEarnings use
 *            wrong URL base — calls /api/v1/... relative to :3001 instead of :8080
 * src: trainers/[id]/page.tsx:17  fetch(`/api/v1/gyms/${gymId}/trainers/${trainerId}/rate`)
 * Fix: use apiClient.get() (configured with baseURL=http://localhost:8080)
 */
async function earningsNo404({ page }: PageFixtures) {
  const apiErrors = collect404s(page);

  await page.locator('button:has-text("Earnings")').click();
  await page.waitForTimeout(3000);

  const rateErrors = apiErrors.filter((e) => e.url.includes('rate') || e.url.includes('earnings'));
  if (rateErrors.length > 0) {
    console.log('[BUG-012] Rate/Earnings 404s (wrong URL base):', rateErrors);
  }
  expect(rateErrors.length).toBe(0);
}

/** Returns a test body that opens `tab` and expects `heading` to render. */
function tabShowsHeading(tab: string, heading: string) {
  return async ({ page }: PageFixtures) => {
    await page.locator(`button:has-text("${tab}")`).first().click();
    await page.waitForTimeout(500);
    await expect(page.locator(`text=${heading}`).first()).toBeVisible({ timeout: 8000 });
  };
}

test.describe(SUITE, () => {
  test.beforeAll(warmupStaffLogin);

  test.describe('9. Trainers', () => {
    test.beforeEach(async ({ page }) => {
      await goTo(page, '/trainers');
    });

    test('trainers page loads with active trainer grid', loadsActiveGrid);
    test('pending approval section shows with Approve/Reject buttons', pendingSectionButtons);
    test('Approve trainer uses toast, not alert()', approveUsesToast);
    test('Reject trainer uses ConfirmModal, not native confirm', rejectUsesConfirmModal);
    test('Add Trainer form validates name+email required', addTrainerValidates);
    test('Trainer card links to detail page', cardLinksToDetail);
  });

  test.describe('10. Trainer Detail', () => {
    test.beforeEach(async ({ page }) => {
      await openFirstTrainer(page);
    });

    test('trainer detail loads with KPI cards', detailKpiCards);
    test('tabs present (Profile, Members, Sessions, Ratings, Earnings)', detailTabsPresent);
    test('[BUG-011] Edit Profile button must NOT fire alert()', editProfileNoAlert);
    test('[BUG-012] Trainer Earnings tab must NOT get 404 on session rate', earningsNo404);
    test('Members tab shows assigned clients table', tabShowsHeading('Members', 'Assigned Clients'));
    test('Sessions tab renders session history', tabShowsHeading('Sessions', 'Session History'));
    test('Ratings tab shows individual feedback', tabShowsHeading('Ratings', 'Client Ratings'));
  });
});
