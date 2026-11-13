import type { ReactNode } from 'react';

/** Plain table in the dashboard card style; logical (start/end) alignment so RTL reads correctly. */
export function PtTable({ head, children }: { head: ReactNode[]; children: ReactNode }) {
  return (
    <div className="rounded-xl border bg-card shadow-sm overflow-x-auto">
      <table className="w-full text-sm">
        <thead className="border-b bg-muted/40 text-muted-foreground">
          <tr>
            {head.map((h, i) => (
              <th key={i} className="px-4 py-3 text-start font-medium">{h}</th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y">{children}</tbody>
      </table>
    </div>
  );
}

export function PtCell({ children, className = '' }: { children: ReactNode; className?: string }) {
  return <td className={`px-4 py-3 ${className}`}>{children}</td>;
}

export function PtState({ text }: { text: string }) {
  return (
    <div className="rounded-xl border bg-card p-8 text-center shadow-sm">
      <p className="text-sm text-muted-foreground">{text}</p>
    </div>
  );
}
