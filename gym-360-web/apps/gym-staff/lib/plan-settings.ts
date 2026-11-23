import type { ChallengeSettings } from './challenge';

/** pt.training_plan — the gym's plan settings as the API returns them. */
export interface PlanSettings {
  inactiveDays: number;
  minInactiveDays: number;
  maxInactiveDays: number;
  /** null when mem.first_month_challenge is off for the gym. */
  challenge?: ChallengeSettings | null;
}

/** What a save sends: only the parts that change. Blank reward texts go back to the default. */
export interface PlanSettingsChange {
  inactiveDays?: number;
  challenge?: { target: number; windowDays: number; rewardTextAr: string; rewardTextEn: string };
}

/** The number typed in the field; '' while it is empty. */
export type DaysInput = number | '';

export function isInactiveDaysValid(days: DaysInput, settings: PlanSettings): days is number {
  return typeof days === 'number' && Number.isInteger(days) && days >= settings.minInactiveDays && days <= settings.maxInactiveDays;
}

/** Save only a valid change. */
export function canSaveInactiveDays(days: DaysInput, settings: PlanSettings): boolean {
  return isInactiveDaysValid(days, settings) && days !== settings.inactiveDays;
}
