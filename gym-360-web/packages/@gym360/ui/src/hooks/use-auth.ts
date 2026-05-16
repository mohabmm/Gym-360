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

/**
 * Client-side auth utility hook. The actual session is managed by next-auth.
 * This hook provides convenient role-checking helpers.
 */
export function useAuthHelpers(user: User | null): AuthContext {
  const hasAdminRole = useCallback(
    (role: AdminRole): boolean => {
      if (!user?.adminRole) return false;
      const hierarchy: Record<AdminRole, number> = {
        super_admin: 4,
        admin: 3,
        support: 2,
        analyst: 1,
      };
      return hierarchy[user.adminRole] >= hierarchy[role];
    },
    [user]
  );

  const hasStaffRole = useCallback(
    (role: StaffSubRole): boolean => {
      if (!user?.staffSubRole) return false;
      const hierarchy: Record<StaffSubRole, number> = {
        owner: 3,
        manager: 2,
        receptionist: 1,
      };
      return hierarchy[user.staffSubRole] >= hierarchy[role];
    },
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
    isSuperAdmin: user?.adminRole === 'super_admin',
    isAdmin: user?.adminRole === 'admin' || user?.adminRole === 'super_admin',
    isSupport: user?.adminRole === 'support',
    isAnalyst: user?.adminRole === 'analyst',
    isOwner: user?.staffSubRole === 'owner',
    isManager: user?.staffSubRole === 'manager' || user?.staffSubRole === 'owner',
    isReceptionist: user?.staffSubRole === 'receptionist',
  };
}
