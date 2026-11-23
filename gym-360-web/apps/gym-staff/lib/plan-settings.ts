/** pt.training_plan — the gym's plan settings as the API returns them. */
export interface PlanSettings {
  inactiveDays: number;
  minInactiveDays: number;
  maxInactiveDays: number;
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
