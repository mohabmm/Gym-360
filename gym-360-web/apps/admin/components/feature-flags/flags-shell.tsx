'use client';

import { useCallback, useState, type ReactNode } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { Clock } from 'lucide-react';
import { ConfirmModal, PageHeader, cn } from '@gym360/ui';
import { useFlagsText } from '@/lib/feature-flags-text';

export const FLAGS_BASE = '/feature-flags';

/** Tabs + "how long changes take" note shared by every feature-flags page. */
export function FlagsShell({ title, description, actions, children }: {
  title: string;
  description: string;
  actions?: ReactNode;
  children: ReactNode;
}) {
  const { t } = useFlagsText();
  const pathname = usePathname();
  const tabs = [
    { href: FLAGS_BASE, label: t.tabGyms, active: pathname === FLAGS_BASE || /\/feature-flags\/gyms\//.test(pathname) },
    { href: `${FLAGS_BASE}/presets`, label: t.tabPresets, active: pathname.startsWith(`${FLAGS_BASE}/presets`) },
    { href: `${FLAGS_BASE}/audit`, label: t.tabAudit, active: pathname.startsWith(`${FLAGS_BASE}/audit`) },
  ];

  return (
    <div className="space-y-5">
      <PageHeader title={title} description={description} actions={actions} />
      <nav className="flex gap-1 border-b border-border">
        {tabs.map((tab) => (
          <Link
            key={tab.href}
            href={tab.href}
            className={cn(
              '-mb-px border-b-2 px-4 py-2 text-sm font-medium transition-colors',
              tab.active ? 'border-primary text-foreground' : 'border-transparent text-muted-foreground hover:text-foreground',
            )}
          >
            {tab.label}
          </Link>
        ))}
      </nav>
      <PropagationNote />
      {children}
    </div>
  );
}

export function PropagationNote() {
  const { t } = useFlagsText();
  return (
    <div className="flex items-start gap-2 rounded-lg border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-800 dark:border-blue-900 dark:bg-blue-950/40 dark:text-blue-200">
      <Clock className="mt-0.5 h-4 w-4 shrink-0" />
      <p>{t.propagationNote}</p>
    </div>
  );
}

export function FlagToast({ message, error }: { message: string | null; error?: boolean }) {
  if (!message) return null;
  return (
    <div
      role="status"
      className={cn(
        'fixed bottom-6 end-6 z-50 max-w-md rounded-lg px-4 py-3 text-sm text-white shadow-lg',
        error ? 'bg-red-600' : 'bg-emerald-600',
      )}
    >
      {message}
    </div>
  );
}

/** Success / error toast that hides itself. */
export function useFlash() {
  const [toast, setToast] = useState<{ message: string; error: boolean } | null>(null);
  const flash = useCallback((message: string, error = false) => {
    setToast({ message, error });
    window.setTimeout(() => setToast(null), 5000);
  }, []);
  return { toast, flash };
}

interface PendingAction {
  title: string;
  description: string;
  run: () => Promise<unknown>;
}

/**
 * Every save goes through a confirmation dialog, then shows a success (or error) toast.
 * `ask(...)` opens the dialog; `dialog` is rendered once by the page.
 */
export function useConfirmedAction() {
  const { t } = useFlagsText();
  const { toast, flash } = useFlash();
  const [pending, setPending] = useState<PendingAction | null>(null);
  const [busy, setBusy] = useState(false);

  const confirm = async () => {
    if (!pending) return;
    setBusy(true);
    try {
      await pending.run();
      flash(t.savedPropagation);
    } catch {
      flash(t.error, true);
    } finally {
      setBusy(false);
      setPending(null);
    }
  };

  const dialog = (
    <>
      <ConfirmModal
        open={!!pending}
        onClose={() => setPending(null)}
        onConfirm={() => void confirm()}
        title={pending?.title ?? ''}
        description={pending?.description ?? ''}
        confirmLabel={t.confirm}
        cancelLabel={t.cancel}
        variant="warning"
        isLoading={busy}
      />
      <FlagToast message={toast?.message ?? null} error={toast?.error} />
    </>
  );

  return { ask: setPending, dialog };
}

export function OnOffBadge({ on }: { on: boolean }) {
  const { t } = useFlagsText();
  return (
    <span
      className={cn(
        'inline-flex rounded-full px-2.5 py-0.5 text-xs font-semibold',
        on
          ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400'
          : 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400',
      )}
    >
      {on ? t.on : t.off}
    </span>
  );
}

export function SourceBadge({ source }: { source: 'preset' | 'override' }) {
  const { t } = useFlagsText();
  return (
    <span
      className={cn(
        'inline-flex rounded-full px-2.5 py-0.5 text-xs font-semibold',
        source === 'override'
          ? 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400'
          : 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
      )}
    >
      {source === 'override' ? t.sourceOverride : t.sourcePreset}
    </span>
  );
}

export function Toggle({ checked, onChange, disabled, label }: {
  checked: boolean;
  onChange: (next: boolean) => void;
  disabled?: boolean;
  label: string;
}) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={label}
      disabled={disabled}
      onClick={() => onChange(!checked)}
      className={cn(
        'relative inline-flex h-6 w-11 shrink-0 items-center rounded-full transition-colors disabled:opacity-50',
        checked ? 'bg-emerald-600' : 'bg-gray-300 dark:bg-gray-600',
      )}
    >
      <span
        className={cn(
          'inline-block h-5 w-5 rounded-full bg-white shadow transition-transform',
          checked ? 'translate-x-5 rtl:-translate-x-5' : 'translate-x-0.5 rtl:-translate-x-0.5',
        )}
      />
    </button>
  );
}
