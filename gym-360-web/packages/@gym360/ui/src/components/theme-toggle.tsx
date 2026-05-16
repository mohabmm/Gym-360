'use client';

import * as React from 'react';
import { Monitor, Moon, Sun } from 'lucide-react';
import { useTheme } from '../hooks/use-theme';
import type { ThemePref } from '../lib/theme';
import { cn } from '../lib/utils';

interface ThemeToggleProps {
  className?: string;
  /** "button" cycles light→dark→system; "segment" exposes all three */
  variant?: 'button' | 'segment';
}

const ORDER: ThemePref[] = ['light', 'dark', 'system'];
const ICONS: Record<ThemePref, React.ComponentType<{ className?: string }>> = {
  light: Sun,
  dark: Moon,
  system: Monitor,
};
const LABELS: Record<ThemePref, string> = {
  light: 'Light',
  dark: 'Dark',
  system: 'System',
};

export function ThemeToggle({ className, variant = 'button' }: ThemeToggleProps) {
  const { theme, setTheme } = useTheme();
  const Icon = ICONS[theme];

  if (variant === 'segment') {
    return (
      <div
        role="group"
        className={cn(
          'inline-flex items-center rounded-full border border-border bg-background p-0.5 text-xs font-semibold',
          className,
        )}
      >
        {ORDER.map((t) => {
          const TIcon = ICONS[t];
          const active = theme === t;
          return (
            <button
              key={t}
              type="button"
              onClick={() => setTheme(t)}
              aria-pressed={active}
              aria-label={LABELS[t]}
              className={cn(
                'inline-flex items-center gap-1 rounded-full px-2.5 py-1 transition-colors',
                active
                  ? 'bg-primary text-primary-foreground shadow-sm'
                  : 'text-muted-foreground hover:text-foreground',
              )}
            >
              <TIcon className="h-3.5 w-3.5" />
              <span className="hidden sm:inline">{LABELS[t]}</span>
            </button>
          );
        })}
      </div>
    );
  }

  const next = ORDER[(ORDER.indexOf(theme) + 1) % ORDER.length]!;
  return (
    <button
      type="button"
      onClick={() => setTheme(next)}
      className={cn(
        'inline-flex h-9 w-9 items-center justify-center rounded-full border border-border bg-background text-foreground transition-colors hover:bg-accent hover:text-accent-foreground',
        className,
      )}
      aria-label={`Theme: ${LABELS[theme]} (click for ${LABELS[next]})`}
      title={`Theme: ${LABELS[theme]}`}
    >
      <Icon className="h-4 w-4" />
    </button>
  );
}
