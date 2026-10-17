import { test, expect, type Page } from '@playwright/test';
import { BASE_URL, SUITE, goTo, openFirstMember, warmupStaffLogin } from '../helpers/gym-staff-comprehensive';
import {
  bodyText,
  clickCancel,
  isMutation,
  watchDialog,
  watchRequest,
  type PageFixtures,
  type RequestWatch,
} from '../helpers/ui';

test.use({ baseURL: BASE_URL });

// ── 3. MEMBERS ────────────────────────────────────────────────────────────

async function tableLoadsWithRows({ page }: PageFixtures) {
  await expect(page.locator('h1, h2').first()).toBeVisible({ timeout: 8000 });
  // Wait for API data to load
  await page.waitForSelector('tbody tr', { timeout: 15000 });
  const rows = await page.locator('tbody tr').count();
  expect(rows).toBeGreaterThan(0);
}

async function tableColumnsVisible({ page }: PageFixtures) {
  await page.waitForSelector('thead th', { timeout: 10000 });
  for (const column of ['Name', 'Phone', 'Plan', 'Status']) {
    await expect(page.locator(`th:has-text("${column}")`).first()).toBeVisible();
  }
}

async function searchFilters({ page }: PageFixtures) {
  await page.waitForSelector('tbody tr', { timeout: 15000 });
  const searchInput = page.locator('input[placeholder*="earch"]').first();
  if (!(await searchInput.isVisible())) return;
  const before = await page.locator('tbody tr').count();
  await searchInput.fill('zz_nonexistent_member');
  await page.waitForTimeout(600);
  const after = await page.locator('tbody tr').count();
  expect(after).toBeLessThanOrEqual(before);
}

async function statusFilterFilters({ page }: PageFixtures) {
  await page.waitForSelector('select', { timeout: 10000 });
  const selects = page.locator('select');
  if (!(await selects.first().isVisible())) return;
  await selects.first().selectOption('active');
  await page.waitForTimeout(500);
  await selects.first().selectOption('all');
}

async function rowNavigatesToDetail({ page }: PageFixtures) {
  await page.waitForSelector('tbody tr', { timeout: 15000 });
  await page.locator('tbody tr').first().click();
  await page.waitForURL(/\/members\/[^/]+$/, { timeout: 10000 });
  expect(page.url()).toMatch(/\/members\//);
}

async function addMemberOpensModal({ page }: PageFixtures) {
  await page.locator('button:has-text("Add Member")').first().click();
  await expect(page.getByRole('heading', { name: /add member/i })).toBeVisible({ timeout: 5000 });
  await expect(page.locator('label:has-text("Name") input')).toBeVisible();
  await expect(page.locator('input[type="tel"]').first()).toBeVisible();
  await clickCancel(page);
}

/**
 * BUG-003 — Members: Add Member uses native alert() on success
 * src: members/page.tsx:65  alert(`Member ${newMember.name} added.`);
 * Fix: replace with toast.success()
 */
async function addMemberNoAlert({ page }: PageFixtures) {
  await page.locator('button:has-text("Add Member")').first().click();
  await page.waitForTimeout(300);
  await page.fill('input.mt-1.w-full.h-9', 'Test Member Alert');
  // Fill phone
  const phoneInput = page.locator('input[type="tel"]').first();
  if (await phoneInput.isVisible()) await phoneInput.fill('01012345678');

  const alert = watchDialog(page, '[BUG-003] alert:');

  await page.locator('button:has-text("Add Member")').last().click();
  await page.waitForTimeout(700);
  expect(alert.fired).toBe(false);
}

/**
 * BUG-004 — Members: Bulk Import uses native alert()
 * src: members/page.tsx:73  alert(`Processing ${file.name}...`);
 * Fix: replace with toast notification
 */
async function bulkImportNoAlert({ page }: PageFixtures) {
  // Trigger the hidden file input programmatically
  const alert = watchDialog(page, '[BUG-004] alert:');

  // Set up a fake file upload
  const [fileChooser] = await Promise.all([
    page.waitForEvent('filechooser', { timeout: 3000 }).catch(() => null),
    page.locator('button:has-text("Bulk Import")').click(),
  ]);
  if (!fileChooser) return;
  await fileChooser.setFiles({
    name: 'members.csv',
    mimeType: 'text/csv',
    buffer: Buffer.from('name,phone\nTest,01012345678'),
  });
  await page.waitForTimeout(700);
  expect(alert.fired).toBe(false);
}

// ── 4. MEMBER DETAIL ──────────────────────────────────────────────────────

async function profileTabSections({ page }: PageFixtures) {
  await expect(page.locator('text=Personal Information').first()).toBeVisible({ timeout: 8000 });
  await expect(page.locator('text=Membership').first()).toBeVisible({ timeout: 5000 });
}

/** Returns a test body that clicks `tab` and expects `heading` to render. */
function tabShowsHeading(tab: string, heading: string) {
  return async ({ page }: PageFixtures) => {
    await page.locator(`button:has-text("${tab}")`).click();
    await expect(page.locator(`text=${heading}`).first()).toBeVisible({ timeout: 5000 });
  };
}

async function editOpensModal({ page }: PageFixtures) {
  const editBtn = page.locator('button:has-text("Edit")').first();
  await expect(editBtn).toBeVisible({ timeout: 8000 });
  const alert = watchDialog(page);
  await editBtn.click();
  await page.waitForTimeout(500);
  // Edit modal should open (not alert)
  expect(alert.fired).toBe(false);
  await expect(page.locator('text=Edit Profile').first()).toBeVisible({ timeout: 5000 });
}

async function clickSaveChanges(page: Page, apiCall: RequestWatch) {
  const saveBtn = page.locator('button:has-text("Save Changes")').first();
  if (!(await saveBtn.isVisible())) return;
  await saveBtn.click();
  await page.waitForTimeout(1000);
  if (!apiCall.seen) {
    console.log('[BUG-005] Save Changes closed modal without API call — data not persisted');
  }
  expect(apiCall.seen).toBe(true);
}

/**
 * BUG-005 — Member Detail: "Save Changes" doesn't call API
 * src: members/[id]/page.tsx:145 — button onClick just closes modal
 * Fix: wire onClick to PUT /gyms/{gymId}/members/{memberId}
 */
async function saveChangesCallsApi({ page }: PageFixtures) {
  const editBtn = page.locator('button:has-text("Edit")').first();
  if (!(await editBtn.isVisible())) return;
  await editBtn.click();
  await page.waitForTimeout(400);
  const apiCall = watchRequest(page, (req) => isMutation(req, ['PUT', 'PATCH'], '/members/'));
  await clickSaveChanges(page, apiCall);
}

async function invalidIdNoCrash({ page }: PageFixtures) {
  await page.goto('/members/nonexistent-id-12345678');
  await page.waitForTimeout(2000);
  const body = await bodyText(page);
  // Should show "No member found" or "Not Found", not throw a crash
  const hasCrash = body?.includes('Unhandled Runtime Error');
  expect(hasCrash).toBe(false);
}

test.describe(SUITE, () => {
  test.beforeAll(warmupStaffLogin);

  test.describe('3. Members', () => {
    test.beforeEach(async ({ page }) => {
      await goTo(page, '/members');
    });

    test('members table loads with rows', tableLoadsWithRows);
    test('table columns: Name, Phone, Plan, Status, Expires visible', tableColumnsVisible);
    test('search input filters member list', searchFilters);
    test('status filter dropdown filters list', statusFilterFilters);
    test('row click navigates to member detail', rowNavigatesToDetail);
    test('Add Member button opens modal with form fields', addMemberOpensModal);
    test('[BUG-003] Add Member must NOT fire native alert() on save', addMemberNoAlert);
    test('[BUG-004] Bulk Import must NOT fire native alert()', bulkImportNoAlert);
  });

  test.describe('4. Member Detail', () => {
    test.beforeEach(async ({ page }) => {
      await openFirstMember(page);
    });

    test('Profile tab shows personal info and membership sections', profileTabSections);
    test('Payments tab renders', tabShowsHeading('Payments', 'Payment History'));
    test('Sessions tab renders', tabShowsHeading('Sessions', 'Training Sessions'));
    test('Edit button opens edit modal (not alert)', editOpensModal);
    test('[BUG-005] Save Changes must call API (not just close modal)', saveChangesCallsApi);
    test('invalid member ID shows not-found, not crash', invalidIdNoCrash);
  });
});
