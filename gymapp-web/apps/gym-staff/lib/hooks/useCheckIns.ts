import { useQuery } from '@tanstack/react-query';
import { useSession } from 'next-auth/react';
import apiClient from '@/lib/api-client';
import type { MemberCheckIn } from '@gymapp/ui';

export function useCheckIns() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  return useQuery<MemberCheckIn[]>({
    queryKey: ['check-ins', gymId],
    enabled: !!gymId,
    queryFn: async () => {
      const res = await apiClient.get(`/gyms/${gymId}/check-ins`, { params: { page: 1, pageSize: 100 } });
      return res.data.data?.items ?? res.data.data ?? [];
    },
  });
}
