import type { NextAuthOptions } from 'next-auth';
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

          // Backend emits either "admin" (legacy role claim) or "platform_admin".
          // Accept both so rotation from one to the other doesn't lock admins out.
          if (user.role !== 'admin' && user.role !== 'platform_admin') return null;

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
      if (user) {
        token.accessToken = user.accessToken;
        token.refreshToken = user.refreshToken;
        token.role = user.role;
        token.adminRole = user.adminRole;
      }
      return token;
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
