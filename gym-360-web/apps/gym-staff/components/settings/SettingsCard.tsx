import type { ReactNode } from 'react';

export const INPUT_CLASS =
  'w-full h-10 px-3 rounded-xl border border-border bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/30';

/** A settings section: bordered card with an optional standard heading. */
export function SettingsCard({ title, children }: { title?: string; children: ReactNode }) {
  return (
    <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
      {title && <h3 className="font-heading font-semibold text-base mb-4">{title}</h3>}
      {children}
    </div>
  );
}

/** A labelled form control. */
export function Field({ label, className, children }: { label: string; className?: string; children: ReactNode }) {
  return (
    <div className={className}>
      <label className="block text-sm font-medium mb-1.5">{label}</label>
      {children}
    </div>
  );
}
