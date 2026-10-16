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

/** Closes the dropdown on any mousedown outside `ref` while it is open. */
function useCloseOnOutsideClick(ref: React.RefObject<HTMLDivElement>, open: boolean, close: () => void) {
  React.useEffect(() => {
    if (!open) return;
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) close();
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);
}

function gymInitials(name: string): string {
  return name.split(' ').map((n) => n[0]).join('').slice(0, 2).toUpperCase();
}

function SingleGymBadge({ gym, compact, className }: { gym: GymOption; compact?: boolean; className?: string }) {
  return (
    <div
      className={cn(
        'inline-flex items-center gap-2 rounded-full border border-border bg-background px-3 py-1.5 text-xs font-medium',
        className,
      )}
      title={gym.name}
    >
      <Building2 className="h-3.5 w-3.5 text-muted-foreground" />
      {!compact && <span className="max-w-[120px] truncate">{gym.name}</span>}
    </div>
  );
}

function TriggerLabel({ gym }: { gym: GymOption }) {
  return (
    <>
      <span className="max-w-[140px] truncate">{gym.name}</span>
      {gym.role && (
        <span className="hidden sm:inline rounded-full bg-primary/10 px-1.5 py-0.5 text-[10px] font-semibold text-primary">
          {gym.role}
        </span>
      )}
    </>
  );
}

function GymOptionDetails({ gym }: { gym: GymOption }) {
  return (
    <div className="flex-1 min-w-0">
      <div className="flex items-center gap-2">
        <p className="truncate font-medium">{gym.name}</p>
        {gym.suspended && (
          <span className="rounded-full bg-red-100 px-1.5 py-0.5 text-[9px] font-semibold text-red-700">
            suspended
          </span>
        )}
      </div>
      {gym.nameAr && <p className="truncate text-xs text-muted-foreground" dir="rtl">{gym.nameAr}</p>}
      <div className="mt-0.5 flex items-center gap-2 text-xs text-muted-foreground">
        {gym.role && <span className="font-medium text-primary">{gym.role}</span>}
        {gym.city && <span>· {gym.city}</span>}
      </div>
    </div>
  );
}

function GymOptionRow({ gym, active, onSelect }: { gym: GymOption; active: boolean; onSelect: () => void }) {
  return (
    <li>
      <button
        type="button"
        role="option"
        aria-selected={active}
        onClick={onSelect}
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
          {gymInitials(gym.name)}
        </div>
        <GymOptionDetails gym={gym} />
        {active && <Check className="h-4 w-4 shrink-0 text-primary" />}
      </button>
    </li>
  );
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
  useCloseOnOutsideClick(ref, open, () => setOpen(false));

  if (!current) return null;

  if (gyms.length <= 1) {
    return <SingleGymBadge gym={current} compact={compact} className={className} />;
  }

  const select = (gymId: string) => {
    if (gymId !== currentGymId) onSwitch(gymId);
    setOpen(false);
  };

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
        {!compact && <TriggerLabel gym={current} />}
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
            {gyms.map((g) => (
              <GymOptionRow key={g.id} gym={g} active={g.id === currentGymId} onSelect={() => select(g.id)} />
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
