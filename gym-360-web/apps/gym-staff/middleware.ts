import { withAuth } from 'next-auth/middleware';
import { NextResponse } from 'next/server';

type StaffToken = { role?: string; gymId?: unknown; staffSubRole?: string } | null;

const STAFF_ROLES = ['gym_staff', 'gym_owner'];
const RECEPTIONIST_PATHS = ['/', '/members', '/check-ins', '/billing'];

const isStaffRole = (token: StaffToken) => !!token?.role && STAFF_ROLES.includes(token.role);

// Hard-mode multi-tenancy: any Owner without an active gymId goes to
// /select-gym (the page handles the zero-owned-gyms case with a sign-out CTA).
const ownerNeedsGym = (token: NonNullable<StaffToken>, path: string) =>
  token.role === 'gym_owner' && !token.gymId && path !== '/select-gym';

const isReceptionistPathAllowed = (path: string) =>
  RECEPTIONIST_PATHS.some((p) => path === p || path.startsWith(p + '/'));

// Settings: managers can see settings but not billing (enforced in the page, not here).
function resolveRedirect(token: StaffToken, path: string): string | null {
  // Must be gym_staff or gym_owner
  if (!token || !isStaffRole(token)) return '/login';
  if (ownerNeedsGym(token, path)) return '/select-gym';
  // Receptionist restrictions
  if (token.staffSubRole === 'receptionist' && !isReceptionistPathAllowed(path)) return '/';
  return null;
}

export default withAuth(
  function middleware(req) {
    const target = resolveRedirect(req.nextauth.token as StaffToken, req.nextUrl.pathname);
    return target ? NextResponse.redirect(new URL(target, req.url)) : NextResponse.next();
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
