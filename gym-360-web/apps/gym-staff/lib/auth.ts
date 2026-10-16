import type { NextAuthOptions, User } from 'next-auth';
import type { JWT } from 'next-auth/jwt';
import CredentialsProvider from 'next-auth/providers/credentials';
import type { StaffSubRole } from '@gym360/ui';

export interface OwnedGym {
  gymId: string;
  name: string;
  slug: string;
  schemaPrefix: string;
}

declare module 'next-auth' {
  interface Session {
    accessToken: string;
    user: {
      id: string;
      email: string;
      name: string;
      role: string;
      staffSubRole: StaffSubRole;
      gymId: string;
      gymName: string;
      schemaPrefix?: string;
      principalType?: string;
      ownerId?: string | null;
      ownedGyms?: OwnedGym[];
      image?: string;
    };
  }

  interface User {
    id: string;
    email: string;
    name: string;
    accessToken: string;
    refreshToken: string;
    role: string;
    staffSubRole: StaffSubRole;
    gymId: string;
    gymName: string;
    schemaPrefix?: string;
    principalType?: string;
    ownerId?: string | null;
    ownedGyms?: OwnedGym[];
  }
}

declare module 'next-auth/jwt' {
  interface JWT {
    accessToken: string;
    refreshToken: string;
    role: string;
    staffSubRole: StaffSubRole;
    gymId: string;
    gymName: string;
    schemaPrefix?: string;
    principalType?: string;
    ownerId?: string | null;
    ownedGyms?: OwnedGym[];
    ctxRefreshedAt?: number;
    accessTokenExpires?: number;
  }
}

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:8080/api/v1';
const STAFF_ROLES = ['gym_staff', 'gym_owner'];
const CTX_REFRESH_INTERVAL_MS = 60_000;
// Refresh the access token this many ms before its embedded `exp`, so client
// API calls never fire with an expired token (which would 401 → forced sign-out).
const ACCESS_TOKEN_REFRESH_SKEW_MS = 60_000;

function toOwnedGyms(raw: unknown): OwnedGym[] {
  return Array.isArray(raw)
    ? raw.map((g: any) => ({
        gymId: g.gymId,
        name: g.name,
        slug: g.slug,
        schemaPrefix: g.schemaPrefix,
      }))
    : [];
}

// Claims of an access token, or null when it cannot be decoded.
function decodeJwtPayload(accessToken: string): Record<string, any> | null {
  try {
    return JSON.parse(Buffer.from(accessToken.split('.')[1], 'base64').toString('utf-8'));
  } catch {
    return null;
  }
}

// Decode the `exp` claim (seconds) of an access token into epoch ms.
function decodeJwtExpMs(accessToken: string): number | null {
  const exp = decodeJwtPayload(accessToken)?.exp;
  return typeof exp === 'number' ? exp * 1000 : null;
}

async function fetchCurrentUser(accessToken: string): Promise<{
  gymId: string;
  gymName: string;
  principalType: string | null;
  ownerId: string | null;
  ownedGyms: OwnedGym[];
} | 'unauthorized' | null> {
  try {
    const res = await fetch(`${API_URL}/identity/me`, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });

    if (res.status === 401) return 'unauthorized';
    if (!res.ok) return null;

    const body = await res.json();
    const u = body?.data;
    if (!u) return null;

    return {
      gymId: u.gymId ?? '',
      gymName: u.gymName ?? '',
      principalType: u.principalType ?? null,
      ownerId: u.ownerId ?? null,
      ownedGyms: toOwnedGyms(u.ownedGyms),
    };
  } catch {
    return null;
  }
}

type TenantContext = Exclude<Awaited<ReturnType<typeof fetchCurrentUser>>, 'unauthorized' | null>;

// Merge freshly-fetched /identity/me tenant context into the token.
//
// Owners are special: /me always reports gymId='' for them (context is derived
// from the gym registry, not the per-session selected-gym token claim). Blindly
// copying that would wipe the owner's selected gym on every 60s context refresh
// (and on token rotation), bouncing them back to /select-gym in an endless loop.
// So for owners we KEEP the selected gymId as long as it is still one of their
// ownedGyms, only clearing it when that gym is gone (deleted / access revoked) —
// which still forces a legitimate re-pick. Staff/managers keep the /me value.
function mergeTenantContext(token: JWT, fresh: TenantContext): void {
  token.principalType = fresh.principalType ?? token.principalType;
  token.ownerId = fresh.ownerId;
  token.ownedGyms = fresh.ownedGyms;

  if (token.role === 'gym_owner') {
    const selected = token.gymId
      ? fresh.ownedGyms.find((g) => g.gymId === token.gymId)
      : undefined;
    if (selected) {
      token.gymName = selected.name ?? token.gymName; // retain the live selection
    } else {
      token.gymId = '';                               // gym gone → force re-pick
      token.gymName = '';
    }
  } else {
    token.gymId = fresh.gymId;
    token.gymName = fresh.gymName;
  }
}

// Exchange a long-lived refresh token for a fresh access/refresh pair.
// The backend rotates (revokes) the old refresh token, so the new value MUST
// be persisted. Note: /identity/refresh-token returns an impoverished user
// payload (ownedGyms/ownerId/principalType are null), so callers must refetch
// tenant context from /identity/me with the new access token — see
// rotateAndRefreshContext.
async function refreshTokens(
  refreshToken?: string
): Promise<{ accessToken: string; refreshToken: string } | null> {
  if (!refreshToken) return null;
  try {
    const res = await fetch(`${API_URL}/identity/refresh-token`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken }),
    });
    if (!res.ok) return null;
    const body = await res.json();
    const d = body?.data;
    if (!d?.accessToken || !d?.refreshToken) return null;
    return { accessToken: d.accessToken, refreshToken: d.refreshToken };
  } catch {
    return null;
  }
}

// Rotate the token pair, then refetch authoritative tenant context (gymId,
// ownedGyms, ownerId, principalType) from /identity/me with the new token —
// the refresh-token response omits those. Mutates `token`; returns true on
// success. For Owners /me reports gymId=null, so they re-pick at /select-gym
// (single-gym owners auto-select).
async function rotateAndRefreshContext(token: JWT): Promise<boolean> {
  const rotated = await refreshTokens(token.refreshToken);
  if (!rotated) return false;
  token.accessToken = rotated.accessToken;
  token.refreshToken = rotated.refreshToken;
  token.accessTokenExpires = decodeJwtExpMs(rotated.accessToken) ?? 0;

  const fresh = await fetchCurrentUser(token.accessToken);
  if (fresh && fresh !== 'unauthorized') {
    mergeTenantContext(token, fresh);
  }
  token.ctxRefreshedAt = Date.now();
  return true;
}

function normalizeRole(role?: string): string {
  if (!role) return '';
  const map: Record<string, string> = {
    GymStaff: 'gym_staff',
    GymOwner: 'gym_owner',
    Trainee: 'trainee',
    Trainer: 'trainer',
    Admin: 'admin',
    staff: 'gym_staff',
    owner: 'gym_owner',
  };
  return map[role] ?? role.toLowerCase();
}

function defaultSubRole(role: string): StaffSubRole {
  return role === 'gym_owner' ? 'owner' : 'receptionist';
}

// Tenant claims from the login response, falling back to the token payload.
function tenantClaims(user: any, payload: Record<string, any>) {
  return {
    gymId: user.gymId ?? payload.gymId ?? '',
    schemaPrefix: payload.schemaPrefix ?? '',
    principalType: user.principalType ?? payload.principalType ?? null,
    ownerId: user.ownerId ?? payload.ownerId ?? null,
  };
}

// Map the /identity/login response onto the NextAuth user. Returns null for
// non-staff roles and for an access token that cannot be decoded.
function toStaffUser(accessToken: string, refreshToken: string, user: any): User | null {
  const role = normalizeRole(user.role);
  if (!STAFF_ROLES.includes(role)) return null;

  const payload = decodeJwtPayload(accessToken);
  if (!payload) return null;

  // Hard-mode multi-tenancy claims (FOUNDER_DECISIONS Q3 revised 2026-05-01).
  // Owners with multiple gyms get an Owner-scoped token (no gymId until
  // they pick one via /api/select-gym). UserResponse.ownedGyms surfaces
  // the chooser list; UserResponse.gymId is null until selection.
  return {
    id: user.id,
    email: user.email,
    name: `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim() || user.email,
    accessToken,
    refreshToken,
    role,
    staffSubRole: (user.staffSubRole ?? payload.staffRole ?? defaultSubRole(role)) as StaffSubRole,
    gymName: user.gymName ?? '',
    ownedGyms: toOwnedGyms(user.ownedGyms),
    ...tenantClaims(user, payload),
  };
}

function applySignIn(token: JWT, user: User): JWT {
  token.accessToken = user.accessToken;
  token.refreshToken = user.refreshToken;
  token.role = user.role;
  token.staffSubRole = user.staffSubRole;
  token.gymId = user.gymId;
  token.gymName = user.gymName;
  token.schemaPrefix = user.schemaPrefix;
  token.principalType = user.principalType;
  token.ownerId = user.ownerId;
  token.ownedGyms = user.ownedGyms;
  token.accessTokenExpires = decodeJwtExpMs(user.accessToken) ?? 0;
  token.ctxRefreshedAt = Date.now();
  return token;
}

// session.update() flow — used after Owner picks a gym at /select-gym.
// The route hits POST /api/v1/identity/select-gym, gets a fresh token
// with gymId/schemaPrefix populated, then calls update(...) with the
// new claims.
function applySessionUpdate(token: JWT, update: Partial<JWT>): JWT {
  if (update.accessToken) {
    token.accessToken = update.accessToken;
    token.accessTokenExpires = decodeJwtExpMs(update.accessToken) ?? token.accessTokenExpires;
  }
  if (update.gymId !== undefined) token.gymId = update.gymId;
  if (update.gymName !== undefined) token.gymName = update.gymName;
  if (update.schemaPrefix !== undefined) token.schemaPrefix = update.schemaPrefix;
  token.ctxRefreshedAt = Date.now();
  return token;
}

function isNearExpiry(token: JWT): boolean {
  const expiresAt = token.accessTokenExpires ?? 0;
  return expiresAt > 0 && Date.now() > expiresAt - ACCESS_TOKEN_REFRESH_SKEW_MS;
}

function isExpired(token: JWT): boolean {
  const expiresAt = token.accessTokenExpires ?? 0;
  return expiresAt > 0 && Date.now() >= expiresAt;
}

// --- Access-token rotation -------------------------------------------
// The NextAuth session cookie lives 24h (maxAge below) but a backend
// access token lives ~1h. Without rotation the cookie outlives its token:
// the user is left "authenticated" with a dead token, every API call 401s,
// and (for Owners) /select-gym auto-select fails with "Unauthenticated".
// Exchange the long-lived refresh token for a fresh pair BEFORE the access
// token expires; also recovers a token blanked by an older build.
function needsRotation(token: JWT): boolean {
  if (!token.refreshToken) return false;
  return !token.accessToken || isNearExpiry(token);
}

async function rotateOrInvalidate(token: JWT): Promise<JWT> {
  const unusable = !token.accessToken || isExpired(token);
  if (await rotateAndRefreshContext(token)) return token;
  // Rotation failed. Only force re-auth if the current access token is
  // actually unusable (blank or expired). If we're merely inside the
  // pre-expiry skew window with a still-valid token, keep it and let a
  // later cycle retry — this avoids a false logout when a concurrent
  // request already rotated (the backend revokes the old refresh token).
  return unusable ? { ...token, accessToken: '' } : token;
}

function isContextStale(token: JWT): boolean {
  return Date.now() - (token.ctxRefreshedAt ?? 0) > CTX_REFRESH_INTERVAL_MS;
}

// Refresh tenant context from the backend periodically. Prevents stale
// gymId / schemaPrefix from sticking in long-lived NextAuth cookies after
// a re-seed, gym deletion, or membership change.
async function refreshContextIfStale(token: JWT): Promise<JWT> {
  if (!token.accessToken || !isContextStale(token)) return token;

  const fresh = await fetchCurrentUser(token.accessToken);
  if (fresh === 'unauthorized') {
    // Token rejected mid-life (e.g. expired between rotation checks).
    // Try a rotation before forcing re-auth.
    return (await rotateAndRefreshContext(token)) ? token : { ...token, accessToken: '' };
  }
  if (fresh) {
    mergeTenantContext(token, fresh);
  }
  token.ctxRefreshedAt = Date.now();
  return token;
}

export const authOptions: NextAuthOptions = {
  providers: [
    CredentialsProvider({
      name: 'credentials',
      credentials: {
        email: { label: 'Email', type: 'email' },
        password: { label: 'Password', type: 'password' },
      },
      async authorize(credentials) {
        if (!credentials?.email || !credentials?.password) return null;

        try {
          const res = await fetch(`${API_URL}/identity/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email: credentials.email, password: credentials.password }),
          });

          if (!res.ok) return null;
          const data = await res.json();
          const { accessToken, refreshToken, user } = data.data ?? {};
          if (!accessToken || !user) return null;

          return toStaffUser(accessToken, refreshToken, user);
        } catch {
          return null;
        }
      },
    }),
  ],
  callbacks: {
    async jwt({ token, user, trigger, session }) {
      if (user) return applySignIn(token, user);
      if (trigger === 'update' && session) return applySessionUpdate(token, session);
      if (needsRotation(token)) return rotateOrInvalidate(token);
      return refreshContextIfStale(token);
    },
    async session({ session, token }) {
      session.accessToken = token.accessToken;
      // NextAuth puts the user id in token.sub during sign-in; surface it on
      // session.user.id so callers (e.g. quick-assign sets AssignedBy = userId)
      // don't see undefined and throw "No active session.".
      session.user.id = (token.sub as string) ?? '';
      session.user.role = token.role;
      session.user.staffSubRole = token.staffSubRole;
      session.user.gymId = token.gymId;
      session.user.gymName = token.gymName;
      session.user.schemaPrefix = token.schemaPrefix;
      session.user.principalType = token.principalType;
      session.user.ownerId = token.ownerId;
      session.user.ownedGyms = token.ownedGyms;
      return session;
    },
  },
  pages: { signIn: '/login' },
  session: { strategy: 'jwt', maxAge: 24 * 60 * 60 },
};

export function canAccess(staffSubRole: StaffSubRole, permission: string): boolean {
  const permissions: Record<StaffSubRole, string[]> = {
    owner: ['all'],
    manager: ['manage_members', 'manage_trainers', 'manage_classes', 'manage_staff', 'manage_announcements', 'manage_check_ins', 'view_analytics', 'manage_billing'],
    receptionist: ['manage_check_ins', 'manage_members_view', 'manage_billing_view'],
  };

  const allowed = permissions[staffSubRole];
  return allowed?.includes('all') || allowed?.includes(permission) || false;
}
