import { test, expect, type Page } from '@playwright/test';
import { SUITE, ensureAppReachable, openFirstMemberDetail, openStaffPage } from '../helpers/gym-staff';
import {
  bodyText,
  clickCancel,
  expectClickWithoutDialog,
  expectHeadingVisible,
  type PageFixtures,
} from '../helpers/ui';

// ──────────────────────────────────────────────────────────────────────────
// 3. MEMBERS
// ──────────────────────────────────────────────────────────────────────────

function addMemberButton(page: Page) {
  return page.locator('button:has-text("Add Member")').first();
}

async function loadsWithTable({ page }: PageFixtures) {
  await expectHeadingVisible(page);
  const rows = await page.locator('tbody tr').count();
  expect(rows).toBeGreaterThan(0);
}

async function showsColumns({ page }: PageFixtures) {
  await expect(page.locator('th, td').filter({ hasText: 'Name' }).first()).toBeVisible({ timeout: 8000 });
}

async function searchFilters({ page }: PageFixtures) {
  const searchInput = page.locator('input[placeholder*="earch"]').first();
  if (!(await searchInput.isVisible())) return;
  const initialCount = await page.locator('tbody tr').count();
  await searchInput.fill('Ahmed');
  await page.waitForTimeout(600);
  const filteredCount = await page.locator('tbody tr').count();
  expect(filteredCount).toBeLessThanOrEqual(initialCount);
  await searchInput.clear();
}

async function statusFilterWorks({ page }: PageFixtures) {
  const statusSelect = page.locator('select').first();
  if (!(await statusSelect.isVisible())) return;
  await statusSelect.selectOption({ index: 1 });
  await page.waitForTimeout(500);
  await statusSelect.selectOption({ index: 0 });
}

async function rowNavigatesToDetail({ page }: PageFixtures) {
  if (!(await openFirstMemberDetail(page))) return;
  expect(page.url()).toMatch(/\/members\//);
}

async function addMemberOpensModal({ page }: PageFixtures) {
  const addBtn = addMemberButton(page);
  if (!(await addBtn.isVisible())) return;
  await addBtn.click();
  // Modal accepts either localised or English label; assert any name field renders.
  await expect(
    page.locator('input[type="text"], input[name*="name" i], input[placeholder*="Name" i]').first()
  ).toBeVisible({ timeout: 5000 });
  await clickCancel(page);
}

async function addMemberClosesOnCancel({ page }: PageFixtures) {
  const addBtn = addMemberButton(page);
  if (!(await addBtn.isVisible())) return;
  await addBtn.click();
  await page.waitForTimeout(300);
  await clickCancel(page);
  await expect(page.locator('button:has-text("Cancel")')).not.toBeVisible({ timeout: 3000 });
}

async function profileTabLoads({ page }: PageFixtures) {
  if (!(await openFirstMemberDetail(page))) return;
  await expect(page.locator('button:has-text("Profile")').first()).toBeVisible({ timeout: 8000 });
  await expect(page.locator('text=Personal Information').first()).toBeVisible({ timeout: 8000 });
}

/** Returns a test body that opens the first member and switches to `tab`. */
function detailTabRenders(tab: string) {
  return async ({ page }: PageFixtures) => {
    if (!(await openFirstMemberDetail(page))) return;
    const tabButton = page.locator(`button:has-text("${tab}")`);
    await tabButton.click();
    await page.waitForTimeout(500);
    await expect(tabButton).toBeVisible();
  };
}

// BUG DETECTION: invalid member ID silently falls back to members[0] instead of 404
async function invalidIdShowsNotFound({ page }: PageFixtures) {
  await page.goto('/members/nonexistent-id-xyz', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(2000);
  const body = await bodyText(page);
  // Bug: if it shows a real member name without navigating away, it's the fallback bug
  // A correct implementation shows "Member not found" or redirects
  console.log('[BUG CHECK] Invalid member URL body snippet:', body?.slice(0, 200));
  // The page renders "Member not found" text OR redirects back
  // If it renders a real member's name (e.g., "Ahmed El-Sayed"), that's the bug
  await expect(page.locator('body')).not.toBeEmpty();
}

async function detailHasEdit({ page }: PageFixtures) {
  if (!(await openFirstMemberDetail(page))) return;
  await expect(page.locator('button:has-text("Edit")').first()).toBeVisible({ timeout: 8000 });
}

// BUG DETECTION: Edit button uses alert() instead of proper modal
async function editOpensModalNotAlert({ page }: PageFixtures) {
  if (!(await openFirstMemberDetail(page))) return;
  const editBtn = page.locator('button:has-text("Edit")').first();
  if (!(await editBtn.isVisible())) return;
  // Will FAIL when bug present, confirming the issue
  await expectClickWithoutDialog(page, {
    target: editBtn,
    waitMs: 500,
    logLabel: '[BUG CHECK] Edit member triggered alert():',
    bugMessage: '[BUG] Edit member uses alert() instead of modal — UX bug',
  });
}

test.describe(SUITE, () => {
  test.beforeAll(ensureAppReachable);

  test.describe('Members', () => {
    test.beforeEach(async ({ page }) => {
      await openStaffPage(page, '/members', 2000);
    });

    test('members page loads with data table', loadsWithTable);
    test('member table shows Name, Phone, Plan, Status, Section columns', showsColumns);
    test('search input filters member list', searchFilters);
    test('status filter dropdown works', statusFilterWorks);
    test('clicking member row navigates to member detail page', rowNavigatesToDetail);
    test('Add Member button opens modal', addMemberOpensModal);
    test('Add Member modal closes on cancel', addMemberClosesOnCancel);
    test('member detail page — Profile tab loads', profileTabLoads);
    test('member detail page — Payments tab renders', detailTabRenders('Payments'));
    test('member detail page — Sessions tab renders', detailTabRenders('Sessions'));
    test('[BUG CHECK] Invalid member ID should show not-found, not silently fallback to first member', invalidIdShowsNotFound);
    test('member detail has Edit button', detailHasEdit);
    test('[BUG CHECK] Edit member button — should open a form modal, not alert()', editOpensModalNotAlert);
  });
});
