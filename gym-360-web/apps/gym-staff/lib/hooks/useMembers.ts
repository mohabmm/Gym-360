import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useSession } from 'next-auth/react';
import apiClient from '@/lib/api-client';
import type { MemberListItem } from '@gym360/ui';

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function normaliseMember(m: any): MemberListItem {
  const statusMap: Record<string, MemberListItem['status']> = {
    active: 'active',
    inactive: 'inactive',
    frozen: 'frozen',
    expired: 'expired',
  };
  const rawStatus = m.isFrozen ? 'frozen' : (m.status ?? '').toLowerCase();
  return {
    id: m.memberId ?? m.id ?? '',
    name: m.name ?? ([m.firstName, m.lastName].filter(Boolean).join(' ') || ''),
    nameAr: m.nameAr ?? '',
    email: m.email ?? '',
    phone: m.phoneNumber ?? m.phone ?? '',
    status: statusMap[rawStatus] ?? 'inactive',
    membershipType: m.membershipType ?? 'monthly',
    joinDate: m.joinedAt ?? m.joinDate ?? '',
    expiryDate: m.expiryDate ?? '',
    section: m.section ?? '',
    lastCheckIn: m.lastCheckIn ?? undefined,
    avatarUrl: m.profilePhotoUrl ?? undefined,
  };
}

export function useMembers() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  return useQuery<MemberListItem[]>({
    queryKey: ['members', gymId],
    enabled: !!gymId,
    queryFn: async () => {
      const res = await apiClient.get(`/gyms/${gymId}/members`, { params: { page: 1, pageSize: 200 } });
      const raw: unknown[] = res.data.data?.items ?? res.data.data ?? [];
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      return (raw as any[]).map(normaliseMember);
    },
  });
}

export interface MembersPage {
  items: MemberListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface MembersQuery {
  page: number;
  pageSize: number;
  search?: string;
  status?: string;
  section?: string;
}

/**
 * Server-side paged + searched member list for the Members table. The BE filters
 * by name/email/phone/memberNumber (search) and status/section across the WHOLE
 * roster, so members past the first page are findable — the old `useMembers()`
 * fetched a 200-row client-side slice and hid everyone beyond it.
 */
export function useMembersPaged(query: MembersQuery) {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const { page, pageSize, search, status, section } = query;
  return useQuery<MembersPage>({
    queryKey: ['members', gymId, 'paged', { page, pageSize, search, status, section }],
    enabled: !!gymId,
    placeholderData: (prev) => prev, // keep the table populated while paging/searching
    queryFn: async () => {
      const params: Record<string, string | number> = { page, pageSize };
      if (search?.trim()) params.search = search.trim();
      if (status && status !== 'all') params.status = status;
      if (section && section !== 'all') params.section = section;
      const res = await apiClient.get(`/gyms/${gymId}/members`, { params });
      const data = res.data.data ?? {};
      const raw: unknown[] = data.items ?? (Array.isArray(data) ? data : []);
      return {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        items: (raw as any[]).map(normaliseMember),
        page: data.page ?? page,
        pageSize: data.pageSize ?? pageSize,
        totalCount: data.totalCount ?? raw.length,
        totalPages: data.totalPages ?? 1,
      };
    },
  });
}

export function useMember(id: string) {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  return useQuery<MemberListItem | undefined>({
    queryKey: ['members', gymId, id],
    enabled: !!gymId && !!id,
    queryFn: async () => {
      const res = await apiClient.get(`/gyms/${gymId}/members/${id}`);
      const m = res.data.data;
      return m ? normaliseMember(m) : undefined;
    },
  });
}

export function useCreateMember() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (data: {
      name: string; email?: string; phone: string; section?: string;
    }) => {
      const [firstName, ...rest] = data.name.trim().split(/\s+/);
      const lastName = rest.join(' ') || firstName;
      // staff-create is the authoritative entry point for NEW members: it creates
      // the Trainee user, calls AddMemberCommand (gym.MemberIds + gym_members
      // directory row), and links the user to the gym. It now REJECTS a phone/email
      // that already belongs to an existing member (409) instead of silently
      // returning that other person's account — the caller handles 409 by offering
      // the "Add existing member" re-link flow (useLinkExistingMember).
      // Email is sent only when provided; the backend synthesizes a stable
      // placeholder from the phone otherwise (no Date.now() churn that masked dups).
      const userRes = await apiClient.post('/identity/users/staff-create', {
        firstName,
        lastName,
        phone: data.phone,
        ...(data.email ? { email: data.email } : {}),
        section: data.section,
      });
      const userId =
        userRes.data?.data?.userId ?? userRes.data?.data?.id ?? userRes.data?.data ?? null;
      if (!userId) {
        throw new Error('Identity service did not return a user id.');
      }
      return userRes.data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['members', gymId] }),
  });
}

export interface ExistingMemberLookup {
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  isAlreadyMemberOfThisGym: boolean;
}

/**
 * Resolves an existing trainee by phone so the Add Member dialog can confirm a
 * returning member before re-linking. Called when staff-create returns 409
 * (phone/email already registered). Backed by GET /identity/users/staff-lookup.
 */
export function useLookupMemberByPhone() {
  return useMutation<ExistingMemberLookup, unknown, string>({
    mutationFn: async (phone: string) => {
      const res = await apiClient.get('/identity/users/staff-lookup', { params: { phone } });
      return res.data.data as ExistingMemberLookup;
    },
  });
}

/**
 * Re-links an existing trainee (resolved by phone) to the staff's gym without
 * creating a duplicate. Backed by POST /identity/users/staff-link-existing.
 */
export function useLinkExistingMember() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (data: { phone: string; section?: string }) => {
      const res = await apiClient.post('/identity/users/staff-link-existing', {
        phone: data.phone,
        section: data.section,
      });
      return res.data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['members', gymId] }),
  });
}

export function useUpdateMember() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, ...data }: {
      id: string; name?: string; nameAr?: string; email?: string; phone?: string;
    }) => {
      const res = await apiClient.patch(`/gyms/${gymId}/members/${id}`, data);
      return res.data;
    },
    onSuccess: (_, vars) => {
      qc.invalidateQueries({ queryKey: ['members', gymId] });
      qc.invalidateQueries({ queryKey: ['members', gymId, vars.id] });
    },
  });
}

export interface MemberPackage {
  id: string;
  trainerId: string;
  name: string;
  totalSessions: number;
  remainingSessions: number;
  pricePerSession: number;
  totalPrice: number;
  currency: string;
  status: string;
  expiresAt: string | null;
  createdAt: string;
}

/**
 * Lists this member's session packages. Backed by GET /billing/packages?memberId=...
 * — used by the Sessions tab on the member detail screen so the assigned trainer,
 * session counts, and remaining sessions are visible after a quick-assign.
 */
export function useMemberPackages(memberId: string) {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  return useQuery<MemberPackage[]>({
    queryKey: ['packages', gymId, 'member', memberId],
    enabled: !!gymId && !!memberId,
    queryFn: async () => {
      const res = await apiClient.get('/billing/packages', { params: { memberId } });
      const raw: unknown[] = res.data.data?.items ?? res.data.data ?? [];
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      return (raw as any[]).map((p) => ({
        id: p.id,
        trainerId: p.trainerId,
        name: p.name,
        totalSessions: p.totalSessions ?? 0,
        remainingSessions: p.remainingSessions ?? 0,
        pricePerSession: p.pricePerSession ?? 0,
        totalPrice: p.totalPrice ?? 0,
        currency: p.currency ?? 'EGP',
        status: p.status ?? 'Active',
        expiresAt: p.expiresAt ?? null,
        createdAt: p.createdAt ?? '',
      }));
    },
  });
}

/**
 * Quick-assign a trainer + session count (multiples of 4) to this member.
 * Backed by POST /billing/packages/quick-assign — creates an Active
 * SessionPackage that downstream apps (trainer roster, scheduling, billing)
 * read to surface the trainer-member relationship.
 */
export function useQuickAssignTrainer() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const userId = session?.user?.id;
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (data: { memberId: string; trainerId: string; sessionCount: number; totalPrice?: number; paymentMethod?: string }) => {
      if (!gymId) throw new Error('No gym selected.');
      if (!userId) throw new Error('No active session.');
      const res = await apiClient.post('/billing/packages/quick-assign', {
        gymId,
        trainerId: data.trainerId,
        memberId: data.memberId,
        sessionCount: data.sessionCount,
        totalPrice: data.totalPrice,
        paymentMethod: data.paymentMethod,
        assignedBy: userId,
      });
      return res.data;
    },
    onSuccess: (_, vars) => {
      qc.invalidateQueries({ queryKey: ['members', gymId] });
      qc.invalidateQueries({ queryKey: ['members', gymId, vars.memberId] });
      qc.invalidateQueries({ queryKey: ['packages'] });
      qc.invalidateQueries({ queryKey: ['packages', gymId, 'member', vars.memberId] });
      qc.invalidateQueries({ queryKey: ['payments'] });
      qc.invalidateQueries({ queryKey: ['revenue'] });
    },
  });
}

/**
 * Owner-only on the BE (RemoveMemberEndpoint requires gym_owner_or_admin).
 * The UI gates this behind the owner principal check at the call site so
 * non-owners never see the affordance.
 */
export function useDeleteMember() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      if (!gymId) throw new Error('No gym selected.');
      await apiClient.delete(`/gyms/${gymId}/members/${id}`);
    },
    onSuccess: (_, id) => {
      qc.invalidateQueries({ queryKey: ['members', gymId] });
      qc.invalidateQueries({ queryKey: ['members', gymId, id] });
    },
  });
}
