'use client';

import { useCallback } from 'react';
import type { User, AdminRole, StaffSubRole } from '../types/user';

interface AuthContext {
  user: User | null;
  isAuthenticated: boolean;
  hasAdminRole: (role: AdminRole) => boolean;
  hasStaffRole: (role: StaffSubRole) => boolean;
  canAccess: (requiredRoles: AdminRole[]) => boolean;
  isSuperAdmin: boolean;
  isAdmin: boolean;
  isSupport: boolean;
  isAnalyst: boolean;
  isOwner: boolean;
  isManager: boolean;
  isReceptionist: boolean;
}

const ADMIN_RANK: Record<AdminRole, number> = {
  super_admin: 4,
  admin: 3,
  support: 2,
  analyst: 1,
};

const STAFF_RANK: Record<StaffSubRole, number> = {
  owner: 3,
  manager: 2,
  receptionist: 1,
};

/** True when `actual` is set and ranks at or above `required`. */
function meetsRank<R extends string>(rank: Record<R, number>, actual: R | undefined | null, required: R): boolean {
  if (!actual) return false;
  return rank[actual] >= rank[required];
}

/** Flag set derived from the user's admin role / staff sub-role. */
function roleFlags(user: User | null) {
  const adminRole = user?.adminRole;
  const staffRole = user?.staffSubRole;
  return {
    isSuperAdmin: adminRole === 'super_admin',
    isAdmin: adminRole === 'admin' || adminRole === 'super_admin',
    isSupport: adminRole === 'support',
    isAnalyst: adminRole === 'analyst',
    isOwner: staffRole === 'owner',
    isManager: staffRole === 'manager' || staffRole === 'owner',
    isReceptionist: staffRole === 'receptionist',
  };
}

/**
 * Client-side auth utility hook. The actual session is managed by next-auth.
 * This hook provides convenient role-checking helpers.
 */
export function useAuthHelpers(user: User | null): AuthContext {
  const hasAdminRole = useCallback(
    (role: AdminRole): boolean => meetsRank(ADMIN_RANK, user?.adminRole, role),
    [user]
  );

  const hasStaffRole = useCallback(
    (role: StaffSubRole): boolean => meetsRank(STAFF_RANK, user?.staffSubRole, role),
    [user]
  );

  const canAccess = useCallback(
    (requiredRoles: AdminRole[]): boolean => {
      if (!user?.adminRole) return false;
      return requiredRoles.includes(user.adminRole);
    },
    [user]
  );

  return {
    user,
    isAuthenticated: !!user,
    hasAdminRole,
    hasStaffRole,
    canAccess,
    ...roleFlags(user),
  };
}
