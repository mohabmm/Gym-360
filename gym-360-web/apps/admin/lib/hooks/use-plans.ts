import { useMutation, useQuery } from '@tanstack/react-query';
import {
  apiGet,
  apiPost,
  apiPut,
  BILLING,
  ADMIN,
  useApiQuery,
  useInvalidateQueries,
} from '@gym360/ui';
import type {
  SubscriptionPlan,
  Feature,
  FeatureMatrixResponse,
  FeatureOverride,
  PlanChangeLog,
  GymSubscription,
  DeployRequest,
  ApiResponse,
} from '@gym360/ui';

export function usePlans() {
  return useApiQuery<SubscriptionPlan[]>(['plans'], BILLING.PLANS);
}

export function useFeatures() {
  return useApiQuery<Feature[]>(['features'], BILLING.FEATURES);
}

export function usePlanFeatureMatrix() {
  return useApiQuery<FeatureMatrixResponse>(['plan-features'], BILLING.PLAN_FEATURE_MATRIX);
}

export function useGymFeatureOverrides(gymId?: string) {
  return useApiQuery<FeatureOverride[]>(
    ['overrides', gymId ?? ''],
    gymId ? BILLING.GYM_OVERRIDES(gymId) : '/admin/feature-overrides',
    undefined,
    { enabled: !!gymId },
  );
}

export function usePlanChangeLogs() {
  return useApiQuery<{ items: PlanChangeLog[] }>(
    ['plan-change-log'],
    BILLING.PLAN_CHANGE_LOG,
    { page: 1, pageSize: 50 },
  );
}

export function useGymSubscription(gymId: string) {
  return useApiQuery<GymSubscription>(
    ['subscriptions', gymId],
    BILLING.GYM_SUBSCRIPTION(gymId),
    undefined,
    { enabled: !!gymId },
  );
}

export function useGymSubscriptions() {
  // Note: no cross-gym list endpoint exists in the backend yet. Callers should prefer
  // fetching per-gym via useGymSubscription(gymId).
  return useQuery<ApiResponse<GymSubscription[]>, Error>({
    queryKey: ['subscriptions'],
    queryFn: async () => ({ data: [] as GymSubscription[] }) as ApiResponse<GymSubscription[]>,
  });
}

export function useTogglePlanFeature() {
  const { invalidate } = useInvalidateQueries();
  return useMutation<
    ApiResponse<void>,
    Error,
    { planId: string; featureId: string; isEnabled: boolean; limitValue?: number | null }
  >({
    mutationFn: async ({ planId, featureId, isEnabled, limitValue }) =>
      apiPut(BILLING.PLAN_FEATURES(planId, featureId), { isEnabled, limitValue: limitValue ?? null }),
    onSuccess: () => {
      invalidate(['plan-features']);
    },
  });
}

export function useDeploy() {
  const { invalidate } = useInvalidateQueries();
  return useMutation<ApiResponse<unknown>, Error, DeployRequest>({
    mutationFn: async ({ scope, targetId }) =>
      // The backend's DeployChangesRequest requires a deployedBy GUID. We send an empty GUID
      // and let the API derive the deployer from the JWT via the UserContext helper.
      apiPost(ADMIN.DEPLOY(scope, targetId), { deployedBy: '00000000-0000-0000-0000-000000000000' }),
    onSuccess: () => {
      invalidate(['plan-features']);
      invalidate(['plans']);
      invalidate(['features']);
    },
  });
}

export function useChangePlan() {
  const { invalidate } = useInvalidateQueries();
  return useMutation<
    ApiResponse<void>,
    Error,
    { gymId: string; planId: string; reason?: string }
  >({
    mutationFn: async ({ gymId, planId, reason }) =>
      apiPut(BILLING.CHANGE_PLAN(gymId), { planId, reason: reason ?? null }),
    onSuccess: () => {
      invalidate(['subscriptions']);
      invalidate(['gyms']);
      invalidate(['plan-change-log']);
    },
  });
}
