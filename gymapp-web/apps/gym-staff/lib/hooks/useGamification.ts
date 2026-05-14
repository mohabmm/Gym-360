import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useSession } from 'next-auth/react';
import apiClient from '@/lib/api-client';

export interface LeaderboardEntry {
  rank: number; memberId: string; memberName: string; memberNameAr?: string;
  score: number; workoutsCompleted: number; streakDays: number; change?: number;
}

export interface TrainerRankingEntry {
  rank: number; trainerId: string; trainerName: string; trainerNameAr?: string;
  monthlyScore: number; rating: number; totalRatings: number; sessionsCompleted: number;
}

export interface Testimonial {
  id: string; memberId: string; memberName: string; trainerId: string; trainerName: string;
  content: string; rating: number; status: 'pending' | 'approved' | 'rejected'; createdAt: string;
}

export function useMemberLeaderboard() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  return useQuery<LeaderboardEntry[]>({
    queryKey: ['leaderboard', 'members', gymId],
    enabled: !!gymId,
    queryFn: async () => {
      const res = await apiClient.get('/gamification/leaderboards/Attendance', { params: { gymId } });
      const data = res.data.data;
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const entries: any[] = data?.entries ?? data?.items ?? (Array.isArray(data) ? data : []);
      return entries.map((e, i): LeaderboardEntry => ({
        rank: e.position ?? e.rank ?? i + 1,
        memberId: e.memberId ?? '',
        memberName: e.memberName ?? '',
        score: e.score ?? 0,
        workoutsCompleted: e.workoutsCompleted ?? 0,
        streakDays: e.streakDays ?? 0,
        change: e.change ?? e.delta ?? undefined,
      }));
    },
  });
}

export function useTrainerRankings() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  return useQuery<TrainerRankingEntry[]>({
    queryKey: ['leaderboard', 'trainers', gymId],
    enabled: !!gymId,
    queryFn: async () => {
      const [rankRes, trainerRes] = await Promise.all([
        apiClient.get('/gamification/trainers/leaderboard', { params: { page: 1, pageSize: 20 } }),
        apiClient.get(`/gyms/${gymId}/trainers`, { params: { page: 1, pageSize: 100 } }),
      ]);
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const scores: any[] = rankRes.data.data?.items ?? rankRes.data.data ?? [];
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const trainers: any[] = trainerRes.data.data?.items ?? trainerRes.data.data ?? [];
      const trainerMap = new Map(
        trainers.map((t) => [
          t.trainerId ?? t.id ?? '',
          [t.firstName, t.lastName].filter(Boolean).join(' ') || t.name || '',
        ])
      );
      return scores.map((s, i): TrainerRankingEntry => ({
        rank: i + 1,
        trainerId: s.trainerId ?? s.id ?? '',
        trainerName: trainerMap.get(s.trainerId ?? s.id ?? '') ?? s.trainerName ?? '',
        trainerNameAr: s.trainerNameAr ?? '',
        monthlyScore: s.normalizedScore ?? s.monthlyScore ?? 0,
        rating: s.rating ?? 0,
        totalRatings: s.totalRatings ?? 0,
        sessionsCompleted: s.sessionCount ?? s.sessionsCompleted ?? 0,
      }));
    },
  });
}

export function useTestimonials() {
  return useQuery<Testimonial[]>({
    queryKey: ['testimonials'],
    queryFn: async () => {
      const res = await apiClient.get('/gamification/testimonials/pending', { params: { page: 1, pageSize: 50 } });
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const raw: any[] = res.data.data?.items ?? (Array.isArray(res.data.data) ? res.data.data : []);
      return raw.map((t): Testimonial => ({
        id: t.id ?? '',
        memberId: t.memberId ?? '',
        memberName: t.memberName ?? '',
        trainerId: t.trainerId ?? '',
        trainerName: t.trainerName ?? '',
        content: t.content ?? t.commentText ?? '',
        rating: t.rating ?? 0,
        status: (t.status?.toLowerCase() ?? 'pending') as Testimonial['status'],
        createdAt: t.createdAt ?? '',
      }));
    },
  });
}

export function useModerateTestimonial() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, status }: { id: string; status: 'approved' | 'rejected' }) => {
      const res = await apiClient.put(`/gamification/testimonials/${id}/moderate`, { status });
      return res.data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['testimonials'] }),
  });
}
