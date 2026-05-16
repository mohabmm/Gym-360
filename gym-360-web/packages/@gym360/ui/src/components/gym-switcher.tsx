'use client';

import * as React from 'react';
import { Building2, Check, ChevronDown } from 'lucide-react';
import { cn } from '../lib/utils';

export interface GymOption {
  id: string;
  name: string;
  nameAr?: string;
  role?: string;
  city?: string;
  suspended?: boolean;
}

interface GymSwitcherProps {
  gyms: GymOption[];
  currentGymId: string;
  onSwitch: (gymId: string) => void;
  className?: string;
  compact?: boolean;
}

/**
 * Gym switcher dropdown. Trainers working at multiple gyms (Q3) and
 * multi-location owners can switch the active X-Gym-Id context.
 * When only one gym is available, renders a read-only badge.
 */
export function GymSwitcher({ gyms, currentGymId, onSwitch, className, compact }: GymSwitcherProps) {
  const [open, setOpen] = React.useState(false);
  const ref = React.useRef<HTMLDivElement>(null);
  const current = gyms.find((g) => g.id === currentGymId) ?? gyms[0];

  React.useEffect(() => {
    if (!open) return;
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [open]);

  if (!current) return null;

  const singleGym = gyms.length <= 1;

  if (singleGym) {
    return (
      <div
        className={cn(
          'inline-flex items-center gap-2 rounded-full border border-border bg-background px-3 py-1.5 text-xs font-medium',
          className,
        )}
        title={current.name}
      >
        <Building2 className="h-3.5 w-3.5 text-muted-foreground" />
        {!compact && <span className="max-w-[120px] truncate">{current.name}</span>}
      </div>
    );
  }

  return (
    <div ref={ref} className={cn('relative', className)}>
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        aria-haspopup="listbox"
        aria-expanded={open}
        className={cn(
          'inline-flex items-center gap-2 rounded-full border border-border bg-background px-3 py-1.5 text-xs font-medium transition-colors hover:bg-accent',
          open && 'bg-accent',
        )}
      >
        <Building2 className="h-3.5 w-3.5 text-primary" />
        {!compact && (
          <>
            <span className="max-w-[140px] truncate">{current.name}</span>
            {current.role && (
              <span className="hidden sm:inline rounded-full bg-primary/10 px-1.5 py-0.5 text-[10px] font-semibold text-primary">
                {current.role}
              </span>
            )}
          </>
        )}
        <ChevronDown className={cn('h-3.5 w-3.5 transition-transform', open && 'rotate-180')} />
      </button>

      {open && (
        <div
          role="listbox"
          className="absolute right-0 top-full z-50 mt-2 w-72 overflow-hidden rounded-xl border border-border bg-popover text-popover-foreground shadow-lg"
        >
          <div className="border-b border-border px-3 py-2 text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">
            Switch gym ({gyms.length})
          </div>
          <ul className="max-h-80 overflow-y-auto py-1">
            {gyms.map((g) => {
              const active = g.id === currentGymId;
              return (
                <li key={g.id}>
                  <button
                    type="button"
                    role="option"
                    aria-selected={active}
                    onClick={() => {
                      if (!active) onSwitch(g.id);
                      setOpen(false);
                    }}
                    className={cn(
                      'flex w-full items-start gap-2.5 px-3 py-2.5 text-left text-sm transition-colors hover:bg-accent',
                      active && 'bg-primary/5',
                    )}
                  >
                    <div
                      className={cn(
                        'mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-lg text-[10px] font-bold',
                        active ? 'bg-primary text-primary-foreground' : 'bg-muted text-muted-foreground',
                      )}
                    >
                      {g.name.split(' ').map((n) => n[0]).join('').slice(0, 2).toUpperCase()}
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <p className="truncate font-medium">{g.name}</p>
                        {g.suspended && (
                          <span className="rounded-full bg-red-100 px-1.5 py-0.5 text-[9px] font-semibold text-red-700">
                            suspended
                          </span>
                        )}
                      </div>
                      {g.nameAr && <p className="truncate text-xs text-muted-foreground" dir="rtl">{g.nameAr}</p>}
                      <div className="mt-0.5 flex items-center gap-2 text-xs text-muted-foreground">
                        {g.role && <span className="font-medium text-primary">{g.role}</span>}
                        {g.city && <span>· {g.city}</span>}
                      </div>
                    </div>
                    {active && <Check className="h-4 w-4 shrink-0 text-primary" />}
                  </button>
                </li>
              );
            })}
          </ul>
        </div>
      )}
    </div>
  );
}
