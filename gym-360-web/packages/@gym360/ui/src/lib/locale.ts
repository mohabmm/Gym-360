/**
 * Shared locale + direction helpers used by both Next.js apps.
 *
 * Strategy:
 *   - Store the user's locale in a cookie (default: "ar").
 *   - Server components read the cookie synchronously in `app/layout.tsx`
 *     to render `<html lang dir>` before hydration.
 *   - Client components toggle the locale by writing the cookie and
 *     refreshing the router — keeps it compatible with static export and
 *     avoids a client/server mismatch flash.
 */

export const LOCALE_COOKIE = 'gymapp_locale';
export const DEFAULT_LOCALE: Locale = 'ar';

export type Locale = 'ar' | 'en';
export type Direction = 'rtl' | 'ltr';

export function isLocale(value: unknown): value is Locale {
  return value === 'ar' || value === 'en';
}

export function directionFor(locale: Locale): Direction {
  return locale === 'ar' ? 'rtl' : 'ltr';
}

/** Parse a cookie header or document.cookie string for the locale. */
export function readLocaleFromCookie(
  cookieHeader: string | null | undefined,
): Locale {
  if (!cookieHeader) return DEFAULT_LOCALE;
  const match = cookieHeader
    .split(';')
    .map((c) => c.trim())
    .find((c) => c.startsWith(`${LOCALE_COOKIE}=`));
  if (!match) return DEFAULT_LOCALE;
  const value = decodeURIComponent(match.split('=')[1] ?? '');
  return isLocale(value) ? value : DEFAULT_LOCALE;
}

/** Write the locale cookie from the browser (client-side only). */
export function writeLocaleCookie(locale: Locale): void {
  if (typeof document === 'undefined') return;
  const oneYear = 60 * 60 * 24 * 365;
  document.cookie = `${LOCALE_COOKIE}=${locale}; path=/; max-age=${oneYear}; SameSite=Lax`;
}
