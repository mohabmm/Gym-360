import { useMutation } from '@tanstack/react-query';
import {
  apiDelete,
  apiPut,
  FEATURE_FLAGS,
  useApiQuery,
  useInvalidateQueries,
  type ApiResponse,
  type FeatureCatalogItem,
  type FeaturePreset,
  type FlagAuditEntry,
  type FlagPage,
  type GymFlagDetail,
  type GymFlagSummary,
} from '@gym360/ui';

const KEYS = {
  gyms: 'ff-gyms',
  gym: 'ff-gym',
  presets: 'ff-presets',
  audit: 'ff-audit',
  catalog: 'ff-catalog',
} as const;

export function useFlagCatalog() {
  return useApiQuery<FeatureCatalogItem[]>([KEYS.catalog], FEATURE_FLAGS.CATALOG, undefined, { staleTime: 5 * 60_000 });
}

export function useFlagGyms(search: string, preset: string) {
  return useApiQuery<FlagPage<GymFlagSummary>>(
    [KEYS.gyms, search, preset],
    FEATURE_FLAGS.GYMS,
    { search: search || undefined, preset: preset || undefined, page: 1, pageSize: 200 },
  );
}

export function useGymFlags(gymId: string) {
  return useApiQuery<GymFlagDetail>([KEYS.gym, gymId], FEATURE_FLAGS.GYM(gymId), undefined, { enabled: !!gymId });
}

export function useFlagPresets() {
  return useApiQuery<FeaturePreset[]>([KEYS.presets], FEATURE_FLAGS.PRESETS);
}

export function useFlagAudit(gymId: string, featureKey: string, page: number) {
  return useApiQuery<FlagPage<FlagAuditEntry>>(
    [KEYS.audit, gymId, featureKey, String(page)],
    FEATURE_FLAGS.AUDIT,
    { gymId: gymId || undefined, featureKey: featureKey || undefined, page, pageSize: 50 },
  );
}

/** Every flag change affects gyms, presets and the audit log: refresh them all. */
function useInvalidateFlags() {
  const { invalidate } = useInvalidateQueries();
  return () => {
    invalidate([KEYS.gyms]);
    invalidate([KEYS.gym]);
    invalidate([KEYS.presets]);
    invalidate([KEYS.audit]);
  };
}

export function useSetGymPreset() {
  const invalidate = useInvalidateFlags();
  return useMutation<ApiResponse<{ affectedGyms: number }>, Error, { gymIds: string[]; presetCode: string }>({
    mutationFn: ({ gymIds, presetCode }) =>
      gymIds.length === 1
        ? apiPut(FEATURE_FLAGS.GYM_PRESET(gymIds[0]), { presetCode })
        : apiPut(FEATURE_FLAGS.BULK_PRESET, { gymIds, presetCode }),
    onSuccess: invalidate,
  });
}

export function useSetOverride() {
  const invalidate = useInvalidateFlags();
  return useMutation<ApiResponse<void>, Error, { gymId: string; key: string; enabled: boolean; reason?: string }>({
    mutationFn: ({ gymId, key, enabled, reason }) =>
      apiPut(FEATURE_FLAGS.GYM_OVERRIDE(gymId, key), { enabled, reason: reason || null }),
    onSuccess: invalidate,
  });
}

export function useClearOverride() {
  const invalidate = useInvalidateFlags();
  return useMutation<ApiResponse<void>, Error, { gymId: string; key: string }>({
    mutationFn: ({ gymId, key }) => apiDelete(FEATURE_FLAGS.GYM_OVERRIDE(gymId, key)),
    onSuccess: invalidate,
  });
}

export function useUpdatePreset() {
  const invalidate = useInvalidateFlags();
  return useMutation<ApiResponse<{ affectedGyms: number }>, Error, { code: string; values: Record<string, boolean> }>({
    mutationFn: ({ code, values }) => apiPut(FEATURE_FLAGS.PRESET(code), { values }),
    onSuccess: invalidate,
  });
}
