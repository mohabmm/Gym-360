'use client';

import { useCallback, useEffect, useMemo } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import {
  DEFAULT_REFRESH_SECONDS,
  FEATURE_DISABLED_EVENT,
  getFeatureFlagService,
  type FeatureFlagSnapshot,
} from '../lib/feature-flags';

export const FEATURE_FLAGS_QUERY_KEY = 'feature-flags';

export interface FeatureFlagsResult {
  snapshot: FeatureFlagSnapshot;
  isEnabled: (key: string) => boolean;
  /** True once the flags come from the server or the device cache (not the bare defaults). */
  isSettled: boolean;
  refresh: () => void;
}

/**
 * Resolved flags for the signed-in gym. Starts from the device cache (or pt_module defaults),
 * then fetches; refetches on window focus, every `refreshAfterSeconds`, on FEATURE_DISABLED
 * and when the app invalidates [FEATURE_FLAGS_QUERY_KEY] (SignalR config_updated).
 */
export function useFeatureFlags(gymId: string | null | undefined): FeatureFlagsResult {
  const queryClient = useQueryClient();
  const service = getFeatureFlagService();
  const gym = gymId ?? '';

  const query = useQuery<FeatureFlagSnapshot>({
    queryKey: [FEATURE_FLAGS_QUERY_KEY, gym],
    queryFn: () => service.refresh(gym),
    enabled: !!gym,
    initialData: () => service.load(gym),
    initialDataUpdatedAt: 0,
    staleTime: 30_000,
    refetchOnWindowFocus: true,
    refetchInterval: (q) => (q.state.data?.refreshAfterSeconds ?? DEFAULT_REFRESH_SECONDS) * 1000,
    retry: false,
  });

  const refresh = useCallback(() => {
    void queryClient.invalidateQueries({ queryKey: [FEATURE_FLAGS_QUERY_KEY] });
  }, [queryClient]);

  useEffect(() => {
    if (typeof window === 'undefined') return;
    window.addEventListener(FEATURE_DISABLED_EVENT, refresh);
    return () => window.removeEventListener(FEATURE_DISABLED_EVENT, refresh);
  }, [refresh]);

  const snapshot = query.data ?? service.load(gym);
  const isEnabled = useCallback((key: string) => snapshot.enabled.has(key), [snapshot]);

  return useMemo(
    () => ({ snapshot, isEnabled, isSettled: snapshot.source !== 'defaults' || query.isFetched, refresh }),
    [snapshot, isEnabled, query.isFetched, refresh],
  );
}
