import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import apiClient from '@/lib/api-client';
import { useGymId } from '@/lib/hooks/query-helpers';
import type { ChallengeAward, PlanSummary } from '@/lib/challenge';

const AWARDS = 'challenge-awards';

/** mem.first_month_challenge — members who earned the reward; delivered false = still to hand over. */
export function useChallengeAwards(delivered: boolean) {
  const gymId = useGymId();
  return useQuery<ChallengeAward[]>({
    queryKey: [AWARDS, gymId, delivered],
    queryFn: async () => (await apiClient.get('/training/challenge/awards', { params: { delivered } })).data?.data ?? [],
    enabled: !!gymId,
  });
}

export function useSetAwardDelivered() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ awardId, delivered }: { awardId: string; delivered: boolean }): Promise<ChallengeAward> =>
      (await apiClient.put(`/training/challenge/awards/${awardId}/delivered`, { delivered })).data?.data,
    onSuccess: () => void qc.invalidateQueries({ queryKey: [AWARDS] }),
  });
}

/** pt.owner_dashboard — active plans, plan workouts this month and the challenge. */
export function usePlanSummary(enabled = true) {
  const gymId = useGymId();
  return useQuery<PlanSummary>({
    queryKey: ['plan-summary', gymId],
    queryFn: async () => (await apiClient.get('/training/reports/plan-summary')).data?.data,
    enabled: enabled && !!gymId,
    retry: false,
  });
}
