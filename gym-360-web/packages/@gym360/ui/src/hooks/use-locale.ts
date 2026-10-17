'use client';

import { useCallback, useEffect, useState } from 'react';
import {
  DEFAULT_LOCALE,
  LOCALE_COOKIE,
  directionFor,
  isLocale,
  writeLocaleCookie,
  type Direction,
  type Locale,
} from '../lib/locale';
import { readChangedCookie } from '../lib/client-cookie';

/**
 * Client hook for reading and updating the app locale.
 *
 * Reads the cookie on mount (SSR rendered `<html lang dir>` already matched it),
 * then `setLocale(next)` updates the cookie, reflects the change on `<html>`,
 * and returns so callers can optionally `router.refresh()` to re-run server
 * components with the new locale.
 */
export function useLocale(initial: Locale = DEFAULT_LOCALE): {
  locale: Locale;
  direction: Direction;
  setLocale: (next: Locale) => void;
  toggleLocale: () => void;
} {
  const [locale, setLocaleState] = useState<Locale>(initial);

  useEffect(() => {
    const value = readChangedCookie(LOCALE_COOKIE, isLocale, locale);
    if (!value) return;
    setLocaleState(value);
    applyDocumentDirection(value);
    // run once on mount
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const setLocale = useCallback((next: Locale) => {
    writeLocaleCookie(next);
    applyDocumentDirection(next);
    setLocaleState(next);
  }, []);

  const toggleLocale = useCallback(() => {
    setLocale(locale === 'ar' ? 'en' : 'ar');
  }, [locale, setLocale]);

  return {
    locale,
    direction: directionFor(locale),
    setLocale,
    toggleLocale,
  };
}

function applyDocumentDirection(locale: Locale): void {
  if (typeof document === 'undefined') return;
  const html = document.documentElement;
  html.lang = locale;
  html.dir = directionFor(locale);
}
