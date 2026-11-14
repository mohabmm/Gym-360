'use client';

import { useState } from 'react';
import { apiDelete, apiPatch, extractApiErrors, IDENTITY, useInvalidateQueries } from '@gym360/ui';
import type { AdminUser } from '@gym360/ui';

export type PendingUserAction = { kind: 'suspend' | 'delete'; user: AdminUser } | null;

export type UserActions = ReturnType<typeof useUserActions>;

const fullName = (user: AdminUser) => `${user.firstName} ${user.lastName}`;

function errorMessage(err: unknown, fallback: string): string {
  return extractApiErrors(err)[0]?.message ?? fallback;
}

/**
 * Suspend / reactivate / permanently delete users from the Super Admin console.
 * Suspend and delete go through a confirm modal (`pending`); reactivate is immediate.
 */
export function useUserActions() {
  const { invalidate } = useInvalidateQueries();
  const [pending, setPending] = useState<PendingUserAction>(null);
  const [busyUserId, setBusyUserId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const run = async (user: AdminUser, action: () => Promise<unknown>, fallback: string) => {
    setBusyUserId(user.id);
    setError(null);
    try {
      await action();
      invalidate(['admin-users']);
      setPending(null);
    } catch (err) {
      setError(errorMessage(err, fallback));
    } finally {
      setBusyUserId(null);
    }
  };

  const setStatus = (user: AdminUser, status: 'active' | 'suspended') =>
    run(
      user,
      () => apiPatch(IDENTITY.ADMIN_USER_STATUS(user.id), { status }),
      `Failed to ${status === 'active' ? 'reactivate' : 'suspend'} ${fullName(user)}.`,
    );

  const confirm = () => {
    if (!pending) return Promise.resolve();
    const { kind, user } = pending;
    return kind === 'suspend'
      ? setStatus(user, 'suspended')
      : run(user, () => apiDelete(IDENTITY.ADMIN_USER(user.id)), `Failed to delete ${fullName(user)}.`);
  };

  return {
    pending,
    busyUserId,
    error,
    clearError: () => setError(null),
    requestSuspend: (user: AdminUser) => { setError(null); setPending({ kind: 'suspend', user }); },
    requestDelete: (user: AdminUser) => { setError(null); setPending({ kind: 'delete', user }); },
    activate: (user: AdminUser) => setStatus(user, 'active'),
    confirm,
    cancel: () => { if (!busyUserId) setPending(null); },
  };
}
