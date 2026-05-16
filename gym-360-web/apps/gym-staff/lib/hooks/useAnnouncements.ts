import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useSession } from 'next-auth/react';
import apiClient from '@/lib/api-client';
import type { Announcement } from '@gym360/ui';

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function normaliseAnnouncement(a: any): Announcement {
  const audienceMap: Record<string, Announcement['targetAudience']> = {
    all: 'all', members: 'members', trainers: 'trainers', staff: 'staff',
  };
  return {
    id: a.id ?? '',
    gymId: a.gymId ?? '',
    title: a.title ?? '',
    titleAr: a.titleAr ?? '',
    body: a.body ?? a.content ?? '',
    bodyAr: a.bodyAr ?? '',
    targetAudience: audienceMap[(a.targetAudience ?? '').toLowerCase()] ?? 'all',
    targetSection: a.targetSection ?? undefined,
    isActive: a.isActive ?? (a.publishedAt != null),
    publishedAt: a.publishedAt ?? '',
    expiresAt: a.expiresAt ?? undefined,
    createdBy: a.createdBy ?? '',
    createdByName: a.createdByName ?? '',
  };
}

export function useAnnouncements() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  return useQuery<Announcement[]>({
    queryKey: ['announcements', gymId],
    enabled: !!gymId,
    queryFn: async () => {
      const res = await apiClient.get(`/gyms/${gymId}/announcements`, { params: { page: 1, pageSize: 50 } });
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const raw: any[] = res.data.data?.items ?? res.data.data ?? [];
      return raw.map(normaliseAnnouncement);
    },
  });
}

export function useCreateAnnouncement() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const userId = session?.user?.id;
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (data: {
      title: string; titleAr: string; body: string; bodyAr: string;
      targetAudience: string; targetSection?: string; isActive: boolean; expiresAt?: string;
    }) => {
      const res = await apiClient.post(`/gyms/${gymId}/announcements`, {
        title: data.title,
        content: data.body,
        category: 'general',
        targetAudience: data.targetAudience,
        scheduledAt: data.isActive ? null : data.expiresAt,
        createdBy: userId,
      });
      return res.data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['announcements', gymId] }),
  });
}

export function useUpdateAnnouncement() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, ...data }: { id: string; isActive?: boolean; [key: string]: unknown }) => {
      const res = await apiClient.put(`/gyms/${gymId}/announcements/${id}`, data);
      return res.data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['announcements', gymId] }),
  });
}

export function useDeleteAnnouncement() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/gyms/${gymId}/announcements/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['announcements', gymId] }),
  });
}
