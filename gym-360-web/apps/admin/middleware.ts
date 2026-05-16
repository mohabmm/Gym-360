import { withAuth } from 'next-auth/middleware';
import { NextResponse } from 'next/server';

export default withAuth(
  function middleware(req) {
    const token = req.nextauth.token;
    const path = req.nextUrl.pathname;

    // Must be a platform admin (API emits role="admin" or "platform_admin" with adminRole claim)
    if (token?.role !== 'admin' && token?.role !== 'platform_admin') {
      return NextResponse.redirect(new URL('/login', req.url));
    }

    // Settings: super_admin only
    if (path.startsWith('/settings') && token.adminRole !== 'super_admin') {
      return NextResponse.redirect(new URL('/', req.url));
    }

    // Plans, Subscriptions, Onboarding: admin+ only
    if (
      (path.startsWith('/plans') || path.startsWith('/subscriptions') || path === '/gyms/onboarding') &&
      !['super_admin', 'admin'].includes(token.adminRole)
    ) {
      return NextResponse.redirect(new URL('/', req.url));
    }

    // Content: admin+ only
    if (path.startsWith('/content') && !['super_admin', 'admin'].includes(token.adminRole)) {
      return NextResponse.redirect(new URL('/', req.url));
    }

    // Analyst can only see analytics
    if (
      token.adminRole === 'analyst' &&
      !path.startsWith('/analytics') &&
      path !== '/'
    ) {
      return NextResponse.redirect(new URL('/analytics', req.url));
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
