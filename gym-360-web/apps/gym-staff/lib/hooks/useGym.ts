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
  // Branding — carried through so settings can hydrate the color pickers and
  // preserve logo/cover on a branding save (the BE replaces the whole object).
  logoUrl?: string;
  coverPhotoUrl?: string;
  primaryColor?: string;
  secondaryColor?: string;
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
    area: g.address?.area ?? g.address?.state ?? '',
    amenities: g.amenities ?? [],
    logoUrl: g.branding?.logoUrl ?? undefined,
    coverPhotoUrl: g.branding?.coverPhotoUrl ?? undefined,
    primaryColor: g.branding?.primaryColor ?? undefined,
    secondaryColor: g.branding?.secondaryColor ?? undefined,
  };
}

export interface BrandingUpdate {
  primaryColor?: string;
  secondaryColor?: string;
  logoUrl?: string;
  coverPhotoUrl?: string;
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
        // BE AddressDto requires street/city/country (validator NotEmpty). The UI
        // only collects street/city/area, so map area→state, default country to
        // Egypt, and send zipCode. Without `country` the save 400s.
        address: (data.street || data.city || data.area)
          ? { street: data.street ?? '', city: data.city ?? '', state: data.area ?? '', country: 'Egypt', zipCode: '' }
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

// Branding (colors/logo/cover) has its own endpoint — the general UpdateGym
// endpoint ignores branding fields. The BE replaces the whole Branding object,
// so callers must pass through existing logo/cover to avoid wiping them.
export function useUpdateBranding() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (data: BrandingUpdate) => {
      const res = await apiClient.put(`/gyms/${gymId}/branding`, {
        logoUrl: data.logoUrl ?? null,
        primaryColor: data.primaryColor ?? null,
        secondaryColor: data.secondaryColor ?? null,
        coverPhotoUrl: data.coverPhotoUrl ?? null,
      });
      return res.data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['gym', gymId] }),
  });
}
