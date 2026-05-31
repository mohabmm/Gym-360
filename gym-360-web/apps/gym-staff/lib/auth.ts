import type { NextAuthOptions } from 'next-auth';
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

const CTX_REFRESH_INTERVAL_MS = 60_000;
// Refresh the access token this many ms before its embedded `exp`, so client
// API calls never fire with an expired token (which would 401 → forced sign-out).
const ACCESS_TOKEN_REFRESH_SKEW_MS = 60_000;

async function fetchCurrentUser(accessToken: string): Promise<{
  gymId: string;
  gymName: string;
  principalType: string | null;
  ownerId: string | null;
  ownedGyms: OwnedGym[];
} | 'unauthorized' | null> {
  try {
    const res = await fetch(
      `${process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:8080/api/v1'}/identity/me`,
      { headers: { Authorization: `Bearer ${accessToken}` } }
    );

    if (res.status === 401) return 'unauthorized';
    if (!res.ok) return null;

    const body = await res.json();
    const u = body?.data;
    if (!u) return null;

    const ownedGyms: OwnedGym[] = Array.isArray(u.ownedGyms)
      ? u.ownedGyms.map((g: any) => ({
          gymId: g.gymId,
          name: g.name,
          slug: g.slug,
          schemaPrefix: g.schemaPrefix,
        }))
      : [];

    return {
      gymId: u.gymId ?? '',
      gymName: u.gymName ?? '',
      principalType: u.principalType ?? null,
      ownerId: u.ownerId ?? null,
      ownedGyms,
    };
  } catch {
    return null;
  }
}

// Decode the `exp` claim (seconds) of an access token into epoch ms.
function decodeJwtExpMs(accessToken: string): number | null {
  try {
    const payload = JSON.parse(
      Buffer.from(accessToken.split('.')[1], 'base64').toString('utf-8')
    );
    return typeof payload.exp === 'number' ? payload.exp * 1000 : null;
  } catch {
    return null;
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
    const res = await fetch(
      `${process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:8080/api/v1'}/identity/refresh-token`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken }),
      }
    );
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
    token.gymId = fresh.gymId;
    token.gymName = fresh.gymName;
    token.principalType = fresh.principalType ?? token.principalType;
    token.ownerId = fresh.ownerId;
    token.ownedGyms = fresh.ownedGyms;
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
          const res = await fetch(
            `${process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:8080/api/v1'}/identity/login`,
            {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({ email: credentials.email, password: credentials.password }),
            }
          );

          if (!res.ok) return null;
          const data = await res.json();
          const { accessToken, refreshToken, user } = data.data ?? {};
          if (!accessToken || !user) return null;

          const normalizedRole = normalizeRole(user.role);
          if (!['gym_staff', 'gym_owner'].includes(normalizedRole)) return null;

          const payload = JSON.parse(
            Buffer.from(accessToken.split('.')[1], 'base64').toString('utf-8')
          );
          const subRole = (user.staffSubRole ?? payload.staffRole ??
            (normalizedRole === 'gym_owner' ? 'owner' : 'receptionist')) as StaffSubRole;

          // Hard-mode multi-tenancy claims (FOUNDER_DECISIONS Q3 revised 2026-05-01).
          // Owners with multiple gyms get an Owner-scoped token (no gymId until
          // they pick one via /api/select-gym). UserResponse.ownedGyms surfaces
          // the chooser list; UserResponse.gymId is null until selection.
          const ownedGyms: OwnedGym[] = Array.isArray(user.ownedGyms)
            ? user.ownedGyms.map((g: any) => ({
                gymId: g.gymId,
                name: g.name,
                slug: g.slug,
                schemaPrefix: g.schemaPrefix,
              }))
            : [];

          return {
            id: user.id,
            email: user.email,
            name: `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim() || user.email,
            accessToken,
            refreshToken,
            role: normalizedRole,
            staffSubRole: subRole,
            gymId: user.gymId ?? payload.gymId ?? '',
            gymName: user.gymName ?? '',
            schemaPrefix: payload.schemaPrefix ?? '',
            principalType: user.principalType ?? payload.principalType ?? null,
            ownerId: user.ownerId ?? payload.ownerId ?? null,
            ownedGyms,
          };
        } catch {
          return null;
        }
      },
    }),
  ],
  callbacks: {
    async jwt({ token, user, trigger, session }) {
      if (user) {
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
      if (trigger === 'update' && session) {
        if (session.accessToken) token.accessToken = session.accessToken;
        if (session.gymId !== undefined) token.gymId = session.gymId;
        if (session.gymName !== undefined) token.gymName = session.gymName;
        if (session.schemaPrefix !== undefined) token.schemaPrefix = session.schemaPrefix;
        if (session.accessToken) {
          token.accessTokenExpires = decodeJwtExpMs(session.accessToken) ?? token.accessTokenExpires;
        }
        token.ctxRefreshedAt = Date.now();
        return token;
      }

      // --- Access-token rotation -------------------------------------------
      // The NextAuth session cookie lives 24h (maxAge below) but a backend
      // access token lives ~1h. Without rotation the cookie outlives its token:
      // the user is left "authenticated" with a dead token, every API call 401s,
      // and (for Owners) /select-gym auto-select fails with "Unauthenticated".
      // Exchange the long-lived refresh token for a fresh pair BEFORE the access
      // token expires; also recovers a token blanked by an older build.
      const expiresAt = token.accessTokenExpires ?? 0;
      const nearExpiry = expiresAt > 0 && Date.now() > expiresAt - ACCESS_TOKEN_REFRESH_SKEW_MS;
      const expired = expiresAt > 0 && Date.now() >= expiresAt;
      if (token.refreshToken && (!token.accessToken || nearExpiry)) {
        if (await rotateAndRefreshContext(token)) return token;
        // Rotation failed. Only force re-auth if the current access token is
        // actually unusable (blank or expired). If we're merely inside the
        // pre-expiry skew window with a still-valid token, keep it and let a
        // later cycle retry — this avoids a false logout when a concurrent
        // request already rotated (the backend revokes the old refresh token).
        if (!token.accessToken || expired) {
          return { ...token, accessToken: '' };
        }
        return token;
      }

      // Refresh tenant context from the backend periodically. Prevents stale
      // gymId / schemaPrefix from sticking in long-lived NextAuth cookies after
      // a re-seed, gym deletion, or membership change.
      const lastRefresh = token.ctxRefreshedAt ?? 0;
      if (token.accessToken && Date.now() - lastRefresh > CTX_REFRESH_INTERVAL_MS) {
        const fresh = await fetchCurrentUser(token.accessToken);
        if (fresh === 'unauthorized') {
          // Token rejected mid-life (e.g. expired between rotation checks).
          // Try a rotation before forcing re-auth.
          if (await rotateAndRefreshContext(token)) return token;
          return { ...token, accessToken: '' };
        }
        if (fresh) {
          token.gymId = fresh.gymId;
          token.gymName = fresh.gymName;
          token.principalType = fresh.principalType ?? token.principalType;
          token.ownerId = fresh.ownerId;
          token.ownedGyms = fresh.ownedGyms;
        }
        token.ctxRefreshedAt = Date.now();
      }

      return token;
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
