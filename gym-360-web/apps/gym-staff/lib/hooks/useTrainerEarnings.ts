import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '@/lib/api-client';

export interface TrainerRate {
  ratePerSession: number;
  currency: string;
  effectiveFrom: string;
}

export interface TrainerEarnings {
  totalSessions: number;
  totalEarningsEgp: number;
  currency: string;
  periodFrom: string;
  periodTo: string;
  records: Array<{ sessionId: string; memberId: string; rateApplied: number; earnedAt: string }>;
}

// --- API helpers ---
async function fetchTrainerRate(gymId: string, trainerId: string) {
  try {
    const res = await apiClient.get(`/gyms/${gymId}/trainers/${trainerId}/rate`);
    return res.data?.data as TrainerRate | null;
  } catch (e) {
    const status = (e as { response?: { status?: number } })?.response?.status;
    if (status === 404) return null;
    throw e;
  }
}

async function setTrainerRate(gymId: string, trainerId: string, rate: number) {
  // setByUserId derived from JWT on the backend.
  await apiClient.put(`/gyms/${gymId}/trainers/${trainerId}/rate`, {
    ratePerSession: rate,
  });
}

async function fetchTrainerEarnings(gymId: string, trainerId: string) {
  const now = new Date();
  const firstOfMonth = new Date(now.getFullYear(), now.getMonth(), 1)
    .toISOString()
    .split('T')[0];
  const today = now.toISOString().split('T')[0];
  const res = await apiClient.get(`/billing/trainers/${trainerId}/earnings`, {
    params: { gymId, from: firstOfMonth, to: today },
  });
  return res.data?.data as TrainerEarnings;
}

export function useTrainerRate(gymId: string, trainerId: string) {
  return useQuery({
    queryKey: ['trainer-rate', gymId, trainerId],
    queryFn: () => fetchTrainerRate(gymId, trainerId),
    enabled: !!gymId && !!trainerId,
  });
}

export function useTrainerEarnings(gymId: string, trainerId: string, enabled: boolean) {
  return useQuery({
    queryKey: ['trainer-earnings', gymId, trainerId],
    queryFn: () => fetchTrainerEarnings(gymId, trainerId),
    enabled: enabled && !!gymId && !!trainerId,
  });
}

export function useSetTrainerRate(gymId: string, trainerId: string, onSaved: () => void) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (rate: number) => setTrainerRate(gymId, trainerId, rate),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['trainer-rate', gymId, trainerId] });
      onSaved();
    },
  });
}
