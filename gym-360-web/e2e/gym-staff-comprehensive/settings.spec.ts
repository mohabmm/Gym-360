import { test, expect } from '@playwright/test';
import { BASE_URL, SUITE, goTo, warmupStaffLogin } from '../helpers/gym-staff-comprehensive';
import { isMutation, watchRequest, type PageFixtures } from '../helpers/ui';

test.use({ baseURL: BASE_URL });

// ── 17. SETTINGS ──────────────────────────────────────────────────────────

async function loadsGymProfile({ page }: PageFixtures) {
  await expect(page.locator('h1').first()).toContainText('Settings');
  await expect(page.locator('text=Gym Profile').first()).toBeVisible({ timeout: 8000 });
}

async function fieldsPrefilled({ page }: PageFixtures) {
  const nameInput = page.locator('input').first();
  const val = await nameInput.inputValue();
  expect(val.length).toBeGreaterThan(0);
}

/** Returns a test body expecting a section heading (8s) followed by its items. */
function sectionShows(heading: string, items: string[]) {
  return async ({ page }: PageFixtures) => {
    await expect(page.locator(`text=${heading}`).first()).toBeVisible({ timeout: 8000 });
    for (const item of items) {
      await expect(page.locator(`text=${item}`).first()).toBeVisible();
    }
  };
}

/**
 * BUG-016 — Settings: Save Changes uses toast but doesn't call API
 * src: settings/page.tsx:43-45 — handleSave() only calls toast.success(), no API call
 * Fix: call PUT /gyms/{gymId} with updated gym profile
 */
async function saveCallsApi({ page }: PageFixtures) {
  const apiCall = watchRequest(page, (req) => isMutation(req, ['PUT', 'PATCH'], '/gyms/'));

  const saveBtn = page.locator('button:has-text("Save Changes")').first();
  if (!(await saveBtn.isVisible())) return;
  await saveBtn.click();
  await page.waitForTimeout(1500);
  if (!apiCall.seen) {
    console.log('[BUG-016] Save Changes showed toast without API call — settings not persisted');
  }
  expect(apiCall.seen).toBe(true);
}

/**
 * BUG-017 — Settings: Field values are hardcoded, not loaded from API
 * src: settings/page.tsx:29-38 — useState with hardcoded "First Gym", "01272463444", etc.
 * Fix: load from GET /gyms/{gymId} on mount
 */
async function loadsGymFromApi({ page }: PageFixtures) {
  const gymApi = watchRequest(page, (req) => req.method() === 'GET' && /\/gyms\/[^/]+$/.test(req.url()));
  await page.goto('/settings');
  await page.waitForTimeout(3000);
  if (!gymApi.seen) {
    console.log('[BUG-017] Settings page did not load gym data from API on mount — hardcoded values');
  }
  expect(gymApi.seen).toBe(true);
}

test.describe(SUITE, () => {
  test.beforeAll(warmupStaffLogin);

  test.describe('17. Settings', () => {
    test.beforeEach(async ({ page }) => {
      await goTo(page, '/settings');
    });

    test('settings page loads with Gym Profile section', loadsGymProfile);
    test('gym name, phone, email fields pre-filled', fieldsPrefilled);
    test('Amenities section has checkboxes', sectionShows('Amenities', ['Parking', 'Showers']));
    test('Notifications toggles visible', sectionShows('Notifications', ['Session Reminders', 'Expiry Warnings']));
    test('Operating Hours section visible', sectionShows('Operating Hours', []));
    test('[BUG-016] Save Changes must call API (not just show toast)', saveCallsApi);
    test('[BUG-017] Gym name field must load from API on mount', loadsGymFromApi);
  });
});
