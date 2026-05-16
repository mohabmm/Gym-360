export type PlanTier = 'free' | 'starter' | 'pro' | 'business';

export interface SubscriptionPlan {
  id: string;
  code: string;
  name: string;
  nameAr?: string;
  description?: string;
  descriptionAr?: string;
  monthlyPriceEgp: number;
  annualPriceEgp: number;
  memberLimit: number;
  trainerLimit: number;
  staffLimit: number;
  isActive: boolean;
  isPublic: boolean;
  sortOrder: number;
  createdAt: string;
  updatedAt?: string;

  /** @deprecated Derived from `code` on the client for UI badges — prefer `code` on the server. */
  tier?: PlanTier;
}

export interface Feature {
  id: string;
  code: string;
  name: string;
  nameAr?: string;
  description?: string;
  descriptionAr?: string;
  category?: string;
  isActive: boolean;
  sortOrder?: number;
}

export interface PlanFeature {
  planId: string;
  featureId: string;
  /** Populated client-side by joining the feature list; NOT returned directly by the API. */
  featureCode?: string;
  /** Populated client-side by joining the feature list; NOT returned directly by the API. */
  featureName?: string;
  isEnabled: boolean;
  limitValue?: number | null;
}

export interface FeatureMatrixResponse {
  plans: SubscriptionPlan[];
  features: Feature[];
  matrix: PlanFeature[];
}

export interface FeatureOverride {
  id: string;
  gymId: string;
  featureId: string;
  featureCode: string;
  featureName: string;
  overrideValue: 'force_on' | 'force_off';
  reason?: string;
  createdAt: string;
  createdBy: string;
}

export interface FeatureMatrixCell {
  planId: string;
  featureId: string;
  isEnabled: boolean;
}

export interface GymSubscription {
  id: string;
  gymId: string;
  planId: string;
  planName: string;
  planTier: PlanTier;
  status: 'active' | 'suspended' | 'cancelled' | 'past_due';
  currentPeriodStart: string;
  currentPeriodEnd: string;
  monthlyAmount: number;
  nextBillingDate: string;
  paymentMethod: string;
}

export interface PlanStorageLimit {
  planId: string;
  maxPhotoStorageGb: number;
  maxVideoStorageGb: number;
  maxDocumentStorageGb: number;
}

export interface GymStorageUsage {
  gymId: string;
  photoStorageUsedGb: number;
  videoStorageUsedGb: number;
  documentStorageUsedGb: number;
  totalUsedGb: number;
  totalLimitGb: number;
}

export interface PlanChangeLog {
  id: string;
  gymId: string;
  gymName: string;
  previousPlanId: string;
  previousPlanName: string;
  newPlanId: string;
  newPlanName: string;
  changedBy: string;
  changedByName: string;
  reason?: string;
  changedAt: string;
}

export interface DeployRequest {
  scope: 'all' | 'gym' | 'plan';
  targetId?: string;
  message?: string;
}
