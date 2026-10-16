import { StatusBadge, formatCurrency, formatDate } from '@gym360/ui';
import { humanize, paymentDate, type PaymentRow } from '@/lib/gym-revenue';

type Props = {
  isLoading: boolean;
  recent: PaymentRow[];
  totalCount: number;
};

const HEADERS: { label: string; align: 'text-left' | 'text-right' }[] = [
  { label: 'Receipt', align: 'text-left' },
  { label: 'Member', align: 'text-left' },
  { label: 'Purpose', align: 'text-left' },
  { label: 'Method', align: 'text-left' },
  { label: 'Amount', align: 'text-right' },
  { label: 'Status', align: 'text-left' },
  { label: 'Date', align: 'text-left' },
];

function PaymentRowView({ p }: { p: PaymentRow }) {
  return (
    <tr className="border-b last:border-0 hover:bg-muted/40">
      <td className="py-2.5 px-4 font-mono text-xs">{p.receiptNumber}</td>
      <td className="py-2.5 px-4 font-medium">{p.memberName ?? '—'}</td>
      <td className="py-2.5 px-4 capitalize">{humanize(p.purpose)}</td>
      <td className="py-2.5 px-4 capitalize">{humanize(p.paymentMethod)}</td>
      <td className="py-2.5 px-4 text-right font-medium">{formatCurrency(p.amount)}</td>
      <td className="py-2.5 px-4">
        <StatusBadge variant={p.status.toLowerCase() as 'completed' | 'voided'} label={p.status} />
      </td>
      <td className="py-2.5 px-4 text-muted-foreground">{formatDate(paymentDate(p))}</td>
    </tr>
  );
}

function PaymentsBody({ isLoading, recent }: Omit<Props, 'totalCount'>) {
  if (isLoading) {
    return <div className="p-10 text-center text-sm text-muted-foreground">Loading…</div>;
  }
  if (recent.length === 0) {
    return (
      <div className="p-10 text-center text-sm text-muted-foreground">
        No payments recorded in this range.
      </div>
    );
  }
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b bg-muted/50 text-xs uppercase tracking-wide text-muted-foreground">
            {HEADERS.map((h) => (
              <th key={h.label} className={`${h.align} py-2.5 px-4 font-semibold`}>
                {h.label}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {recent.map((p) => (
            <PaymentRowView key={p.id} p={p} />
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function RecentPaymentsTable({ isLoading, recent, totalCount }: Props) {
  return (
    <div className="rounded-xl border bg-card shadow-sm overflow-hidden">
      <div className="flex items-center justify-between px-6 py-4 border-b">
        <div>
          <h4 className="text-sm font-semibold">Recent Payments</h4>
          <p className="text-xs text-muted-foreground mt-0.5">
            Last 15 of {totalCount} in selected range
          </p>
        </div>
      </div>
      <PaymentsBody isLoading={isLoading} recent={recent} />
    </div>
  );
}
