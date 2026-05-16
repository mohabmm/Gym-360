/**
 * Backend emits the short forms: 'admin' | 'owner' | 'staff' | 'trainer' | 'trainee'.
 * Legacy forms ('platform_admin', 'gym_owner', 'gym_staff') are still accepted by
 * frontends for backwards compatibility. See CLAUDE.md §"Team 5 (API)".
 */
export type UserRole =
  | 'trainee'
  | 'trainer'
  | 'staff'
  | 'owner'
  | 'admin'
  // Legacy aliases — kept so old sessions/snapshots continue to resolve.
  | 'gym_staff'
  | 'gym_owner'
  | 'platform_admin';

export type AdminRole = 'super_admin' | 'admin' | 'support' | 'analyst';

export type StaffSubRole = 'owner' | 'manager' | 'receptionist';

/**
 * Normalises either the short or legacy role form into a single canonical short form.
 * Use this in RBAC checks so the UI does not have to branch on both.
 */
export function normaliseRole(role: UserRole): 'trainee' | 'trainer' | 'staff' | 'owner' | 'admin' {
  switch (role) {
    case 'gym_staff':
      return 'staff';
    case 'gym_owner':
      return 'owner';
    case 'platform_admin':
      return 'admin';
    default:
      return role;
  }
}

export type UserStatus = 'active' | 'inactive' | 'suspended' | 'pending';

export interface User {
  id: string;
  email: string;
  phone: string;
  firstName: string;
  lastName: string;
  firstNameAr: string;
  lastNameAr: string;
  role: UserRole;
  adminRole?: AdminRole;
  staffSubRole?: StaffSubRole;
  status: UserStatus;
  avatarUrl?: string;
  createdAt: string;
  lastLoginAt?: string;
  consentGivenAt?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface AuthSession {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface AdminUserGym {
  id: string;
  name: string;
}

export interface AdminUser extends User {
  adminRole: AdminRole;
  isYou?: boolean;
  department?: string;
  gyms?: AdminUserGym[];
}

export interface StaffMember extends User {
  staffSubRole: StaffSubRole;
  gymId: string;
  hireDate: string;
  permissions: StaffPermission[];
}

export type StaffPermission =
  | 'manage_members'
  | 'manage_trainers'
  | 'manage_billing'
  | 'manage_classes'
  | 'manage_staff'
  | 'manage_settings'
  | 'view_analytics'
  | 'manage_announcements'
  | 'manage_check_ins';
