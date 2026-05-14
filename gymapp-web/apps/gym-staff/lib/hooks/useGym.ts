import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useSession } from 'next-auth/react';
import apiClient from '@/lib/api-client';

export interface GymProfile {
  id: string;
  name: string;
  description?: string;
  phone?: string;
  email?: string;
  website?: string;
  street?: string;
  city?: string;
  area?: string;
  amenities: string[];
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function normaliseGym(g: any): GymProfile {
  return {
    id: g.id ?? g.gymId ?? '',
    name: g.name ?? '',
    description: g.description ?? '',
    phone: g.contact?.phone ?? g.phone ?? '',
    email: g.contact?.email ?? g.email ?? '',
    website: g.contact?.website ?? g.website ?? '',
    street: g.address?.street ?? '',
    city: g.address?.city ?? '',
    area: g.address?.area ?? '',
    amenities: g.amenities ?? [],
  };
}

export function useGym() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  return useQuery<GymProfile | undefined>({
    queryKey: ['gym', gymId],
    enabled: !!gymId,
    queryFn: async () => {
      const res = await apiClient.get(`/gyms/${gymId}`);
      const g = res.data?.data;
      return g ? normaliseGym(g) : undefined;
    },
  });
}

export function useUpdateGym() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (data: Partial<GymProfile>) => {
      const res = await apiClient.put(`/gyms/${gymId}`, {
        name: data.name,
        description: data.description,
        address: (data.street || data.city || data.area)
          ? { street: data.street, city: data.city, area: data.area }
          : undefined,
        contact: (data.phone || data.email || data.website)
          ? { phone: data.phone, email: data.email, website: data.website }
          : undefined,
        amenities: data.amenities,
      });
      return res.data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['gym', gymId] }),
  });
}
