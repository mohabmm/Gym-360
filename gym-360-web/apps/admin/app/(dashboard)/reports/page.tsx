'use client';

import { useMemo } from 'react';
import { PageHeader, StatusBadge, formatCurrency, PageSkeleton } from '@gym360/ui';
import { Download } from 'lucide-react';
import { useGyms } from '@/lib/hooks';

export default function ReportsPage() {
  const { data, isLoading } = useGyms({ pageSize: 100 });

  const topGyms = useMemo(() => {
    const all = data?.data ?? [];
    return [...all]
      .filter((g) => g.status !== 'terminated')
      .sort((a, b) => (b.monthlyRevenue ?? 0) - (a.monthlyRevenue ?? 0))
      .slice(0, 15);
  }, [data]);

  const exportCsv = () => {
    const rows = topGyms.map((g) => [
      g.name,
      g.city ?? '',
      g.planName ?? '',
      g.activeMemberCount ?? 0,
      g.activeTrainerCount ?? 0,
      g.monthlyRevenue ?? 0,
      g.paymentStatus ?? '',
      g.status ?? '',
    ]);
    const csv = [
      ['Gym', 'City', 'Plan', 'Members', 'Trainers', 'Monthly Revenue (EGP)', 'Payment', 'Status'],
      ...rows,
    ]
      .map((r) => r.map((c) => `"${String(c).replace(/"/g, '""')}"`).join(','))
      .join('\n');
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `gym-report-${new Date().toISOString().slice(0, 10)}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  };

  if (isLoading) return <PageSkeleton />;

  return (
    <div className="space-y-6">
      <PageHeader
        title="Gym Reports"
        breadcrumbs={[{ label: 'Dashboard', href: '/' }, { label: 'Reports' }]}
        actions={
          <button
            onClick={exportCsv}
            disabled={topGyms.length === 0}
            className="inline-flex items-center gap-1.5 h-9 px-3 rounded-lg border border-input bg-background text-sm font-medium hover:bg-muted transition-colors disabled:opacity-50"
          >
            <Download className="h-4 w-4" /> Export CSV
          </button>
        }
      />

      <div className="rounded-xl border bg-card p-5 shadow-sm">
        <h3 className="font-semibold text-sm mb-4">Gym Performance Report</h3>
        {topGyms.length === 0 ? (
          <p className="text-sm text-muted-foreground py-8 text-center">No gyms to report on yet.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-left text-muted-foreground">
                  <th className="pb-2 font-medium">#</th>
                  <th className="pb-2 font-medium">Gym</th>
                  <th className="pb-2 font-medium">City</th>
                  <th className="pb-2 font-medium">Plan</th>
                  <th className="pb-2 font-medium text-right">Members</th>
                  <th className="pb-2 font-medium text-right">Trainers</th>
                  <th className="pb-2 font-medium text-right">Monthly Revenue</th>
                  <th className="pb-2 font-medium">Payment</th>
                  <th className="pb-2 font-medium">Status</th>
                </tr>
              </thead>
              <tbody>
                {topGyms.map((g, i) => (
                  <tr key={g.id} className="border-b border-border/50 hover:bg-muted/30 transition-colors">
                    <td className="py-2.5 text-muted-foreground">{i + 1}</td>
                    <td className="py-2.5">
                      <p className="font-medium">{g.name}</p>
                    </td>
                    <td className="py-2.5 text-muted-foreground">{g.city ?? '—'}</td>
                    <td className="py-2.5">
                      <StatusBadge variant={(g.planName ?? 'inactive').toLowerCase()} label={g.planName || 'No Plan'} />
                    </td>
                    <td className="py-2.5 text-right font-medium">{g.activeMemberCount ?? 0}</td>
                    <td className="py-2.5 text-right font-medium">{g.activeTrainerCount ?? 0}</td>
                    <td className="py-2.5 text-right font-medium">{formatCurrency(g.monthlyRevenue ?? 0)}</td>
                    <td className="py-2.5">
                      <StatusBadge variant={g.paymentStatus} dot />
                    </td>
                    <td className="py-2.5">
                      <StatusBadge variant={g.status} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
