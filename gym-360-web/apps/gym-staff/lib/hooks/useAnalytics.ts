import { useQuery } from '@tanstack/react-query';
import apiClient from '@/lib/api-client';
import { useGymId } from './query-helpers';

export function useGymAnalytics() {
  const gymId = useGymId();
  return useQuery({
    queryKey: ['gym-analytics', gymId],
    enabled: !!gymId,
    queryFn: async () => {
      const res = await apiClient.get(`/gyms/${gymId}/analytics`);
      return res.data?.data ?? null;
    },
  });
}

export function useClassesForUtilization() {
  const gymId = useGymId();
  return useQuery({
    queryKey: ['classes-utilization', gymId],
    enabled: !!gymId,
    queryFn: async () => {
      const res = await apiClient.get(`/gyms/${gymId}/classes`);
      return res.data?.data ?? [];
    },
  });
}

export function usePeakHours() {
  const gymId = useGymId();
  return useQuery({
    queryKey: ['peak-hours', gymId],
    enabled: !!gymId,
    queryFn: async () => {
      const now = new Date();
      const from = new Date(now.getFullYear(), now.getMonth(), 1).toISOString();
      const to = now.toISOString();
      const tzOffset = -now.getTimezoneOffset();
      const res = await apiClient.get(`/gyms/${gymId}/analytics/peak-hours`, {
        params: { from, to, tzOffsetMinutes: tzOffset },
      });
      return res.data.data ?? null;
    },
  });
}
