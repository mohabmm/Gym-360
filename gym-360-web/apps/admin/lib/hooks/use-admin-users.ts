import { useApiQuery, useApiMutation, useInvalidateQueries, IDENTITY } from '@gymapp/ui';
import type { AdminUser } from '@gymapp/ui';

export function useAdminUsers() {
  return useApiQuery<AdminUser[]>(
    ['admin-users'],
    IDENTITY.ADMIN_USERS,
  );
}

export function useAdminUser(id: string) {
  return useApiQuery<AdminUser>(
    ['admin-users', id],
    IDENTITY.ADMIN_USER(id),
    undefined,
    { enabled: !!id },
  );
}

export function useCreateAdminUser() {
  const { invalidate } = useInvalidateQueries();
  return useApiMutation<AdminUser, Partial<AdminUser>>(
    IDENTITY.ADMIN_USERS,
    'post',
    {
      onSuccess: () => {
        invalidate(['admin-users']);
      },
    }
  );
}

export function useUpdateAdminUser(id: string) {
  const { invalidate } = useInvalidateQueries();
  return useApiMutation<AdminUser, Partial<AdminUser>>(
    IDENTITY.ADMIN_USER(id),
    'patch',
    {
      onSuccess: () => {
        invalidate(['admin-users']);
      },
    }
  );
}

export function useDeleteAdminUser(id: string) {
  const { invalidate } = useInvalidateQueries();
  return useApiMutation<void, void>(
    IDENTITY.ADMIN_USER(id),
    'delete',
    {
      onSuccess: () => {
        invalidate(['admin-users']);
      },
    }
  );
}
