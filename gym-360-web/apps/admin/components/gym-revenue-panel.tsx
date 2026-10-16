'use client';

import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { apiGet, BILLING } from '@gym360/ui';
import { AlertTriangle } from 'lucide-react';
import {
  latestPayments,
  rangeBounds,
  summarizeRevenue,
  unwrapPayments,
  type PagedPayments,
  type PaymentRow,
  type RevenueRange,
} from '@/lib/gym-revenue';
import { RevenueKpis } from './revenue/revenue-kpis';
import {
  RevenueByMethodChart,
  RevenueByPurposeChart,
  RevenueTrendChart,
} from './revenue/revenue-charts';
import { RecentPaymentsTable } from './revenue/recent-payments-table';

type Props = {
  gymId: string;
};

const RANGES = ['6m', '12m', 'ytd'] as const;

function RangeToggle({ range, onChange }: { range: RevenueRange; onChange: (r: RevenueRange) => void }) {
  return (
    <div className="inline-flex items-center rounded-lg border bg-card p-0.5 text-xs">
      {RANGES.map((r) => (
        <button
          key={r}
          onClick={() => onChange(r)}
          className={`px-3 py-1.5 rounded-md font-medium transition-colors ${
            range === r ? 'bg-primary text-white' : 'text-muted-foreground hover:text-foreground'
          }`}
        >
          {r === 'ytd' ? 'YTD' : r.toUpperCase()}
        </button>
      ))}
    </div>
  );
}

function RevenueError() {
  return (
    <div className="rounded-xl border bg-card p-10 text-center">
      <AlertTriangle className="mx-auto h-8 w-8 text-destructive" />
      <p className="mt-3 text-sm text-muted-foreground">Failed to load revenue data.</p>
    </div>
  );
}

function useGymPayments(gymId: string, from: string, to: string) {
  return useQuery({
    queryKey: ['gym-payments', gymId, from, to],
    queryFn: async () => {
      const res = await apiGet<PagedPayments | PaymentRow[]>(BILLING.PAYMENTS, {
        gymId,
        from,
        to,
        page: 1,
        pageSize: 500,
      });
      return unwrapPayments(res.data as unknown);
    },
    enabled: !!gymId,
  });
}

export function GymRevenuePanel({ gymId }: Props) {
  const [range, setRange] = useState<RevenueRange>('6m');

  const { from, to } = useMemo(() => rangeBounds(range), [range]);

  const { data: paymentsResp, isLoading, isError } = useGymPayments(gymId, from, to);

  const payments = paymentsResp ?? [];

  const { trend, totals, byMethod, byPurpose } = useMemo(
    () => summarizeRevenue(payments, range),
    [payments, range],
  );

  const recent = useMemo(() => latestPayments(payments, 15), [payments]);

  if (isError) {
    return <RevenueError />;
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
        <RangeToggle range={range} onChange={setRange} />
      </div>

      {/* KPIs */}
      <RevenueKpis totals={totals} range={range} monthCount={trend.length} />

      {/* Charts row */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2">
          <RevenueTrendChart trend={trend} totalRevenue={totals.totalRevenue} />
        </div>

        <RevenueByMethodChart byMethod={byMethod} />
      </div>

      {/* By purpose bar */}
      {byPurpose.length > 0 && <RevenueByPurposeChart byPurpose={byPurpose} />}

      {/* Payment history */}
      <RecentPaymentsTable isLoading={isLoading} recent={recent} totalCount={payments.length} />
    </div>
  );
}
