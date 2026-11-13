'use client';

import { useEffect, useState } from 'react';
import { DEFAULT_LOCALE, isLocale, type Locale } from '../lib/locale';

/**
 * Current locale from <html lang>, kept in sync when the header LocaleToggle switches it
 * (the toggle rewrites lang/dir on <html> without remounting client components).
 */
export function useDocumentLocale(): Locale {
  const [locale, setLocale] = useState<Locale>(DEFAULT_LOCALE);

  useEffect(() => {
    const read = (): Locale => {
      const lang = document.documentElement.lang;
      return isLocale(lang) ? lang : DEFAULT_LOCALE;
    };
    setLocale(read());
    const observer = new MutationObserver(() => setLocale(read()));
    observer.observe(document.documentElement, { attributes: true, attributeFilter: ['lang'] });
    return () => observer.disconnect();
  }, []);

  return locale;
}
