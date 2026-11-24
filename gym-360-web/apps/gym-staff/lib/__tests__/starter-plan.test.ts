import { test, expect } from '@playwright/test';
import { starterPlanText, unlockState, unlockStatusText, type LimitedPlanStatus, type ProgramUnlock } from '../starter-plan';

const row = (over: Partial<ProgramUnlock> = {}): ProgramUnlock => ({
  trainerId: 'c1', trainerName: 'Coach', unlocked: false, length: 'OneWeek', changedAt: null, limitedPlan: null, ...over,
});
const plan = (limit: LimitedPlanStatus['limit'], hasEnded: boolean): LimitedPlanStatus =>
  ({ planId: 'p1', limit, startDate: '2026-11-24', endsOn: '2026-11-30', hasEnded });

test('pt.starter_plan: the status follows the unlock first, then the 3-day or one-week plan', () => {
  expect(unlockState(row({ unlocked: true, limitedPlan: plan('Starter', true) }))).toBe('unlocked');
  expect(unlockState(row())).toBe('noPlan');
  expect(unlockState(row({ limitedPlan: plan('Starter', true) }))).toBe('starterEnded');
  expect(unlockState(row({ limitedPlan: plan('OneWeek', false) }))).toBe('weekRunning');
});

test('a running one-week program shows its last day, Arabic first', () => {
  expect(unlockStatusText(row({ limitedPlan: plan('OneWeek', false) }), starterPlanText('ar'), (d) => d))
    .toBe('برنامج أسبوع شغال · آخر يوم 2026-11-30');
  expect(unlockStatusText(row({ limitedPlan: plan('OneWeek', true) }), starterPlanText('en'), (d) => d))
    .toBe('One-week program is over – unlock another if they are still away');
});

test('the desk sees which length is unlocked and a running 30-day program', () => {
  const ar = starterPlanText('ar');
  expect(unlockStatusText(row({ unlocked: true, length: 'ThirtyDays' }), ar, (d) => d)).toBe('مفتوح: برنامج ٣٠ يوم – المدرب يقدر يدّيه');
  expect(unlockState(row({ limitedPlan: plan('ThirtyDays', false) }))).toBe('monthRunning');
  expect(unlockStatusText(row({ limitedPlan: plan('ThirtyDays', true) }), starterPlanText('en'), (d) => d))
    .toBe('30-day program is over – unlock another if they are still away');
});
