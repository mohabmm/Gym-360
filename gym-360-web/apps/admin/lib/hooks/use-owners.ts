import { useApiMutation, useInvalidateQueries, IDENTITY } from '@gym360/ui';

export interface CreateGymOwnerPayload {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phone?: string;
  companyName: string;
  billingEmail: string;
  planTier?: 'Free' | 'Starter' | 'Pro' | 'Business';
  dashboardMode?: 'Unified' | 'PerGym' | 'Both';
}

export interface CreateGymOwnerResponse {
  userId: string;
  ownerId: string;
}

export function useCreateGymOwner() {
  const { invalidate } = useInvalidateQueries();
  return useApiMutation<CreateGymOwnerResponse, CreateGymOwnerPayload>(
    IDENTITY.ADMIN_OWNERS,
    'post',
    {
      onSuccess: () => {
        invalidate(['admin-users']);
      },
    },
  );
}
