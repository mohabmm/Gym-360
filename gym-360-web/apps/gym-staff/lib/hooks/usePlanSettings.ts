import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useSession } from 'next-auth/react';
import apiClient from '@/lib/api-client';

import type { PlanSettings, PlanSettingsChange } from '@/lib/plan-settings';

export type { PlanSettings } from '@/lib/plan-settings';

const KEY = 'plan-settings';

export function usePlanSettings() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId ?? '';
  return useQuery<PlanSettings>({
    queryKey: [KEY, gymId],
    queryFn: async () => (await apiClient.get('/training/plan-settings')).data?.data,
    enabled: !!gymId,
  });
}

export function useSavePlanSettings() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (settings: PlanSettingsChange): Promise<PlanSettings> =>
      (await apiClient.put('/training/plan-settings', settings)).data?.data,
    onSuccess: () => void qc.invalidateQueries({ queryKey: [KEY] }),
  });
}
