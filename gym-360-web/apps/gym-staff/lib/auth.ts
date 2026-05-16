import type { NextAuthOptions } from 'next-auth';
import CredentialsProvider from 'next-auth/providers/credentials';
import type { StaffSubRole } from '@gymapp/ui';

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
  }
}

const CTX_REFRESH_INTERVAL_MS = 60_000;

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
        token.ctxRefreshedAt = Date.now();
        return token;
      }

      // Refresh tenant context from the backend periodically. Prevents stale
      // gymId / schemaPrefix from sticking in long-lived NextAuth cookies after
      // a re-seed, gym deletion, or membership change.
      const lastRefresh = token.ctxRefreshedAt ?? 0;
      if (token.accessToken && Date.now() - lastRefresh > CTX_REFRESH_INTERVAL_MS) {
        const fresh = await fetchCurrentUser(token.accessToken);
        if (fresh === 'unauthorized') {
          // Backend rejected token. Force re-auth by clearing the session token.
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
