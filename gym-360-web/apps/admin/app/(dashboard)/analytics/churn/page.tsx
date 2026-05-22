'use client';

import { PageHeader, KpiCard, ChartContainer, StatusBadge, DataTable, formatCurrency, PageSkeleton } from '@gym360/ui';
import { type ColumnDef } from '@tanstack/react-table';
import { TrendingDown, AlertTriangle, DollarSign, AlertCircle } from 'lucide-react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Cell } from 'recharts';
import { useChurnAnalytics } from '@/lib/hooks';
import type { AtRiskGym } from '@gym360/ui';

const FALLBACK_CHURN_BY_PLAN = [
  { plan: 'Free', churnRate: 18, color: '#94A3B8' },
  { plan: 'Starter', churnRate: 8, color: '#3B82F6' },
  { plan: 'Pro', churnRate: 3, color: '#8B5CF6' },
  { plan: 'Business', churnRate: 1, color: '#F59E0B' },
];

const atRiskColumns: ColumnDef<AtRiskGym, unknown>[] = [
  {
    accessorKey: 'name',
    header: 'Gym',
    cell: ({ row }) => (
      <div>
        <p className="font-medium">{row.original.name}</p>
        <StatusBadge variant={row.original.planName.toLowerCase()} className="mt-1" />
      </div>
    ),
  },
  {
    accessorKey: 'riskScore',
    header: 'Risk Score',
    cell: ({ row }) => (
      <div className="flex items-center gap-2">
        <div className="w-12 h-2 rounded-full bg-muted overflow-hidden">
          <div
            className={`h-full rounded-full ${row.original.riskScore >= 70 ? 'bg-red-500' : row.original.riskScore >= 50 ? 'bg-amber-500' : 'bg-emerald-500'}`}
            style={{ width: `${row.original.riskScore}%` }}
          />
        </div>
        <span className="text-sm font-bold">{row.original.riskScore}%</span>
      </div>
    ),
  },
  {
    accessorKey: 'riskFactors',
    header: 'Risk Factors',
    cell: ({ row }) => (
      <div className="flex flex-wrap gap-1">
        {row.original.riskFactors.map((factor) => (
          <span key={factor} className="text-xs bg-red-50 text-red-700 px-2 py-0.5 rounded dark:bg-red-900/20 dark:text-red-400">
            {factor}
          </span>
        ))}
      </div>
    ),
  },
  { accessorKey: 'memberCount', header: 'Members' },
  {
    accessorKey: 'monthlyRevenue',
    header: 'Revenue',
    cell: ({ row }) => formatCurrency(row.original.monthlyRevenue),
  },
  {
    accessorKey: 'lastActivity',
    header: 'Last Activity',
    cell: ({ row }) => new Date(row.original.lastActivity).toLocaleDateString(),
  },
];

export default function ChurnAnalyticsPage() {
  const { data: churnData, isLoading, isError } = useChurnAnalytics();
  const analytics = churnData?.data;
  const usingFallback = !analytics;
  const atRiskGyms = analytics?.atRiskGyms ?? [];
  const churnByPlan = analytics?.churnByPlan?.map((c, i) => ({
    ...c,
    color: FALLBACK_CHURN_BY_PLAN[i]?.color ?? '#94A3B8',
  })) ?? FALLBACK_CHURN_BY_PLAN;
  const metrics = analytics?.metrics;

  if (isLoading) return <PageSkeleton />;

  return (
    <div className="space-y-6">
      <PageHeader
        title="Churn Analytics"
        description="Monitor gym churn risk and retention metrics"
        breadcrumbs={[{ label: 'Analytics', href: '/analytics' }, { label: 'Churn' }]}
      />

      {isError && (
        <div className="flex items-center gap-3 rounded-xl border border-red-200 bg-red-50 px-4 py-4 dark:border-red-800 dark:bg-red-950/20">
          <AlertCircle className="h-5 w-5 text-red-500 shrink-0" />
          <p className="text-sm text-red-700 dark:text-red-400">Failed to load churn analytics. Check API connectivity.</p>
        </div>
      )}
      {!isError && usingFallback && (
        <div className="flex items-center gap-3 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 dark:border-amber-800 dark:bg-amber-950/20">
          <AlertTriangle className="h-5 w-5 text-amber-600 shrink-0" />
          <p className="text-sm text-amber-800 dark:text-amber-300">Showing sample data — live churn metrics unavailable.</p>
        </div>
      )}

      {/* KPIs */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <KpiCard
          value={metrics ? `${metrics.churnRate}%` : '—'}
          label="Monthly Churn Rate"
          change={metrics?.churnRateChange}
          icon={<TrendingDown className="h-5 w-5" />}
          variant="danger"
        />
        <KpiCard
          value={metrics?.atRiskCount.toString() ?? '—'}
          label="At-Risk Gyms"
          change={metrics?.atRiskChange}
          icon={<AlertTriangle className="h-5 w-5" />}
          variant="warning"
        />
        <KpiCard
          value={metrics ? formatCurrency(metrics.avgLifetimeValue) : '—'}
          label="Avg. Lifetime Value"
          change={metrics?.lifetimeValueChange}
          icon={<DollarSign className="h-5 w-5" />}
          variant="success"
        />
      </div>

      {/* Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-1 gap-6">
        <ChartContainer title="Churn Rate by Plan" subtitle="Monthly churn percentage" height={280}>
          <BarChart data={churnByPlan}>
            <CartesianGrid strokeDasharray="3 3" stroke="#E2E8F0" />
            <XAxis dataKey="plan" tick={{ fontSize: 12 }} stroke="#94A3B8" />
            <YAxis tick={{ fontSize: 12 }} stroke="#94A3B8" tickFormatter={(v: number) => `${v}%`} />
            <Tooltip formatter={(value: number) => `${value}%`} />
            <Bar dataKey="churnRate" radius={[4, 4, 0, 0]}>
              {churnByPlan.map((entry) => (
                <Cell key={entry.plan} fill={entry.color} />
              ))}
            </Bar>
          </BarChart>
        </ChartContainer>
      </div>

      {/* At-Risk Table */}
      <div>
        <h2 className="text-lg font-semibold mb-4 flex items-center gap-2">
          <AlertTriangle className="h-5 w-5 text-amber-500" />
          At-Risk Gyms
        </h2>
        <DataTable
          data={atRiskGyms}
          columns={atRiskColumns}
          searchPlaceholder="Search at-risk gyms..."
          searchColumn="name"
        />
      </div>
    </div>
  );
}
