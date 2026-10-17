import type { PlanFeature, SubscriptionPlan } from '@gym360/ui';

export type PlanFormState = {
  code: string;
  name: string;
  description: string;
  monthlyPriceEgp: number;
  annualPriceEgp: number;
  memberLimit: number;
  trainerLimit: number;
  staffLimit: number;
  isPublic: boolean;
  isActive: boolean;
  sortOrder: number;
};

export const EMPTY_PLAN_FORM: PlanFormState = {
  code: '',
  name: '',
  description: '',
  monthlyPriceEgp: 0,
  annualPriceEgp: 0,
  memberLimit: 100,
  trainerLimit: 5,
  staffLimit: 3,
  isPublic: true,
  isActive: true,
  sortOrder: 0,
};

export const FEATURE_CATEGORIES = [
  'Training', 'Nutrition', 'Progress',
  'Scheduling', 'Billing', 'Communication',
  'Gamification', 'Settings', 'Gyms',
  'Identity', 'Analytics',
] as const;

// Derive a display tier from the plan code (backend has no tier column).
export function tierOf(plan: SubscriptionPlan): string {
  const c = plan.code?.toLowerCase() ?? '';
  if (c.includes('free')) return 'free';
  if (c.includes('business') || c.includes('enterprise')) return 'business';
  if (c.includes('pro')) return 'pro';
  return 'starter';
}

export function planToForm(p: SubscriptionPlan): PlanFormState {
  return {
    code: p.code ?? '',
    name: p.name,
    description: p.description ?? '',
    monthlyPriceEgp: p.monthlyPriceEgp ?? 0,
    annualPriceEgp: p.annualPriceEgp ?? 0,
    memberLimit: p.memberLimit ?? 0,
    trainerLimit: p.trainerLimit ?? 0,
    staffLimit: p.staffLimit ?? 0,
    isPublic: p.isPublic ?? true,
    isActive: p.isActive ?? true,
    sortOrder: p.sortOrder ?? 0,
  };
}

function sharedPlanBody(form: PlanFormState) {
  return {
    name: form.name,
    description: form.description || null,
    monthlyPrice: form.monthlyPriceEgp,
    annualPrice: form.annualPriceEgp,
    memberLimit: form.memberLimit,
    trainerLimit: form.trainerLimit,
    staffLimit: form.staffLimit,
    isPublic: form.isPublic,
  };
}

// POST /admin/plans
export function toCreatePlanBody(form: PlanFormState) {
  const { name, ...rest } = sharedPlanBody(form);
  return { code: form.code, name, ...rest, sortOrder: form.sortOrder };
}

// PUT /admin/plans/{id} — all fields optional; backend ignores Code on update.
export function toUpdatePlanBody(form: PlanFormState) {
  return { ...sharedPlanBody(form), isActive: form.isActive, sortOrder: form.sortOrder };
}

export function hasNameAndCode(form: { name: string; code: string }): boolean {
  return !!form.name.trim() && !!form.code.trim();
}

/** Build matrix lookup: planId -> featureId -> isEnabled */
export function buildFeatureMatrix(matrix: PlanFeature[]): Record<string, Record<string, boolean>> {
  return matrix.reduce<Record<string, Record<string, boolean>>>((acc, pf) => {
    if (!acc[pf.planId]) acc[pf.planId] = {};
    acc[pf.planId]![pf.featureId] = pf.isEnabled;
    return acc;
  }, {});
}

export function matrixCellKey(planId: string, featureId: string): string {
  return `${planId}:${featureId}`;
}

export async function withBusy(setBusy: (busy: boolean) => void, work: () => Promise<void>): Promise<void> {
  setBusy(true);
  try {
    await work();
  } finally {
    setBusy(false);
  }
}
