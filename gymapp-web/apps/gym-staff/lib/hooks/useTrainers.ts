import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useSession } from 'next-auth/react';
import apiClient from '@/lib/api-client';
import type { TrainerListItem } from '@gymapp/ui';

export interface TrainerClientItem {
  id: string;
  name: string;
  email?: string;
  phone?: string;
  memberNumber?: string;
  joinedAt: string;
}

export interface TrainerSessionItem {
  id: string;
  memberId: string;
  memberName?: string;
  scheduledStart: string;
  scheduledEnd?: string;
  status: string;
  type?: string;
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function normaliseTrainer(t: any): TrainerListItem {
  const statusMap: Record<string, TrainerListItem['status']> = {
    approved: 'active',
    pending: 'pending_approval',
    suspended: 'suspended',
    inactive: 'inactive',
  };
  const rawEmail: string | undefined = t.email ?? undefined;
  const isPlaceholder = typeof rawEmail === 'string' && rawEmail.endsWith('@placeholder.gymapp.local');
  return {
    id: t.trainerId ?? t.id ?? '',
    name: t.name ?? ([t.firstName, t.lastName].filter(Boolean).join(' ') || ''),
    nameAr: t.nameAr ?? '',
    email: isPlaceholder ? undefined : rawEmail,
    phone: t.phoneNumber ?? t.phone ?? undefined,
    status: statusMap[(t.status ?? '').toLowerCase()] ?? 'inactive',
    specializations: t.specializations ?? [],
    bio: t.bio ?? undefined,
    activeMemberCount: t.activeMemberCount ?? 0,
    rating: t.rating ?? 0,
    monthlyScore: t.monthlyScore ?? 0,
  };
}

export function useTrainers() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  return useQuery<TrainerListItem[]>({
    queryKey: ['trainers', gymId],
    enabled: !!gymId,
    queryFn: async () => {
      const res = await apiClient.get(`/gyms/${gymId}/trainers`, { params: { page: 1, pageSize: 100 } });
      const raw: unknown[] = res.data.data?.items ?? res.data.data ?? [];
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      return (raw as any[]).map(normaliseTrainer);
    },
  });
}

export function useTrainer(id: string) {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const isAlias = /^t\d+$/i.test(id);
  return useQuery<TrainerListItem | undefined>({
    queryKey: ['trainers', gymId, id],
    enabled: !!gymId && !!id && !isAlias,
    queryFn: async () => {
      const res = await apiClient.get(`/gyms/${gymId}/trainers/${id}`);
      const t = res.data.data;
      return t ? normaliseTrainer(t) : undefined;
    },
  });
}

export function useCreateTrainer() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (data: {
      name: string; email?: string; phone: string;
    }) => {
      const [firstName, ...rest] = data.name.trim().split(/\s+/);
      const lastName = rest.join(' ') || firstName;
      const userRes = await apiClient.post('/identity/users/staff-create-trainer', {
        firstName,
        lastName,
        phone: data.phone,
        email: data.email || null,
      });
      const userId =
        userRes.data?.data?.userId ?? userRes.data?.data?.id ?? userRes.data?.data ?? null;
      if (!userId) {
        throw new Error('Identity service did not return a user id.');
      }
      const res = await apiClient.post(`/gyms/${gymId}/trainers`, {
        trainerId: userId,
      });
      return res.data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['trainers', gymId] }),
  });
}

export function useTrainerClients(trainerId: string) {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  return useQuery<TrainerClientItem[]>({
    queryKey: ['trainer-clients', gymId, trainerId],
    enabled: !!gymId && !!trainerId,
    queryFn: async () => {
      const res = await apiClient.get(`/gyms/${gymId}/trainers/${trainerId}/clients`);
      const raw: unknown[] = res.data?.data ?? [];
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      return (raw as any[]).map((m) => ({
        id: m.memberId,
        name: [m.firstName, m.lastName].filter(Boolean).join(' ') || '—',
        email: typeof m.email === 'string' && !m.email.endsWith('@placeholder.gymapp.local') ? m.email : undefined,
        phone: m.phoneNumber ?? undefined,
        memberNumber: m.memberNumber ?? undefined,
        joinedAt: m.joinedAt,
      }));
    },
  });
}

export function useTrainerSessions(trainerId: string) {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  return useQuery<TrainerSessionItem[]>({
    queryKey: ['trainer-sessions', gymId, trainerId],
    enabled: !!gymId && !!trainerId,
    queryFn: async () => {
      const res = await apiClient.get('/scheduling/sessions', {
        params: { gymId, trainerId, page: 1, pageSize: 50 },
        headers: { 'X-Gym-Id': gymId },
      });
      const raw: unknown[] = res.data?.data?.items ?? res.data?.data ?? [];
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      return (raw as any[]).map((s) => ({
        id: s.id,
        memberId: s.memberId,
        scheduledStart: s.scheduledAt,
        status: s.status,
        type: s.sessionType,
      }));
    },
  });
}

export function useUpdateTrainerProfile(trainerId: string) {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (payload: { bio?: string | null; specializations?: string[] | null }) => {
      const res = await apiClient.put(`/gyms/${gymId}/trainers/${trainerId}/profile`, payload);
      return res.data;
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['trainers', gymId] });
      qc.invalidateQueries({ queryKey: ['trainers', gymId, trainerId] });
    },
  });
}

export function useApproveTrainer() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (trainerId: string) => {
      const res = await apiClient.post(`/gyms/${gymId}/trainers/${trainerId}/approve`);
      return res.data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['trainers', gymId] }),
  });
}

export function useRejectTrainer() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ trainerId, reason }: { trainerId: string; reason: string }) => {
      const res = await apiClient.post(`/gyms/${gymId}/trainers/${trainerId}/reject`, { reason });
      return res.data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['trainers', gymId] }),
  });
}
