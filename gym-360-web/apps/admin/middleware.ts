import { withAuth } from 'next-auth/middleware';
import { NextResponse } from 'next/server';
import type { JWT } from 'next-auth/jwt';

const ADMIN_PLUS_ROLES = ['super_admin', 'admin'];

// Must be a platform admin (API emits role="admin" or "platform_admin" with adminRole claim)
function isPlatformAdmin(token: JWT | null): token is JWT {
  return token?.role === 'admin' || token?.role === 'platform_admin';
}

function isAdminPlus(token: JWT): boolean {
  return ADMIN_PLUS_ROLES.includes(token.adminRole);
}

// Settings: super_admin only
function isSettingsDenied(path: string, token: JWT): boolean {
  return path.startsWith('/settings') && token.adminRole !== 'super_admin';
}

// Plans, Subscriptions, Onboarding, Content: admin+ only
function isAdminPlusPath(path: string): boolean {
  return (
    path.startsWith('/plans') ||
    path.startsWith('/subscriptions') ||
    path === '/gyms/onboarding' ||
    path.startsWith('/content')
  );
}

// Analyst can only see analytics
function isAnalystOutOfScope(path: string, token: JWT): boolean {
  if (token.adminRole !== 'analyst') return false;
  return !path.startsWith('/analytics') && path !== '/';
}

// Resolve where (if anywhere) the request must be redirected.
function resolveRedirect(path: string, token: JWT | null): string | null {
  if (!isPlatformAdmin(token)) return '/login';
  if (isSettingsDenied(path, token)) return '/';
  if (isAdminPlusPath(path) && !isAdminPlus(token)) return '/';
  if (isAnalystOutOfScope(path, token)) return '/analytics';
  return null;
}

export default withAuth(
  function middleware(req) {
    const redirectTo = resolveRedirect(req.nextUrl.pathname, req.nextauth.token);
    if (redirectTo) {
      return NextResponse.redirect(new URL(redirectTo, req.url));
    }
    return NextResponse.next();
  },
  {
    callbacks: {
      authorized: ({ token }) => !!token,
    },
  }
);

export const config = {
  matcher: [
    '/((?!login|api|_next/static|_next/image|favicon.ico|logo.svg|apple-touch-icon.png|android-chrome-.*\\.png|favicon-.*\\.png|robots.txt|sitemap.xml).*)',
  ],
};
