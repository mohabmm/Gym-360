import { test, expect } from '@playwright/test';
import { canSaveInactiveDays, isInactiveDaysValid, type PlanSettings } from '../plan-settings';

const settings: PlanSettings = { inactiveDays: 5, minInactiveDays: 2, maxInactiveDays: 30 };

test('inactive days must be a whole number within the gym limits', () => {
  expect(isInactiveDaysValid(2, settings)).toBe(true);
  expect(isInactiveDaysValid(30, settings)).toBe(true);
  expect(isInactiveDaysValid(1, settings)).toBe(false);
  expect(isInactiveDaysValid(31, settings)).toBe(false);
  expect(isInactiveDaysValid(4.5, settings)).toBe(false);
  expect(isInactiveDaysValid('', settings)).toBe(false);
});

test('save is offered only for a valid change', () => {
  expect(canSaveInactiveDays(5, settings)).toBe(false);
  expect(canSaveInactiveDays(7, settings)).toBe(true);
  expect(canSaveInactiveDays(40, settings)).toBe(false);
});
