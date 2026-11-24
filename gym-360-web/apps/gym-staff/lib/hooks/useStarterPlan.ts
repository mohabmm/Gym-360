import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import apiClient from '@/lib/api-client';
import { useGymId } from '@/lib/hooks/query-helpers';
import type { MemberProgramUnlocks, ProgramUnlock, UnlockLength } from '@/lib/starter-plan';

const UNLOCKS = 'plan-unlocks';

/** pt.starter_plan — the member's coach and whether a one-week program is unlocked. */
export function useMemberProgramUnlocks(memberId: string, enabled = true) {
  const gymId = useGymId();
  return useQuery<MemberProgramUnlocks>({
    queryKey: [UNLOCKS, gymId, memberId],
    queryFn: async () => (await apiClient.get('/training/plan-unlocks', { params: { traineeId: memberId } })).data?.data,
    enabled: enabled && !!gymId && !!memberId,
    retry: false,
  });
}

/** Unlocks (unlocked true) one program of `length` for a coach and member, or takes it back. */
export function useSetProgramUnlock() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: { trainerId: string; traineeId: string; unlocked: boolean; length: UnlockLength }): Promise<ProgramUnlock> =>
      (await apiClient.put('/training/plan-unlocks', body)).data?.data,
    onSuccess: () => void qc.invalidateQueries({ queryKey: [UNLOCKS] }),
  });
}
