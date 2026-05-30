'use client';

import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  KpiCard,
  ChartContainer,
  StatusBadge,
  apiGet,
  BILLING,
  formatCurrency,
  formatDate,
} from '@gym360/ui';
import {
  AreaChart,
  Area,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  PieChart,
  Pie,
  Cell,
} from 'recharts';
import { DollarSign, TrendingUp, Receipt, AlertTriangle } from 'lucide-react';

type Props = {
  gymId: string;
};

type PaymentRow = {
  id: string;
  memberId: string;
  memberName?: string | null;
  amount: number;
  currency: string;
  paymentMethod: string;
  purpose: string;
  status: string;
  receiptNumber: string;
  voidReason?: string | null;
  paidAt?: string | null;
  createdAt: string;
};

type PagedPayments = { items: PaymentRow[]; page: number; pageSize: number; total: number };

// BE sends enum names in PascalCase ("SessionPackage", "BankTransfer"); space them for display.
const humanize = (v?: string | null): string =>
  String(v ?? '')
    .replace(/_/g, ' ')
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2');

const METHOD_COLORS: Record<string, string> = {
  cash: '#10B981',
  card: '#3B82F6',
  vodafone_cash: '#EF4444',
  instapay: '#8B5CF6',
  fawry: '#F59E0B',
  bank_transfer: '#6366F1',
};

function toDateOnly(d: Date) {
  return d.toISOString().slice(0, 10);
}

function firstOfMonth(d: Date) {
  return new Date(d.getFullYear(), d.getMonth(), 1);
}

function subMonths(d: Date, n: number) {
  return new Date(d.getFullYear(), d.getMonth() - n, 1);
}

function labelForMonth(d: Date) {
  return d.toLocaleDateString(undefined, { month: 'short' });
}

export function GymRevenuePanel({ gymId }: Props) {
  const [range, setRange] = useState<'6m' | '12m' | 'ytd'>('6m');

  const { from, to } = useMemo(() => {
    const now = new Date();
    const end = toDateOnly(new Date(now.getFullYear(), now.getMonth() + 1, 0));
    const start =
      range === 'ytd'
        ? new Date(now.getFullYear(), 0, 1)
        : subMonths(firstOfMonth(now), range === '6m' ? 5 : 11);
    return { from: toDateOnly(start), to: end };
  }, [range]);

  const { data: paymentsResp, isLoading, isError } = useQuery({
    queryKey: ['gym-payments', gymId, from, to],
    queryFn: async () => {
      const res = await apiGet<PagedPayments | PaymentRow[]>(BILLING.PAYMENTS, {
        gymId,
        from,
        to,
        page: 1,
        pageSize: 500,
      });
      const raw = res.data as unknown;
      const items = Array.isArray(raw)
        ? (raw as PaymentRow[])
        : ((raw as PagedPayments)?.items ?? []);
      return items;
    },
    enabled: !!gymId,
  });

  const payments = paymentsResp ?? [];

  const { trend, totals, byMethod, byPurpose } = useMemo(() => {
    const now = new Date();
    const bucketCount = range === '12m' ? 12 : range === '6m' ? 6 : now.getMonth() + 1;
    const buckets = Array.from({ length: bucketCount }).map((_, i) => {
      const d = subMonths(firstOfMonth(now), bucketCount - 1 - i);
      return { key: `${d.getFullYear()}-${d.getMonth()}`, label: labelForMonth(d), revenue: 0 };
    });
    const bucketMap = new Map(buckets.map((b) => [b.key, b]));

    let completed = 0;
    let voided = 0;
    let voidedCount = 0;
    const methodTotals = new Map<string, number>();
    const purposeTotals = new Map<string, number>();

    for (const p of payments) {
      const when = new Date(p.paidAt ?? p.createdAt);
      const key = `${when.getFullYear()}-${when.getMonth()}`;
      const bucket = bucketMap.get(key);
      const isCompleted = p.status.toLowerCase() === 'completed';
      if (isCompleted) {
        completed += p.amount;
        if (bucket) bucket.revenue += p.amount;
        methodTotals.set(p.paymentMethod, (methodTotals.get(p.paymentMethod) ?? 0) + p.amount);
        purposeTotals.set(p.purpose, (purposeTotals.get(p.purpose) ?? 0) + p.amount);
      } else if (p.status.toLowerCase() === 'voided') {
        voided += p.amount;
        voidedCount++;
      }
    }

    const thisMonthKey = `${now.getFullYear()}-${now.getMonth()}`;
    const prevMonthKey = `${subMonths(now, 1).getFullYear()}-${subMonths(now, 1).getMonth()}`;
    const thisMonth = buckets.find((b) => b.key === thisMonthKey)?.revenue ?? 0;
    const prevMonth = buckets.find((b) => b.key === prevMonthKey)?.revenue ?? 0;
    const momChange = prevMonth > 0 ? ((thisMonth - prevMonth) / prevMonth) * 100 : 0;

    return {
      trend: buckets,
      totals: {
        totalRevenue: completed,
        voidedRevenue: voided,
        voidedCount,
        thisMonth,
        prevMonth,
        momChange,
        completedCount: payments.filter((p) => p.status.toLowerCase() === 'completed').length,
      },
      byMethod: Array.from(methodTotals, ([name, value]) => ({ name, value })).sort((a, b) => b.value - a.value),
      byPurpose: Array.from(purposeTotals, ([name, value]) => ({ name, value })).sort((a, b) => b.value - a.value),
    };
  }, [payments, range]);

  const recent = useMemo(
    () =>
      [...payments]
        .sort(
          (a, b) =>
            new Date(b.paidAt ?? b.createdAt).getTime() -
            new Date(a.paidAt ?? a.createdAt).getTime(),
        )
        .slice(0, 15),
    [payments],
  );

  if (isError) {
    return (
      <div className="rounded-xl border bg-card p-10 text-center">
        <AlertTriangle className="mx-auto h-8 w-8 text-destructive" />
        <p className="mt-3 text-sm text-muted-foreground">Failed to load revenue data.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div>
          <h3 className="text-base font-semibold">Revenue</h3>
          <p className="text-xs text-muted-foreground">
            {isLoading ? 'Loading payments…' : `${payments.length} payments · ${from} → ${to}`}
          </p>
        </div>
        <div className="inline-flex items-center rounded-lg border bg-card p-0.5 text-xs">
          {(['6m', '12m', 'ytd'] as const).map((r) => (
            <button
              key={r}
              onClick={() => setRange(r)}
              className={`px-3 py-1.5 rounded-md font-medium transition-colors ${
                range === r ? 'bg-primary text-white' : 'text-muted-foreground hover:text-foreground'
              }`}
            >
              {r === 'ytd' ? 'YTD' : r.toUpperCase()}
            </button>
          ))}
        </div>
      </div>

      {/* KPIs */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <KpiCard
          value={formatCurrency(totals.thisMonth)}
          label="This Month"
          change={totals.momChange ? Math.round(totals.momChange * 10) / 10 : undefined}
          icon={<DollarSign className="h-5 w-5" />}
          variant="success"
        />
        <KpiCard
          value={formatCurrency(totals.totalRevenue)}
          label={range === 'ytd' ? 'YTD Revenue' : `Last ${trend.length} Months`}
          icon={<TrendingUp className="h-5 w-5" />}
          variant="info"
        />
        <KpiCard
          value={totals.completedCount.toString()}
          label="Completed Payments"
          icon={<Receipt className="h-5 w-5" />}
        />
        <KpiCard
          value={formatCurrency(totals.voidedRevenue)}
          label={`Voided (${totals.voidedCount})`}
          icon={<AlertTriangle className="h-5 w-5" />}
          variant="warning"
        />
      </div>

      {/* Charts row */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2">
          <ChartContainer
            title="Revenue Trend"
            subtitle={`Monthly completed revenue (${totals.totalRevenue > 0 ? 'EGP' : 'no data'})`}
          >
            <AreaChart data={trend}>
              <defs>
                <linearGradient id="revGrad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#10B981" stopOpacity={0.4} />
                  <stop offset="95%" stopColor="#10B981" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="rgba(148,163,184,0.2)" />
              <XAxis dataKey="label" tick={{ fontSize: 11 }} />
              <YAxis tick={{ fontSize: 11 }} tickFormatter={(v) => `${(v / 1000).toFixed(0)}k`} />
              <Tooltip
                formatter={(v: number) => formatCurrency(v)}
                contentStyle={{ borderRadius: 8, fontSize: 12 }}
              />
              <Area
                type="monotone"
                dataKey="revenue"
                stroke="#10B981"
                strokeWidth={2}
                fill="url(#revGrad)"
              />
            </AreaChart>
          </ChartContainer>
        </div>

        <ChartContainer title="By Method" subtitle="Revenue split by payment method" height={300}>
          <PieChart>
            <Pie
              data={byMethod.length > 0 ? byMethod : [{ name: 'No data', value: 1 }]}
              dataKey="value"
              nameKey="name"
              cx="50%"
              cy="50%"
              outerRadius={90}
              label={(entry) => entry.name.replace(/_/g, ' ')}
            >
              {(byMethod.length > 0 ? byMethod : [{ name: 'No data', value: 1 }]).map((entry, i) => (
                <Cell
                  key={entry.name}
                  fill={byMethod.length > 0 ? METHOD_COLORS[entry.name] ?? '#94A3B8' : '#CBD5E1'}
                />
              ))}
            </Pie>
            <Tooltip formatter={(v: number) => formatCurrency(v)} />
          </PieChart>
        </ChartContainer>
      </div>

      {/* By purpose bar */}
      {byPurpose.length > 0 && (
        <ChartContainer title="By Purpose" subtitle="Revenue grouped by payment purpose" height={260}>
          <BarChart data={byPurpose} layout="vertical" margin={{ left: 60 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="rgba(148,163,184,0.2)" />
            <XAxis type="number" tick={{ fontSize: 11 }} tickFormatter={(v) => `${(v / 1000).toFixed(0)}k`} />
            <YAxis
              dataKey="name"
              type="category"
              tick={{ fontSize: 11 }}
              tickFormatter={(v: string) => v.replace(/_/g, ' ')}
              width={120}
            />
            <Tooltip formatter={(v: number) => formatCurrency(v)} />
            <Bar dataKey="value" fill="#8B5CF6" radius={[0, 4, 4, 0]} />
          </BarChart>
        </ChartContainer>
      )}

      {/* Payment history */}
      <div className="rounded-xl border bg-card shadow-sm overflow-hidden">
        <div className="flex items-center justify-between px-6 py-4 border-b">
          <div>
            <h4 className="text-sm font-semibold">Recent Payments</h4>
            <p className="text-xs text-muted-foreground mt-0.5">
              Last 15 of {payments.length} in selected range
            </p>
          </div>
        </div>
        {isLoading ? (
          <div className="p-10 text-center text-sm text-muted-foreground">Loading…</div>
        ) : recent.length === 0 ? (
          <div className="p-10 text-center text-sm text-muted-foreground">
            No payments recorded in this range.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50 text-xs uppercase tracking-wide text-muted-foreground">
                  <th className="text-left py-2.5 px-4 font-semibold">Receipt</th>
                  <th className="text-left py-2.5 px-4 font-semibold">Member</th>
                  <th className="text-left py-2.5 px-4 font-semibold">Purpose</th>
                  <th className="text-left py-2.5 px-4 font-semibold">Method</th>
                  <th className="text-right py-2.5 px-4 font-semibold">Amount</th>
                  <th className="text-left py-2.5 px-4 font-semibold">Status</th>
                  <th className="text-left py-2.5 px-4 font-semibold">Date</th>
                </tr>
              </thead>
              <tbody>
                {recent.map((p) => (
                  <tr key={p.id} className="border-b last:border-0 hover:bg-muted/40">
                    <td className="py-2.5 px-4 font-mono text-xs">{p.receiptNumber}</td>
                    <td className="py-2.5 px-4 font-medium">{p.memberName ?? '—'}</td>
                    <td className="py-2.5 px-4 capitalize">{humanize(p.purpose)}</td>
                    <td className="py-2.5 px-4 capitalize">{humanize(p.paymentMethod)}</td>
                    <td className="py-2.5 px-4 text-right font-medium">
                      {formatCurrency(p.amount)}
                    </td>
                    <td className="py-2.5 px-4">
                      <StatusBadge
                        variant={p.status.toLowerCase() as 'completed' | 'voided'}
                        label={p.status}
                      />
                    </td>
                    <td className="py-2.5 px-4 text-muted-foreground">
                      {formatDate(p.paidAt ?? p.createdAt)}
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
