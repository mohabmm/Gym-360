import { test, expect, type Page } from '@playwright/test';
import { BASE_URL, SUITE, goTo, warmupStaffLogin } from '../helpers/gym-staff-comprehensive';
import { isMutation, isShown, watchDialog, watchRequest, type PageFixtures } from '../helpers/ui';

test.use({ baseURL: BASE_URL });

// ── 7. BILLING ────────────────────────────────────────────────────────────

async function openTab(page: Page, tab: string, settleMs?: number) {
  await page.locator(`button:has-text("${tab}")`).click();
  if (settleMs !== undefined) await page.waitForTimeout(settleMs);
}

async function loadsWithKpis({ page }: PageFixtures) {
  await expect(page.locator('h1').first()).toContainText('Billing');
  await expect(page.locator('text=Total Revenue').first()).toBeVisible({ timeout: 8000 });
}

/** Returns a test body that opens `tab` and expects `selector` to render. */
function tabShows(tab: string, selector: string) {
  return async ({ page }: PageFixtures) => {
    await openTab(page, tab);
    await expect(page.locator(selector).first()).toBeVisible({ timeout: 8000 });
  };
}

async function fillPaymentForm(page: Page) {
  // Select first member
  const memberSelect = page.locator('select').first();
  if (await memberSelect.isVisible()) await memberSelect.selectOption({ index: 1 });

  // Enter amount
  const amountInput = page.locator('input[type="number"]').first();
  if (await amountInput.isVisible()) await amountInput.fill('500');
}

/**
 * BUG-007 — Billing: Record Payment uses native alert() on success
 * src: billing/page.tsx:84  alert(`Payment of ... recorded for ...`);
 * Fix: replace with toast.success()
 */
async function recordPaymentNoAlert({ page }: PageFixtures) {
  await page.locator('button:has-text("Record Payment")').first().click();
  await page.waitForTimeout(400);
  await fillPaymentForm(page);

  const alert = watchDialog(page, '[BUG-007] Record Payment alert:');

  await page.locator('button:has-text("Record Payment")').last().click();
  await page.waitForTimeout(700);
  expect(alert.fired).toBe(false);
}

async function optionCount(page: Page, nth: number) {
  return page.locator('select').nth(nth).locator('option').count().catch(() => 0);
}

async function submitAssignModal(page: Page) {
  const memberOpts = await optionCount(page, 0);
  const templateOpts = await optionCount(page, 1);
  if (memberOpts > 1) await page.locator('select').nth(0).selectOption({ index: 1 });
  if (templateOpts > 1) await page.locator('select').nth(1).selectOption({ index: 1 });

  // Submit (the modal's primary CTA — last "Assign" button in document)
  const submit = page.locator('div[role="dialog"] button:has-text("Assign")').last();
  if (await isShown(submit)) {
    await submit.click();
    await page.waitForTimeout(700);
  }
}

/**
 * BUG-008 — Billing: Assign Package uses native alert()
 * src: billing/page.tsx:67  alert(`${template.name} assigned to ${member.name}`);
 * Fix: replace with toast.success()
 */
async function assignPackageNoAlert({ page }: PageFixtures) {
  await openTab(page, 'Packages', 500);

  const assignBtn = page.locator('button:has-text("Assign to member")').first();
  if (!(await isShown(assignBtn))) {
    // No package templates seeded — nothing to assign; nothing to assert.
    test.skip();
    return;
  }

  const alert = watchDialog(page, '[BUG-008] alert:', true);

  await assignBtn.click();
  await page.waitForTimeout(400);
  await submitAssignModal(page);
  expect(alert.fired).toBe(false);
}

type SaveCheck = {
  tab: string;
  newButton: string;
  urlPart: string;
  name: string;
  bugMessage: string;
};

/** Returns a test body asserting a "New …" modal's Save sends POST/PUT to the API. */
function saveCallsApi(check: SaveCheck) {
  return async ({ page }: PageFixtures) => {
    await openTab(page, check.tab, 300);

    const apiCall = watchRequest(page, (req) => isMutation(req, ['POST', 'PUT'], check.urlPart));

    await page.locator(`button:has-text("${check.newButton}")`).click();
    await page.waitForTimeout(300);
    await page.locator('input').first().fill(check.name);
    await page.locator('button:has-text("Save")').last().click();
    await page.waitForTimeout(1000);

    if (!apiCall.seen) {
      console.log(check.bugMessage);
    }
    expect(apiCall.seen).toBe(true);
  };
}

/**
 * BUG-009 — Billing: Save Plan doesn't call API
 * src: billing/page.tsx:94-97 — savePlan() just closes modal
 * Fix: call POST/PUT /gyms/{gymId}/membership-plans
 */
const savePlanCallsApi = saveCallsApi({
  tab: 'Plans',
  newButton: 'New Plan',
  urlPart: 'plan',
  name: 'Test Plan API',
  bugMessage: '[BUG-009] Save Plan closed modal without API call',
});

/**
 * BUG-010 — Billing: Save Template doesn't call API
 * src: billing/page.tsx:108-111 — saveTemplate() just closes modal
 * Fix: call POST/PUT /gyms/{gymId}/session-package-templates
 */
const saveTemplateCallsApi = saveCallsApi({
  tab: 'Packages',
  newButton: 'New Template',
  urlPart: 'package',
  name: 'Test Template API',
  bugMessage: '[BUG-010] Save Template closed modal without API call',
});

test.describe(SUITE, () => {
  test.beforeAll(warmupStaffLogin);

  test.describe('7. Billing', () => {
    test.beforeEach(async ({ page }) => {
      await goTo(page, '/billing');
    });

    test('billing page loads with KPI cards', loadsWithKpis);
    test('Payments tab shows payment table', tabShows('Payments', 'th:has-text("Member")'));
    test('Plans tab shows membership plans', tabShows('Plans', 'text=Membership Plans'));
    test('Packages tab shows templates and active packages', tabShows('Packages', 'text=Session Package Templates'));
    test('[BUG-007] Record Payment must NOT fire native alert()', recordPaymentNoAlert);
    test('[BUG-008] Assign Package must NOT fire native alert()', assignPackageNoAlert);
    test('[BUG-009] Save Plan must POST/PUT to API', savePlanCallsApi);
    test('[BUG-010] Save Package Template must POST/PUT to API', saveTemplateCallsApi);
  });
});
