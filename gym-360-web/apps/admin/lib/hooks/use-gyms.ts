import { useMutation, useQuery } from '@tanstack/react-query';
import {
  useInvalidateQueries,
  GYMS,
  apiGet,
  apiPost,
  apiDelete,
} from '@gym360/ui';
import type {
  GymListItem,
  PendingGym,
  Gym,
  MemberListItem,
  TrainerListItem,
  ApiResponse,
} from '@gym360/ui';

// The API returns a paginated envelope: { data: { items: [...], page, pageSize, ... } }
// The admin UI wants a flat array, so unwrap items here and normalise
// casing/missing-field differences between the DTO and what the UI expects.
type PagedItems = { items: Array<Record<string, unknown>> };

// Accepts either a bare array or a paginated `{ items }` envelope.
function unwrapItems(raw: unknown): unknown[] {
  if (Array.isArray(raw)) return raw;
  const items = (raw as PagedItems | undefined)?.items;
  return Array.isArray(items) ? items : [];
}

function lowerIfString(value: unknown): unknown {
  return typeof value === 'string' ? value.toLowerCase() : value;
}

// Fields shared by the list and detail DTO normalisers.
function normalizeGymCommon(raw: Record<string, any>) {
  return {
    ...raw,
    status: lowerIfString(raw.status),
    paymentStatus: lowerIfString(raw.paymentStatus),
    ownerName: raw.ownerName ?? '',
    planName: raw.planName ?? '',
    activeMemberCount: raw.activeMemberCount ?? raw.memberCount ?? 0,
    activeTrainerCount: raw.activeTrainerCount ?? raw.trainerCount ?? 0,
  };
}

function normalizeGym(raw: Record<string, any>): GymListItem {
  return {
    ...normalizeGymCommon(raw),
    city: raw.city ?? raw.address?.city ?? '',
    monthlyRevenue: raw.monthlyRevenue ?? 0,
  } as GymListItem;
}

export function useGyms(params?: { search?: string; status?: string; city?: string; page?: number; pageSize?: number }) {
  return useQuery<ApiResponse<GymListItem[]>, Error>({
    queryKey: ['gyms', JSON.stringify(params ?? {})],
    queryFn: async () => {
      const res = await apiGet<PagedItems | GymListItem[]>(
        GYMS.LIST,
        params as Record<string, unknown>,
      );
      const items = unwrapItems(res.data);
      return { ...res, data: items.map((g) => normalizeGym(g as Record<string, any>)) };
    },
  });
}

function normalizeGymDetail(raw: Record<string, any>): Gym {
  return {
    ...normalizeGymCommon(raw),
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

function toPendingGym(g: Record<string, any>): PendingGym {
  return {
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
  };
}

export function usePendingGyms() {
  // Backend now exposes GET /gyms/pending. Map the response (GymResponse) to the simpler
  // PendingGym shape the onboarding UI expects.
  return useQuery<ApiResponse<PendingGym[]>, Error>({
    queryKey: ['gyms', 'pending'],
    queryFn: async () => {
      const res = await apiGet<Array<Record<string, any>>>(GYMS.PENDING);
      const items = Array.isArray(res.data) ? res.data : [];
      return { ...res, data: items.map(toPendingGym) } as ApiResponse<PendingGym[]>;
    },
  });
}

// Shared shape for gym lifecycle mutations: call the endpoint, then refresh
// the given query keys.
function useGymMutation<TVars>(
  mutationFn: (vars: TVars) => Promise<ApiResponse<void>>,
  invalidateKeys: string[][],
) {
  const { invalidate } = useInvalidateQueries();
  return useMutation<ApiResponse<void>, Error, TVars>({
    mutationFn,
    onSuccess: () => {
      invalidateKeys.forEach((key) => invalidate(key));
    },
  });
}

const GYMS_KEYS = [['gyms']];
const PENDING_AND_GYMS_KEYS = [['gyms', 'pending'], ['gyms']];

export function useSuspendGym() {
  return useGymMutation<{ gymId: string; reason: string }>(
    ({ gymId, reason }) => apiPost(GYMS.SUSPEND(gymId), { reason }),
    GYMS_KEYS,
  );
}

export function useDeleteGym() {
  return useGymMutation<{ gymId: string }>(({ gymId }) => apiDelete(GYMS.DELETE(gymId)), GYMS_KEYS);
}

export function useReactivateGym() {
  return useGymMutation<{ gymId: string }>(({ gymId }) => apiPost(GYMS.REACTIVATE(gymId)), GYMS_KEYS);
}

export function useApproveGym() {
  return useGymMutation<{ gymId: string }>(
    ({ gymId }) => apiPost(GYMS.APPROVE(gymId)),
    PENDING_AND_GYMS_KEYS,
  );
}

export function useRejectGym() {
  return useGymMutation<{ gymId: string; reason: string }>(
    ({ gymId, reason }) => apiPost(GYMS.REJECT(gymId), { reason }),
    PENDING_AND_GYMS_KEYS,
  );
}

// First page (50) of a gym-scoped roster endpoint, flattened to an array.
function useGymRoster<T>(gymId: string, resource: 'members' | 'trainers', url: string) {
  return useQuery<ApiResponse<T[]>, Error>({
    queryKey: ['gyms', gymId, resource],
    enabled: !!gymId,
    queryFn: async () => {
      const res = await apiGet<PagedItems | T[]>(url, { page: 1, pageSize: 50 });
      return { ...res, data: unwrapItems(res.data) as T[] };
    },
  });
}

export function useGymMembers(gymId: string) {
  return useGymRoster<MemberListItem>(gymId, 'members', GYMS.MEMBERS(gymId));
}

export function useGymTrainers(gymId: string) {
  return useGymRoster<TrainerListItem>(gymId, 'trainers', GYMS.TRAINERS(gymId));
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
      const items = unwrapItems(res.data) as Array<Record<string, any>>;
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
