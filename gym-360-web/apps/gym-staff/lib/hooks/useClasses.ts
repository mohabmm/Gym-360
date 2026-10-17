import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useSession } from 'next-auth/react';
import apiClient from '@/lib/api-client';
import type { GymClass } from '@gym360/ui';
import { useGymMutation, postData, putData, type GymScope } from './query-helpers';

const DAY_NAME_TO_INDEX: Record<string, number> = {
  sunday: 0, monday: 1, tuesday: 2, wednesday: 3, thursday: 4, friday: 5, saturday: 6,
};

function toDayIndex(val: unknown): number {
  if (typeof val === 'number') return val;
  if (typeof val === 'string') return DAY_NAME_TO_INDEX[val.toLowerCase()] ?? 0;
  return 0;
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function normaliseClass(c: any): GymClass {
  const startTime: string = c.startTime ?? c.schedule?.startTime ?? '00:00';
  const durationMinutes: number = c.durationMinutes ?? c.schedule?.durationMinutes ?? 60;
  const [sh, sm] = startTime.split(':').map(Number);
  const endMinutes = (sh * 60 + (sm ?? 0) + durationMinutes) % (24 * 60);
  const endTime = `${String(Math.floor(endMinutes / 60)).padStart(2, '0')}:${String(endMinutes % 60).padStart(2, '0')}`;
  const rawStatus = (c.status ?? 'active').toLowerCase();
  return {
    id: c.id ?? '',
    name: c.name ?? '',
    nameAr: c.nameAr ?? '',
    description: c.description ?? undefined,
    trainerId: c.trainerId ?? '',
    trainerName: c.trainerName ?? '',
    gymId: c.gymId ?? '',
    section: c.section ?? '',
    capacity: c.capacity ?? 0,
    enrolledCount: c.enrolledCount ?? c.bookedCount ?? 0,
    dayOfWeek: toDayIndex(c.dayOfWeek ?? c.schedule?.dayOfWeek),
    startTime,
    endTime: c.endTime ?? endTime,
    isRecurring: c.isRecurring ?? c.schedule?.isRecurring ?? false,
    status: (rawStatus === 'active' ? 'active' : rawStatus === 'full' ? 'full' : 'cancelled') as GymClass['status'],
    color: c.color ?? undefined,
  };
}

export function useClasses() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  return useQuery<GymClass[]>({
    queryKey: ['classes', gymId],
    enabled: !!gymId,
    queryFn: async () => {
      const res = await apiClient.get(`/gyms/${gymId}/classes`, { params: { page: 1, pageSize: 100 } });
      const raw: unknown[] = res.data.data?.items ?? res.data.data ?? [];
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      return (raw as any[]).map(normaliseClass);
    },
  });
}

function diffMinutes(start: string, end: string): number {
  const [sh, sm] = start.split(':').map(Number);
  const [eh, em] = end.split(':').map(Number);
  const total = (eh * 60 + em) - (sh * 60 + sm);
  return total > 0 ? total : 60;
}

// The API binds startTime to a .NET TimeOnly, whose System.Text.Json converter
// requires "HH:mm:ss". <input type="time"> yields "HH:mm" (no seconds), which
// fails body deserialization with "Failed to read parameter Request from body
// as JSON". Pad to seconds so the payload is a valid TimeOnly string.
function toApiTime(t: string): string {
  if (!t) return '00:00:00';
  const parts = t.split(':');
  while (parts.length < 3) parts.push('00');
  return parts.map((p) => p.padStart(2, '0')).join(':');
}

export interface ClassMutationInput {
  name: string;
  nameAr?: string;
  description?: string;
  trainerId: string;
  section?: string;
  capacity: number;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  isRecurring: boolean;
  color?: string;
}

// Shared body for create/update: the API takes a nested schedule with a duration rather than an end time.
function toClassPayload(data: ClassMutationInput) {
  return {
    name: data.name,
    description: data.description,
    trainerId: data.trainerId,
    schedule: {
      dayOfWeek: data.dayOfWeek,
      startTime: toApiTime(data.startTime),
      durationMinutes: diffMinutes(data.startTime, data.endTime),
      isRecurring: data.isRecurring,
    },
    capacity: data.capacity,
  };
}

const classesKey = ({ gymId }: GymScope) => ['classes', gymId];

export function useCreateClass() {
  return useGymMutation(
    (data: ClassMutationInput, { gymId }) =>
      postData(`/gyms/${gymId}/classes`, { ...toClassPayload(data), waitlistEnabled: true }),
    classesKey,
  );
}

export function useUpdateClass() {
  return useGymMutation(
    ({ id, ...data }: ClassMutationInput & { id: string }, { gymId }) =>
      putData(`/gyms/${gymId}/classes/${id}`, toClassPayload(data)),
    classesKey,
  );
}

export function useDeleteClass() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (classId: string) => {
      await apiClient.delete(`/gyms/${gymId}/classes/${classId}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['classes', gymId] }),
  });
}
