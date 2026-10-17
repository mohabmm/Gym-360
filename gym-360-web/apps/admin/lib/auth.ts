import type { NextAuthOptions, User } from 'next-auth';
import type { JWT } from 'next-auth/jwt';
import CredentialsProvider from 'next-auth/providers/credentials';
import type { AdminRole } from '@gym360/ui';

declare module 'next-auth' {
  interface Session {
    accessToken: string;
    user: {
      id: string;
      email: string;
      name: string;
      role: string;
      adminRole: AdminRole;
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
    adminRole: AdminRole;
  }
}

declare module 'next-auth/jwt' {
  interface JWT {
    accessToken: string;
    refreshToken: string;
    expiresAt: number;
    role: string;
    adminRole: AdminRole;
  }
}

// Refresh the access token this many ms before its embedded `exp`, so client
// API calls never fire with an expired token (which would 401 → forced sign-out).
const ACCESS_TOKEN_REFRESH_SKEW_MS = 60_000;

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
// be persisted. Mutates `token`; returns true on success. The admin role
// claim does not change across refreshes, so it is preserved as-is.
async function rotateTokens(token: JWT): Promise<boolean> {
  if (!token.refreshToken) return false;
  try {
    const res = await fetch(
      `${process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:8080/api/v1'}/identity/refresh-token`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken: token.refreshToken }),
      }
    );
    if (!res.ok) return false;
    const body = await res.json();
    const d = body?.data;
    if (!d?.accessToken || !d?.refreshToken) return false;
    token.accessToken = d.accessToken;
    token.refreshToken = d.refreshToken;
    token.expiresAt = decodeJwtExpMs(d.accessToken) ?? 0;
    return true;
  } catch {
    return false;
  }
}

// Copy the freshly signed-in user's credentials and role claims onto the JWT.
function applySignIn(token: JWT, user: User): JWT {
  token.accessToken = user.accessToken;
  token.refreshToken = user.refreshToken;
  token.expiresAt = decodeJwtExpMs(user.accessToken) ?? 0;
  token.role = user.role;
  token.adminRole = user.adminRole;
  return token;
}

// True once the access token is inside the pre-expiry refresh window.
function isNearExpiry(expiresAt: number): boolean {
  return expiresAt > 0 && Date.now() > expiresAt - ACCESS_TOKEN_REFRESH_SKEW_MS;
}

// True once the access token is past its embedded expiry.
function isExpired(expiresAt: number): boolean {
  return expiresAt > 0 && Date.now() >= expiresAt;
}

// A refresh is attempted when we hold a refresh token and the access token is
// missing or about to expire.
function needsRotation(token: JWT): boolean {
  if (!token.refreshToken) return false;
  return !token.accessToken || isNearExpiry(token.expiresAt ?? 0);
}

// The current access token can no longer be used for API calls.
function isTokenUnusable(token: JWT): boolean {
  return !token.accessToken || isExpired(token.expiresAt ?? 0);
}

// Backend emits either "admin" (legacy role claim) or "platform_admin".
// Accept both so rotation from one to the other doesn't lock admins out.
function isPlatformAdminRole(role: string): boolean {
  return role === 'admin' || role === 'platform_admin';
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
              body: JSON.stringify({
                email: credentials.email,
                password: credentials.password,
              }),
            }
          );

          if (!res.ok) return null;

          const data = await res.json();
          const { user, accessToken, refreshToken } = data.data;

          if (!isPlatformAdminRole(user.role)) return null;

          // adminRole lives in the JWT payload, not the user object
          const payload = JSON.parse(
            Buffer.from(accessToken.split('.')[1], 'base64').toString('utf-8')
          );

          return {
            id: user.id,
            email: user.email,
            name: `${user.firstName} ${user.lastName}`,
            accessToken,
            refreshToken,
            role: user.role,
            adminRole: (payload.adminRole ?? 'admin') as AdminRole,
          };
        } catch {
          return null;
        }
      },
    }),
  ],
  callbacks: {
    async jwt({ token, user }) {
      if (user) return applySignIn(token, user);

      // The NextAuth session cookie lives 24h (maxAge below) but a backend
      // access token lives ~1h. Without rotation the cookie outlives its token:
      // every admin API call 401s and the user is bounced to /login mid-session.
      // Exchange the long-lived refresh token for a fresh pair before expiry;
      // also recovers a token blanked by an older build.
      if (!needsRotation(token)) return token;
      if (await rotateTokens(token)) return token;
      // Rotation failed. Only force re-auth if the current token is genuinely
      // unusable; keep a still-valid token in the pre-expiry skew window so a
      // later cycle retries instead of a false logout (the backend revokes
      // the old refresh token on rotation, so concurrent calls can race).
      return isTokenUnusable(token) ? { ...token, accessToken: '' } : token;
    },
    async session({ session, token }) {
      session.accessToken = token.accessToken;
      session.user.role = token.role;
      session.user.adminRole = token.adminRole;
      return session;
    },
  },
  pages: {
    signIn: '/login',
  },
  session: {
    strategy: 'jwt',
    maxAge: 24 * 60 * 60,
  },
};

// Admin role access matrix
export const ADMIN_ACCESS: Record<string, AdminRole[]> = {
  '/': ['super_admin', 'admin', 'support', 'analyst'],
  '/gyms': ['super_admin', 'admin', 'support'],
  '/gyms/onboarding': ['super_admin', 'admin'],
  '/users': ['super_admin', 'admin', 'support'],
  '/subscriptions': ['super_admin', 'admin'],
  '/plans': ['super_admin', 'admin'],
  '/analytics': ['super_admin', 'admin', 'support', 'analyst'],
  '/analytics/churn': ['super_admin', 'admin', 'analyst'],
  '/content': ['super_admin', 'admin'],
  '/settings': ['super_admin'],
};

export function canAccessRoute(adminRole: AdminRole, path: string): boolean {
  // Find the most specific matching route
  const routes = Object.keys(ADMIN_ACCESS).sort((a, b) => b.length - a.length);
  for (const route of routes) {
    if (path === route || path.startsWith(route + '/')) {
      const allowedRoles = ADMIN_ACCESS[route];
      return allowedRoles ? allowedRoles.includes(adminRole) : false;
    }
  }
  return true; // Default allow for unspecified routes
}

export function isReadOnly(adminRole: AdminRole): boolean {
  return adminRole === 'support';
}
