import { useMutation, useQuery } from '@tanstack/react-query';
import {
  useInvalidateQueries,
  GYMS,
  apiGet,
  apiPost,
  apiDelete,
} from '@gymapp/ui';
import type {
  GymListItem,
  PendingGym,
  Gym,
  MemberListItem,
  TrainerListItem,
  ApiResponse,
} from '@gymapp/ui';

// The API returns a paginated envelope: { data: { items: [...], page, pageSize, ... } }
// The admin UI wants a flat array, so unwrap items here and normalise
// casing/missing-field differences between the DTO and what the UI expects.
type PagedGyms = { items: Array<Record<string, unknown>> };

function normalizeGym(raw: Record<string, any>): GymListItem {
  return {
    ...raw,
    status: typeof raw.status === 'string' ? raw.status.toLowerCase() : raw.status,
    paymentStatus:
      typeof raw.paymentStatus === 'string' ? raw.paymentStatus.toLowerCase() : raw.paymentStatus,
    city: raw.city ?? raw.address?.city ?? '',
    ownerName: raw.ownerName ?? '',
    planName: raw.planName ?? '',
    activeMemberCount: raw.activeMemberCount ?? raw.memberCount ?? 0,
    activeTrainerCount: raw.activeTrainerCount ?? raw.trainerCount ?? 0,
    monthlyRevenue: raw.monthlyRevenue ?? 0,
  } as GymListItem;
}

export function useGyms(params?: { search?: string; status?: string; city?: string; page?: number; pageSize?: number }) {
  return useQuery<ApiResponse<GymListItem[]>, Error>({
    queryKey: ['gyms', JSON.stringify(params ?? {})],
    queryFn: async () => {
      const res = await apiGet<PagedGyms | GymListItem[]>(
        GYMS.LIST,
        params as Record<string, unknown>,
      );
      const raw: unknown = res.data;
      const items = Array.isArray(raw)
        ? raw
        : Array.isArray((raw as PagedGyms)?.items)
          ? (raw as PagedGyms).items
          : [];
      return { ...res, data: items.map((g) => normalizeGym(g as Record<string, any>)) };
    },
  });
}

function normalizeGymDetail(raw: Record<string, any>): Gym {
  return {
    ...raw,
    status: typeof raw.status === 'string' ? raw.status.toLowerCase() : raw.status,
    paymentStatus:
      typeof raw.paymentStatus === 'string' ? raw.paymentStatus.toLowerCase() : raw.paymentStatus,
    planName: raw.planName ?? '',
    ownerName: raw.ownerName ?? '',
    activeMemberCount: raw.activeMemberCount ?? raw.memberCount ?? 0,
    activeTrainerCount: raw.activeTrainerCount ?? raw.trainerCount ?? 0,
    amenities: raw.amenities ?? [],
    sections: raw.sections ?? [],
    operatingHours: raw.operatingHours ?? [],
    seasonalHours: raw.seasonalHours ?? [],
    socialMedia: raw.socialMedia ?? {},
  } as Gym;
}

export function useGym(id: string) {
  return useQuery<ApiResponse<Gym>, Error>({
    queryKey: ['gyms', id],
    enabled: !!id,
    queryFn: async () => {
      const res = await apiGet<Record<string, any>>(GYMS.DETAIL(id));
      return { ...res, data: normalizeGymDetail(res.data ?? {}) } as ApiResponse<Gym>;
    },
  });
}

export function usePendingGyms() {
  // Backend now exposes GET /gyms/pending. Map the response (GymResponse) to the simpler
  // PendingGym shape the onboarding UI expects.
  return useQuery<ApiResponse<PendingGym[]>, Error>({
    queryKey: ['gyms', 'pending'],
    queryFn: async () => {
      const res = await apiGet<Array<Record<string, any>>>(GYMS.PENDING);
      const items = Array.isArray(res.data) ? res.data : [];
      const pending: PendingGym[] = items.map((g) => ({
        id: String(g.id),
        name: String(g.name),
        ownerName: String(g.ownerName ?? ''),
        ownerEmail: String(g.contact?.email ?? g.ownerEmail ?? ''),
        ownerPhone: String(g.contact?.phone ?? g.ownerPhone ?? ''),
        city: String(g.address?.city ?? g.city ?? ''),
        district: String(g.address?.state ?? g.district ?? ''),
        requestedPlan: String(g.requestedPlan ?? ''),
        submittedAt: String(g.createdAt ?? ''),
        notes: g.notes ?? undefined,
      }));
      return { ...res, data: pending } as ApiResponse<PendingGym[]>;
    },
  });
}

export function useSuspendGym() {
  const { invalidate } = useInvalidateQueries();
  return useMutation<ApiResponse<void>, Error, { gymId: string; reason: string }>({
    mutationFn: ({ gymId, reason }) => apiPost(GYMS.SUSPEND(gymId), { reason }),
    onSuccess: () => {
      invalidate(['gyms']);
    },
  });
}

export function useDeleteGym() {
  const { invalidate } = useInvalidateQueries();
  return useMutation<ApiResponse<void>, Error, { gymId: string }>({
    mutationFn: ({ gymId }) => apiDelete(GYMS.DELETE(gymId)),
    onSuccess: () => {
      invalidate(['gyms']);
    },
  });
}

export function useReactivateGym() {
  const { invalidate } = useInvalidateQueries();
  return useMutation<ApiResponse<void>, Error, { gymId: string }>({
    mutationFn: ({ gymId }) => apiPost(GYMS.REACTIVATE(gymId)),
    onSuccess: () => {
      invalidate(['gyms']);
    },
  });
}

export function useApproveGym() {
  const { invalidate } = useInvalidateQueries();
  return useMutation<ApiResponse<void>, Error, { gymId: string }>({
    mutationFn: ({ gymId }) => apiPost(GYMS.APPROVE(gymId)),
    onSuccess: () => {
      invalidate(['gyms', 'pending']);
      invalidate(['gyms']);
    },
  });
}

export function useRejectGym() {
  const { invalidate } = useInvalidateQueries();
  return useMutation<ApiResponse<void>, Error, { gymId: string; reason: string }>({
    mutationFn: ({ gymId, reason }) => apiPost(GYMS.REJECT(gymId), { reason }),
    onSuccess: () => {
      invalidate(['gyms', 'pending']);
      invalidate(['gyms']);
    },
  });
}

type PagedMembers = { items: Array<Record<string, unknown>> };

export function useGymMembers(gymId: string) {
  return useQuery<ApiResponse<MemberListItem[]>, Error>({
    queryKey: ['gyms', gymId, 'members'],
    enabled: !!gymId,
    queryFn: async () => {
      const res = await apiGet<PagedMembers | MemberListItem[]>(GYMS.MEMBERS(gymId), { page: 1, pageSize: 50 });
      const raw: unknown = res.data;
      const items = Array.isArray(raw)
        ? raw
        : Array.isArray((raw as PagedMembers)?.items)
          ? (raw as PagedMembers).items
          : [];
      return { ...res, data: items as MemberListItem[] };
    },
  });
}

type PagedTrainers = { items: Array<Record<string, unknown>> };

export function useGymTrainers(gymId: string) {
  return useQuery<ApiResponse<TrainerListItem[]>, Error>({
    queryKey: ['gyms', gymId, 'trainers'],
    enabled: !!gymId,
    queryFn: async () => {
      const res = await apiGet<PagedTrainers | TrainerListItem[]>(GYMS.TRAINERS(gymId), { page: 1, pageSize: 50 });
      const raw: unknown = res.data;
      const items = Array.isArray(raw)
        ? raw
        : Array.isArray((raw as PagedTrainers)?.items)
          ? (raw as PagedTrainers).items
          : [];
      return { ...res, data: items as TrainerListItem[] };
    },
  });
}

export interface CreateGymStaffPayload {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phone?: string;
  staffSubRole: 'Owner' | 'Manager' | 'Receptionist';
}

export interface CreateGymStaffResponse {
  userId: string;
}

export function useCreateGymStaff(gymId: string) {
  const { invalidate } = useInvalidateQueries();
  return useMutation<ApiResponse<CreateGymStaffResponse>, Error, CreateGymStaffPayload>({
    mutationFn: async (payload) => {
      const res = await apiPost<CreateGymStaffResponse>(GYMS.ADMIN_CREATE_STAFF(gymId), {
        ...payload,
        phone: payload.phone?.trim() ? payload.phone.trim() : undefined,
      });
      return res;
    },
    onSuccess: () => {
      invalidate(['gyms']);
      invalidate(['gyms', gymId, 'staff']);
      invalidate(['admin-users']);
    },
  });
}

export function useGymStaff(gymId: string) {
  return useQuery<ApiResponse<Array<Record<string, any>>>, Error>({
    queryKey: ['gyms', gymId, 'staff'],
    enabled: !!gymId,
    queryFn: async () => {
      const res = await apiGet<{ items?: Array<Record<string, any>> } | Array<Record<string, any>>>(
        GYMS.STAFF(gymId),
        { page: 1, pageSize: 100 },
      );
      const raw = res.data as any;
      const items: Array<Record<string, any>> = Array.isArray(raw) ? raw : Array.isArray(raw?.items) ? raw.items : [];
      return { ...res, data: items };
    },
  });
}

export function useBulkSuspendGyms() {
  const { invalidate } = useInvalidateQueries();
  return useMutation<{ successCount: number; failCount: number }, Error, { gymIds: string[]; reason: string }>({
    mutationFn: async ({ gymIds, reason }) => {
      let successCount = 0;
      let failCount = 0;
      for (const gymId of gymIds) {
        try {
          await apiPost(GYMS.SUSPEND(gymId), { reason });
          successCount++;
        } catch {
          failCount++;
        }
      }
      return { successCount, failCount };
    },
    onSuccess: () => {
      invalidate(['gyms']);
    },
  });
}
