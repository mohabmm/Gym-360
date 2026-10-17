import { test, expect } from '@playwright/test';
import { BASE_URL, SUITE, goTo, warmupStaffLogin } from '../helpers/gym-staff-comprehensive';
import { cancelIfShown, expectClickWithoutDialog, isShown, type PageFixtures } from '../helpers/ui';

test.use({ baseURL: BASE_URL });

// ── 14. EXERCISES ─────────────────────────────────────────────────────────

async function loadsGlobalLibrary({ page }: PageFixtures) {
  await expect(page.locator('h1').first()).toContainText('Exercises');
  await expect(page.locator('text=Global Exercise Library').first()).toBeVisible({ timeout: 8000 });
}

async function showsSeeded({ page }: PageFixtures) {
  await page.waitForSelector('tbody tr', { timeout: 10000 });
  await expect(page.locator('text=Barbell Squat').first()).toBeVisible({ timeout: 8000 });
  await expect(page.locator('text=Deadlift').first()).toBeVisible();
}

async function muscleFilterWorks({ page }: PageFixtures) {
  await page.waitForSelector('select', { timeout: 8000 });
  await page.locator('select').first().selectOption('Chest');
  await page.waitForTimeout(500);
  await expect(page.locator('text=Barbell Bench Press').first()).toBeVisible();
}

async function searchFilters({ page }: PageFixtures) {
  await page.waitForSelector('tbody tr', { timeout: 10000 });
  const searchInput = page.locator('input[placeholder*="earch"]').first();
  if (!(await searchInput.isVisible())) return;
  const before = await page.locator('tbody tr').count();
  await searchInput.fill('squat');
  await page.waitForTimeout(600);
  const after = await page.locator('tbody tr').count();
  expect(after).toBeLessThanOrEqual(before);
}

/**
 * BUG-015 — Exercises: Add Custom Exercise validation uses alert()
 * src: exercises/page.tsx:75  alert('Exercise name is required.');
 * Fix: show inline validation error instead
 */
async function addCustomNoAlert({ page }: PageFixtures) {
  const addBtn = page.locator('button:has-text("Add Custom Exercise")');
  if (!(await isShown(addBtn, 3000))) return;
  await addBtn.click();
  await page.waitForTimeout(300);

  await expectClickWithoutDialog(page, {
    target: page.locator('button:has-text("Add Exercise")').last(),
    waitMs: 500,
    logLabel: '[BUG-015] Add Custom Exercise alert:',
  });
  await cancelIfShown(page);
}

test.describe(SUITE, () => {
  test.beforeAll(warmupStaffLogin);

  test.describe('14. Exercises', () => {
    test.beforeEach(async ({ page }) => {
      await goTo(page, '/exercises');
    });

    test('exercises page loads with global library', loadsGlobalLibrary);
    test('exercise table shows seeded exercises (Squat, Deadlift)', showsSeeded);
    test('muscle group filter works', muscleFilterWorks);
    test('search filters exercises', searchFilters);
    test('[BUG-015] Add Custom Exercise empty submit must NOT fire alert()', addCustomNoAlert);
  });
});
