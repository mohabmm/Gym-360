/**
 * Shared color-theme helpers used by both Next.js apps.
 *
 * Strategy mirrors locale handling:
 *   - Store theme preference ("light" | "dark" | "system") in a cookie.
 *   - Server components read it in `app/layout.tsx` to set the initial
 *     `class="dark"` on `<html>`, avoiding a flash of the wrong theme.
 *   - A small inline script resolves "system" against the OS preference
 *     on the client before React hydration.
 */

export const THEME_COOKIE = 'gymapp_theme';
export const DEFAULT_THEME: ThemePref = 'system';

export type ThemePref = 'light' | 'dark' | 'system';
export type ResolvedTheme = 'light' | 'dark';

export function isThemePref(value: unknown): value is ThemePref {
  return value === 'light' || value === 'dark' || value === 'system';
}

/** Read preference from a cookie string (returns "system" if unset). */
export function readThemeFromCookie(
  cookieHeader: string | null | undefined,
): ThemePref {
  if (!cookieHeader) return DEFAULT_THEME;
  const match = cookieHeader
    .split(';')
    .map((c) => c.trim())
    .find((c) => c.startsWith(`${THEME_COOKIE}=`));
  if (!match) return DEFAULT_THEME;
  const value = decodeURIComponent(match.split('=')[1] ?? '');
  return isThemePref(value) ? value : DEFAULT_THEME;
}

/**
 * Initial `class="dark"` the server should emit on `<html>`.
 * For "system", emit neither — the inline script resolves it before paint.
 */
export function initialHtmlClass(pref: ThemePref): string | undefined {
  if (pref === 'dark') return 'dark';
  if (pref === 'light') return undefined;
  return undefined;
}

export function writeThemeCookie(pref: ThemePref): void {
  if (typeof document === 'undefined') return;
  const oneYear = 60 * 60 * 24 * 365;
  document.cookie = `${THEME_COOKIE}=${pref}; path=/; max-age=${oneYear}; SameSite=Lax`;
}

/**
 * Inline script that runs before React hydration to resolve "system" into
 * an actual class. Inject via `<Script>` with `strategy="beforeInteractive"`
 * or via a plain `<script dangerouslySetInnerHTML>` in the layout head.
 */
export const THEME_BOOTSTRAP_SCRIPT = `
(function(){try{
  var m=document.cookie.split('; ').find(function(r){return r.indexOf('${THEME_COOKIE}=')===0;});
  var v=m?decodeURIComponent(m.split('=')[1]):'system';
  var dark = v==='dark' || (v==='system' && window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches);
  if(dark){document.documentElement.classList.add('dark');} else {document.documentElement.classList.remove('dark');}
}catch(e){}})();
`;
