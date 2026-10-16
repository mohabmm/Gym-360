'use client';

import { PageHeader, KpiCard, ChartContainer, formatCurrency, formatNumber, type PlatformKpis } from '@gym360/ui';
import { Building2, Users, DollarSign, Dumbbell, TrendingUp, Activity, AlertCircle } from 'lucide-react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip } from 'recharts';
import { usePlatformAnalytics } from '@/lib/hooks';
import { PageSkeleton } from '@gym360/ui';
import { PlanDistributionChart } from '@/components/plan-distribution-chart';
import { AreaTrendCard, BarTrendCard, thousands } from '@/components/charts';

const FALLBACK_DAU = [
  { date: 'Mon', value: 3200 }, { date: 'Tue', value: 3450 },
  { date: 'Wed', value: 3100 }, { date: 'Thu', value: 3600 },
  { date: 'Fri', value: 2800 }, { date: 'Sat', value: 4200 },
  { date: 'Sun', value: 3900 },
];

// KPI card values; '—' until analytics has loaded.
function kpiValues(kpis?: PlatformKpis) {
  if (!kpis) return { gyms: '—', members: '—', trainers: '—', mrr: '—' };
  return {
    gyms: kpis.totalGyms.toString(),
    members: formatNumber(kpis.totalMembers),
    trainers: kpis.totalTrainers.toString(),
    mrr: formatCurrency(kpis.mrr),
  };
}

function FeatureUsageCard({ features }: { features: { featureCode: string; featureName: string; adoptionRate: number }[] }) {
  return (
    <div className="rounded-xl border bg-card p-6 shadow-sm">
      <h3 className="text-sm font-semibold mb-4">Feature Usage</h3>
      <div className="space-y-3">
        {features.slice(0, 6).map((feature) => (
          <div key={feature.featureCode} className="flex items-center gap-3">
            <span className="text-sm w-40 truncate">{feature.featureName}</span>
            <div className="flex-1 h-2 rounded-full bg-muted overflow-hidden">
              <div
                className="h-full bg-primary rounded-full"
                style={{ width: `${feature.adoptionRate}%` }}
              />
            </div>
            <span className="text-xs font-medium w-10 text-right">{feature.adoptionRate}%</span>
          </div>
        ))}
      </div>
    </div>
  );
}

export default function AnalyticsPage() {
  const { data: analyticsData, isLoading, isError } = usePlatformAnalytics();
  const analytics = analyticsData?.data;
  const kpis = analytics?.kpis;
  const values = kpiValues(kpis);

  if (isLoading) return <PageSkeleton />;
  if (isError) return (
    <div className="flex items-center gap-3 rounded-xl border border-red-200 bg-red-50 px-4 py-4 dark:border-red-800 dark:bg-red-950/20">
      <AlertCircle className="h-5 w-5 text-red-500 shrink-0" />
      <p className="text-sm text-red-700 dark:text-red-400">Failed to load analytics. Please refresh the page.</p>
    </div>
  );

  return (
    <div className="space-y-6">
      <PageHeader
        title="Platform Analytics"
        description="Comprehensive platform metrics and trends"
      />

      {/* KPIs */}
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
        <KpiCard value={values.gyms} label="Total Gyms" change={kpis?.activeGymsChange} icon={<Building2 className="h-5 w-5" />} variant="info" />
        <KpiCard value={values.members} label="Total Members" change={kpis?.memberGrowthRate} icon={<Users className="h-5 w-5" />} variant="success" />
        <KpiCard value={values.trainers} label="Total Trainers" change={kpis?.trainerGrowthRate} icon={<Dumbbell className="h-5 w-5" />} />
        <KpiCard value={values.mrr} label="MRR" change={kpis?.mrrChange} icon={<DollarSign className="h-5 w-5" />} />
        <KpiCard value="—" label="DAU" change={undefined} icon={<Activity className="h-5 w-5" />} variant="success" />
        <KpiCard value="—" label="Feature Adoption" change={undefined} icon={<TrendingUp className="h-5 w-5" />} />
      </div>

      {/* Charts Row 1 */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <AreaTrendCard
          title="Member Growth"
          subtitle="Total active members"
          data={analytics?.memberGrowth ?? []}
          gradientId="colorMembers"
        />
        <BarTrendCard
          title="Gym Registrations"
          subtitle="New gyms per month"
          data={analytics?.gymGrowth ?? []}
          color="#3B82F6"
        />
      </div>

      {/* Charts Row 2 */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <ChartContainer title="Revenue by City" subtitle="Monthly revenue per city" className="lg:col-span-2">
          <BarChart data={analytics?.cityRevenue ?? []} layout="vertical">
            <CartesianGrid strokeDasharray="3 3" stroke="#E2E8F0" />
            <XAxis type="number" tick={{ fontSize: 12 }} stroke="#94A3B8" tickFormatter={thousands} />
            <YAxis type="category" dataKey="city" tick={{ fontSize: 12 }} stroke="#94A3B8" width={100} />
            <Tooltip formatter={(value: number) => formatCurrency(value)} />
            <Bar dataKey="revenue" fill="#8B5CF6" radius={[0, 4, 4, 0]} />
          </BarChart>
        </ChartContainer>

        <PlanDistributionChart data={analytics?.planDistribution} height={250} />
      </div>

      {/* DAU + Feature Usage */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <BarTrendCard title="Daily Active Users" subtitle="This week" data={FALLBACK_DAU} color="#F59E0B" />
        <FeatureUsageCard features={analytics?.featureUsage ?? []} />
      </div>
    </div>
  );
}
