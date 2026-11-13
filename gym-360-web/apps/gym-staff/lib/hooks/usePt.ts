import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useSession } from 'next-auth/react';
import apiClient from '@/lib/api-client';

/** A trainee who finished a free trial and has not bought a package (pt.trial_end_alerts). */
export interface NotConvertedTrial {
  packageId: string;
  memberId: string;
  memberName: string;
  trainerName: string;
  finishedAt: string;
  daysSinceFinished: number;
  conversionWindowClosed: boolean;
}

export interface PtCoachRow {
  trainerId?: string;
  trainerName?: string;
  trialsFinished: number;
  trialsConverted: number;
  conversionRate: number;
  renewals: number;
  newPaidPackages: number;
  sessionsLogged: number;
}

export interface PtDashboard {
  totals: PtCoachRow & { activeTrials?: number; notConvertedTrials?: number };
  coaches: PtCoachRow[];
}

/** A session the trainee disputed, waiting for the desk (pt.dual_session_logging). */
export interface ReviewSession {
  id: string;
  memberId: string;
  trainerId: string;
  scheduledAt: string;
  memberConfirmation: string;
}

const KEYS = {
  notConverted: 'pt-not-converted',
  review: 'pt-review',
  dashboard: 'pt-dashboard',
} as const;

function useGymId() {
  const { data: session } = useSession();
  return session?.user?.gymId ?? '';
}

export function useNotConvertedTrials(enabled = true) {
  const gymId = useGymId();
  return useQuery<NotConvertedTrial[]>({
    queryKey: [KEYS.notConverted, gymId],
    queryFn: async () => (await apiClient.get('/billing/trials/not-converted')).data?.data ?? [],
    enabled: enabled && !!gymId,
  });
}

export function useSessionsForReview(enabled = true) {
  const gymId = useGymId();
  return useQuery<ReviewSession[]>({
    queryKey: [KEYS.review, gymId],
    queryFn: async () => (await apiClient.get('/scheduling/sessions/review')).data?.data ?? [],
    enabled: enabled && !!gymId,
  });
}

export function useReverseSession() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (sessionId: string) => {
      await apiClient.post(`/scheduling/sessions/${sessionId}/reverse`);
    },
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: [KEYS.review] });
      void qc.invalidateQueries({ queryKey: ['packages'] });
    },
  });
}

export function usePtDashboard(days: number, enabled = true) {
  const gymId = useGymId();
  return useQuery<PtDashboard>({
    queryKey: [KEYS.dashboard, gymId, days],
    queryFn: async () => {
      const to = new Date();
      const from = new Date(to.getTime() - days * 24 * 60 * 60 * 1000);
      const res = await apiClient.get('/billing/pt-dashboard', {
        params: { from: from.toISOString(), to: to.toISOString() },
      });
      return res.data?.data ?? { totals: emptyRow(), coaches: [] };
    },
    enabled: enabled && !!gymId,
  });
}

export function emptyRow(): PtCoachRow {
  return { trialsFinished: 0, trialsConverted: 0, conversionRate: 0, renewals: 0, newPaidPackages: 0, sessionsLogged: 0 };
}
