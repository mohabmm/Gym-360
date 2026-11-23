import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useSession } from 'next-auth/react';
import apiClient from '@/lib/api-client';
import type { PlanDocument, PlanGoal } from '@/lib/plan-draft';

/** pt.training_plan — a template as listed (no days). */
export interface PlanTemplateSummary {
  id: string;
  scope: 'Gym' | 'Coach' | string;
  name: string;
  nameAr?: string | null;
  goal: PlanGoal;
  goalText?: string | null;
  durationWeeks: number;
  daysPerWeek: number;
  isSample: boolean;
  canEdit: boolean;
  updatedAt: string;
}

export interface PlanTemplate {
  summary: PlanTemplateSummary;
  document: PlanDocument;
}

export interface LibraryExercise {
  id: string;
  name: string;
  nameAr?: string | null;
  demoMediaUrl?: string | null;
}

const KEY = 'plan-templates';

function useGymId() {
  const { data: session } = useSession();
  return session?.user?.gymId ?? '';
}

/** Gym templates (the first call seeds three Arabic samples). */
export function usePlanTemplates() {
  const gymId = useGymId();
  return useQuery<PlanTemplateSummary[]>({
    queryKey: [KEY, gymId],
    queryFn: async () => (await apiClient.get('/training/plan-templates')).data?.data ?? [],
    enabled: !!gymId,
  });
}

export function usePlanTemplate(id: string | null) {
  const gymId = useGymId();
  return useQuery<PlanTemplate>({
    queryKey: [KEY, gymId, id],
    queryFn: async () => (await apiClient.get(`/training/plan-templates/${id}`)).data?.data,
    enabled: !!gymId && !!id,
  });
}

/** Creates a gym template (id null) or saves an existing one. */
export function useSavePlanTemplate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, content }: { id: string | null; content: unknown }): Promise<PlanTemplate> => {
      const res = id
        ? await apiClient.put(`/training/plan-templates/${id}`, content)
        : await apiClient.post('/training/plan-templates', { asGymTemplate: true, content });
      return res.data?.data;
    },
    onSuccess: () => void qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useArchivePlanTemplate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/training/plan-templates/${id}`);
    },
    onSuccess: () => void qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

export function useDuplicatePlanTemplate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, name }: { id: string; name: string }): Promise<PlanTemplate> =>
      (await apiClient.post(`/training/plan-templates/${id}/duplicate`, { asGymTemplate: true, name })).data?.data,
    onSuccess: () => void qc.invalidateQueries({ queryKey: [KEY] }),
  });
}

/** Library search (English or Arabic) for the editor; nothing below two characters. */
export function useExerciseSearch(query: string) {
  const gymId = useGymId();
  const q = query.trim();
  return useQuery<LibraryExercise[]>({
    queryKey: ['plan-exercises', gymId, q],
    queryFn: async () =>
      (await apiClient.get('/training/plan-exercises', { params: { search: q, pageSize: 10 } })).data?.data?.items ?? [],
    enabled: !!gymId && q.length >= 2,
    staleTime: 60_000,
  });
}
