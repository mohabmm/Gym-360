import { withAuth } from 'next-auth/middleware';
import { NextResponse } from 'next/server';

export default withAuth(
  function middleware(req) {
    const token = req.nextauth.token;
    const path = req.nextUrl.pathname;

    // Must be gym_staff or gym_owner
    if (!token?.role || !['gym_staff', 'gym_owner'].includes(token.role)) {
      return NextResponse.redirect(new URL('/login', req.url));
    }

    // Hard-mode multi-tenancy: any Owner without an active gymId goes to
    // /select-gym (the page handles the zero-owned-gyms case with a sign-out CTA).
    const hasGymId = !!token.gymId;
    if (
      token.role === 'gym_owner' &&
      !hasGymId &&
      path !== '/select-gym'
    ) {
      return NextResponse.redirect(new URL('/select-gym', req.url));
    }

    // Receptionist restrictions
    if (token.staffSubRole === 'receptionist') {
      const allowed = ['/', '/members', '/check-ins', '/billing'];
      const isAllowed = allowed.some((p) => path === p || path.startsWith(p + '/'));
      if (!isAllowed) {
        return NextResponse.redirect(new URL('/', req.url));
      }
    }

    // Billing settings: owner only
    if (path === '/settings' && token.staffSubRole !== 'owner') {
      // Managers can see settings but not billing
    }

    return NextResponse.next();
  },
  {
    callbacks: {
      authorized: ({ token }) => !!token,
    },
    pages: {
      signIn: '/login',
    },
  }
);

export const config = {
  matcher: ['/((?!login|select-gym|api|_next/static|_next/image|favicon.ico|logo\\.svg|.*\\.(?:png|jpg|jpeg|gif|svg|ico|webp|avif)).*)'],
};
