import type { Gym, SubscriptionPlan } from '@gym360/ui';

export const GYM_DETAIL_TABS = ['Overview', 'Members', 'Trainers', 'Staff', 'Revenue', 'Subscription & Features', 'Settings'] as const;

export type GymDetailTab = (typeof GYM_DETAIL_TABS)[number];

/** Which gym-detail modal is currently open (at most one at a time). */
export type GymDetailModal = 'suspend' | 'reactivate' | 'delete' | 'edit' | 'changePlan' | 'staff';

/** Matrix lookup: planId -> featureId -> isEnabled (backend keys on ids, not codes). */
export type FeatureMatrixLookup = Record<string, Record<string, boolean>>;

export { buildFeatureMatrix } from './plans';

/** Plan-level default for a feature; OFF when the gym has no plan. */
export function planDefaultFor(matrix: FeatureMatrixLookup, plan: SubscriptionPlan | undefined, featureId: string): boolean {
  if (!plan) return false;
  return matrix[plan.id]?.[featureId] ?? false;
}

export interface GymPlanSummary {
  planNameLower: string;
  planLabel: string;
  ownerName: string;
  plan: SubscriptionPlan | undefined;
}

export function summarizeGymPlan(gym: Gym, plans: SubscriptionPlan[]): GymPlanSummary {
  const planName = gym.planName ?? '';
  return {
    planNameLower: planName.toLowerCase(),
    planLabel: planName || 'No Plan',
    ownerName: gym.ownerName || '—',
    plan: planName ? plans.find((p) => p.name === planName) : undefined,
  };
}

export function errorMessage(err: unknown, fallback: string): string {
  return err instanceof Error ? err.message : fallback;
}

/** Case/whitespace-insensitive gym-name confirmation used by the delete flow. */
export function confirmsGymName(typed: string, gymName: string | undefined): boolean {
  return typed.trim().toLowerCase() === (gymName ?? '').trim().toLowerCase();
}

/** Usage bar width (0–100) for `used` out of `limit`. */
export function usagePercent(used: number, limit: number): number {
  return Math.min((used / Math.max(limit, 1)) * 100, 100);
}

/** Staff count isn't on the typed Gym DTO; read whichever field the API sent. */
export function gymStaffCount(gym: Gym): number | undefined {
  const raw = gym as unknown as { activeStaffCount?: number; staffCount?: number };
  return raw.activeStaffCount ?? raw.staffCount;
}

export function pluralSuffix(count: number): string {
  return count === 1 ? '' : 's';
}
