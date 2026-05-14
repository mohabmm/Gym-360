import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useSession } from 'next-auth/react';
import apiClient from '@/lib/api-client';
import type { StaffMember, StaffSubRole } from '@gymapp/ui';

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function normaliseStaff(s: any): StaffMember {
  const roleMap: Record<string, StaffSubRole> = {
    owner: 'owner',
    manager: 'manager',
    receptionist: 'receptionist',
  };
  return {
    id: s.id ?? s.userId ?? '',
    email: s.email ?? '',
    phone: s.phoneNumber ?? s.phone ?? '',
    firstName: s.firstName ?? '',
    lastName: s.lastName ?? '',
    firstNameAr: s.firstNameAr ?? '',
    lastNameAr: s.lastNameAr ?? '',
    role: 'staff',
    status: s.status ?? 'active',
    avatarUrl: s.profilePhotoUrl ?? undefined,
    createdAt: s.createdAt ?? '',
    staffSubRole: roleMap[(s.staffSubRole ?? s.role ?? '').toLowerCase()] ?? 'receptionist',
    gymId: s.gymId ?? '',
    hireDate: s.hireDate ?? '',
    permissions: s.permissions ?? [],
  };
}

export function useStaff() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  return useQuery<StaffMember[]>({
    queryKey: ['staff', gymId],
    enabled: !!gymId,
    queryFn: async () => {
      const res = await apiClient.get(`/gyms/${gymId}/staff`, { params: { page: 1, pageSize: 100 } });
      const raw: unknown[] = res.data.data?.items ?? res.data.data ?? [];
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      return (raw as any[]).map(normaliseStaff);
    },
  });
}

export function useCreateStaff() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (data: {
      firstName: string; lastName: string; email: string; phone: string; password: string;
      staffSubRole: string; permissions?: string[];
    }) => {
      if (!gymId) throw new Error('No gym selected. Pick a gym before adding staff.');
      // /gyms/{id}/staff/create provisions the user + assigns the StaffRole atomically.
      // The plain /gyms/{id}/staff endpoint requires an existing UserId, which the
      // staff web "Add Staff" flow does not have.
      const res = await apiClient.post(`/gyms/${gymId}/staff/create`, {
        email: data.email,
        password: data.password,
        firstName: data.firstName,
        lastName: data.lastName,
        phone: data.phone,
        staffSubRole: data.staffSubRole,
      });
      return res.data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['staff', gymId] }),
  });
}
