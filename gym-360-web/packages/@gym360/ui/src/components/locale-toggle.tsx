'use client';

import * as React from 'react';
import { useLocale } from '../hooks/use-locale';
import { cn } from '../lib/utils';

interface LocaleToggleProps {
  className?: string;
  /** "button" = single icon-like toggle; "segment" = "AR | EN" segmented control */
  variant?: 'button' | 'segment';
  /** Called after the cookie + html dir are updated. Pass `router.refresh` to
   * re-render server components with the new locale. */
  onAfterChange?: () => void;
}

type LocaleCode = 'ar' | 'en';

const otherLocale = (locale: LocaleCode): LocaleCode => (locale === 'ar' ? 'en' : 'ar');

/** Caller-supplied refresh, else a full reload so server components pick up the new locale. */
function refreshAfterLocaleChange(onAfterChange: (() => void) | undefined) {
  if (onAfterChange) {
    onAfterChange();
  } else if (typeof window !== 'undefined') {
    window.location.reload();
  }
}

function LocaleButton({ locale, onChange, className }: { locale: LocaleCode; onChange: (next: LocaleCode) => void; className?: string }) {
  const isArabic = locale === 'ar';
  return (
    <button
      type="button"
      onClick={() => onChange(otherLocale(locale))}
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full border border-border bg-background px-3 py-1.5 text-xs font-semibold uppercase tracking-wide text-foreground transition-colors hover:bg-accent hover:text-accent-foreground',
        className,
      )}
      aria-label={isArabic ? 'Switch to English' : 'التبديل إلى العربية'}
    >
      <span aria-hidden>🌐</span>
      {isArabic ? 'EN' : 'AR'}
    </button>
  );
}

/**
 * Language toggle that writes the locale cookie, flips `<html dir>`, and
 * optionally triggers a caller-supplied refresh so server components re-render
 * with the new locale.
 */
export function LocaleToggle({
  className,
  variant = 'segment',
  onAfterChange,
}: LocaleToggleProps) {
  const { locale, setLocale } = useLocale();

  const onChange = React.useCallback(
    (next: LocaleCode) => {
      if (next === locale) return;
      setLocale(next);
      refreshAfterLocaleChange(onAfterChange);
    },
    [locale, setLocale, onAfterChange],
  );

  if (variant === 'button') {
    return <LocaleButton locale={locale} onChange={onChange} className={className} />;
  }

  return (
    <div
      role="group"
      className={cn(
        'inline-flex items-center rounded-full border border-border bg-background p-0.5 text-xs font-semibold',
        className,
      )}
    >
      {(['ar', 'en'] as const).map((code) => {
        const active = locale === code;
        return (
          <button
            key={code}
            type="button"
            onClick={() => onChange(code)}
            aria-pressed={active}
            className={cn(
              'rounded-full px-2.5 py-1 transition-colors',
              active
                ? 'bg-primary text-primary-foreground shadow-sm'
                : 'text-muted-foreground hover:text-foreground',
            )}
          >
            {code === 'ar' ? 'AR' : 'EN'}
          </button>
        );
      })}
    </div>
  );
}
