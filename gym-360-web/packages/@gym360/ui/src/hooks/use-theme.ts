'use client';

import { useCallback, useEffect, useState } from 'react';
import {
  DEFAULT_THEME,
  THEME_COOKIE,
  isThemePref,
  writeThemeCookie,
  type ResolvedTheme,
  type ThemePref,
} from '../lib/theme';
import { readChangedCookie } from '../lib/client-cookie';

function resolve(pref: ThemePref): ResolvedTheme {
  if (pref !== 'system') return pref;
  if (typeof window === 'undefined') return 'light';
  return window.matchMedia('(prefers-color-scheme: dark)').matches
    ? 'dark'
    : 'light';
}

function apply(resolved: ResolvedTheme): void {
  if (typeof document === 'undefined') return;
  document.documentElement.classList.toggle('dark', resolved === 'dark');
}

/**
 * Client hook for reading and updating the theme preference.
 *
 * - `theme` is the raw preference ("light" | "dark" | "system").
 * - `resolvedTheme` is what's actually applied ("light" | "dark").
 * - `setTheme` persists the cookie and applies immediately.
 * - Listens for OS-level changes when preference is "system".
 */
export function useTheme(initial: ThemePref = DEFAULT_THEME): {
  theme: ThemePref;
  resolvedTheme: ResolvedTheme;
  setTheme: (next: ThemePref) => void;
} {
  const [theme, setThemeState] = useState<ThemePref>(initial);
  const [resolvedTheme, setResolved] = useState<ResolvedTheme>(() =>
    resolve(initial),
  );

  useEffect(() => {
    const value = readChangedCookie(THEME_COOKIE, isThemePref, theme);
    if (!value) return;
    setThemeState(value);
    const r = resolve(value);
    setResolved(r);
    apply(r);
    // run once on mount
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (theme !== 'system' || typeof window === 'undefined') return;
    const mql = window.matchMedia('(prefers-color-scheme: dark)');
    const onChange = () => {
      const r: ResolvedTheme = mql.matches ? 'dark' : 'light';
      setResolved(r);
      apply(r);
    };
    mql.addEventListener('change', onChange);
    return () => mql.removeEventListener('change', onChange);
  }, [theme]);

  const setTheme = useCallback((next: ThemePref) => {
    writeThemeCookie(next);
    setThemeState(next);
    const r = resolve(next);
    setResolved(r);
    apply(r);
  }, []);

  return { theme, resolvedTheme, setTheme };
}
