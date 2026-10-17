import { KpiCard, formatCurrency } from '@gym360/ui';
import { DollarSign, TrendingUp, Receipt, AlertTriangle } from 'lucide-react';
import type { RevenueRange, RevenueTotals } from '@/lib/gym-revenue';

type Props = {
  totals: RevenueTotals;
  range: RevenueRange;
  monthCount: number;
};

function roundedChange(momChange: number) {
  return momChange ? Math.round(momChange * 10) / 10 : undefined;
}

export function RevenueKpis({ totals, range, monthCount }: Props) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
      <KpiCard
        value={formatCurrency(totals.thisMonth)}
        label="This Month"
        change={roundedChange(totals.momChange)}
        icon={<DollarSign className="h-5 w-5" />}
        variant="success"
      />
      <KpiCard
        value={formatCurrency(totals.totalRevenue)}
        label={range === 'ytd' ? 'YTD Revenue' : `Last ${monthCount} Months`}
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
  );
}
