// Pages (and their sub-pages) a receptionist may open.
const RECEPTIONIST_PATHS = ['/', '/members', '/check-ins', '/billing', '/pt/trials', '/pt/review', '/pt/interests'];

// Pages a receptionist may open exactly, but not their sub-pages: /trainers lets the
// desk add a coach; trainer detail pages (edit profile, rates) stay owner/manager.
const RECEPTIONIST_EXACT_PATHS = ['/trainers'];

export const isReceptionistPathAllowed = (path: string) =>
  RECEPTIONIST_EXACT_PATHS.includes(path) ||
  RECEPTIONIST_PATHS.some((p) => path === p || path.startsWith(p + '/'));
