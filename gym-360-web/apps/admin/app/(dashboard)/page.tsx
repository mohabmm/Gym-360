'use client';

import { useState } from 'react';
import { KpiCard, ChartContainer, StatusBadge, PageHeader, ConfirmModal, formatCurrency, formatNumber } from '@gym360/ui';
import { Building2, Users, DollarSign, Dumbbell, AlertTriangle, TrendingUp, TrendingDown, Pause } from 'lucide-react';
import { AreaChart, Area, BarChart, Bar, PieChart, Pie, Cell, XAxis, YAxis, CartesianGrid, Tooltip, Legend } from 'recharts';
import Link from 'next/link';
import { useGyms, usePlatformAnalytics, useBulkSuspendGyms } from '@/lib/hooks';

const COLORS = ['#94A3B8', '#3B82F6', '#8B5CF6', '#F59E0B'];

export default function DashboardPage() {
  const { data: gymsData } = useGyms();
  const { data: analyticsData } = usePlatformAnalytics();
  const bulkSuspend = useBulkSuspendGyms();
  const [showBulkSuspend, setShowBulkSuspend] = useState(false);
  const [bulkResult, setBulkResult] = useState<string | null>(null);
  const [bulkSuspending, setBulkSuspending] = useState(false);

  const gyms = gymsData?.data ?? [];
  const analytics = analyticsData?.data;

  const recentRegistrations = gyms
    .filter((g) => g.status !== 'terminated')
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    .slice(0, 5);

  const topGyms = [...gyms]
    .filter((g) => g.status === 'active')
    .sort((a, b) => b.monthlyRevenue - a.monthlyRevenue)
    .slice(0, 5);
  const revenueTrend = analytics?.revenueTrend ?? [];
  const gymGrowth = analytics?.gymGrowth ?? [];
  // Only plot plans that have at least one gym — zero-count plans (incl. stale
  // QA/test plans) otherwise pile overlapping "0%" labels and a single 100%
  // slice renders as a glitchy broken ring.
  const planDistribution = (analytics?.planDistribution ?? []).filter((p) => p.count > 0);
  const atRiskGyms = analytics?.atRiskGyms ?? [];

  const totalGyms = gyms.filter((g) => g.status !== 'terminated').length;
  const totalMembers = analytics?.kpis?.totalMembers ?? gyms.reduce((sum, g) => sum + g.activeMemberCount, 0);
  const totalTrainers = analytics?.kpis?.totalTrainers ?? gyms.reduce((sum, g) => sum + g.activeTrainerCount, 0);
  const mrr = analytics?.kpis?.mrr ?? gyms.reduce((sum, g) => sum + g.monthlyRevenue, 0);
  const overdueGyms = gyms.filter((g) => g.paymentStatus === 'overdue');
  const warningGyms = gyms.filter((g) => g.paymentStatus === 'warning');
  const paymentAlerts = overdueGyms.length + warningGyms.length;

  const bulkSuspendOverdue = async () => {
    const ids = overdueGyms.filter((g) => g.status === 'active').map((g) => g.id);
    if (ids.length === 0) { setShowBulkSuspend(false); return; }
    setBulkSuspending(true);
    setShowBulkSuspend(false);
    try {
      const { successCount, failCount } = await bulkSuspend.mutateAsync({
        gymIds: ids,
        reason: 'Bulk suspend — payment overdue',
      });
      const msg = failCount > 0
        ? `Suspended ${successCount} gym${successCount === 1 ? '' : 's'}. ${failCount} failed.`
        : `Suspended ${successCount} overdue gym${successCount === 1 ? '' : 's'}.`;
      setBulkResult(msg);
      setTimeout(() => setBulkResult(null), 5000);
    } finally {
      setBulkSuspending(false);
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title="Dashboard"
        description="Platform overview and key metrics"
      />

      {/* Payment Alerts Bar */}
      {paymentAlerts > 0 && (
        <div className="flex items-center gap-3 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 dark:border-amber-800 dark:bg-amber-950/20">
          <AlertTriangle className="h-5 w-5 text-amber-600 shrink-0" />
          <p className="text-sm font-medium text-amber-800 dark:text-amber-300">
            {paymentAlerts} gym{paymentAlerts > 1 ? 's' : ''} with payment issues
            {overdueGyms.length > 0 && (
              <>
                {' '}— <span className="font-semibold">{overdueGyms.length} overdue</span>
              </>
            )}
            {warningGyms.length > 0 && (
              <>
                {overdueGyms.length > 0 ? ',' : ' —'} {warningGyms.length} in warning
              </>
            )}
          </p>
          <div className="ml-auto flex items-center gap-2">
            {overdueGyms.filter((g) => g.status === 'active').length > 0 && (
              <button
                onClick={() => setShowBulkSuspend(true)}
                className="inline-flex items-center gap-1.5 rounded-lg border border-amber-300 bg-white px-3 py-1.5 text-xs font-medium text-amber-800 hover:bg-amber-100 dark:bg-amber-900/20"
              >
                <Pause className="h-3.5 w-3.5" /> Bulk Suspend Overdue ({overdueGyms.filter((g) => g.status === 'active').length})
              </button>
            )}
            <Link
              href="/gyms?paymentStatus=overdue"
              className="text-sm font-medium text-amber-700 hover:text-amber-900 dark:text-amber-400"
            >
              View All
            </Link>
          </div>
        </div>
      )}

      {bulkResult && (
        <div className="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-2 text-sm font-medium text-emerald-800">
          {bulkResult}
        </div>
      )}

      <ConfirmModal
        open={showBulkSuspend}
        onClose={() => setShowBulkSuspend(false)}
        onConfirm={bulkSuspendOverdue}
        title="Bulk Suspend Overdue Gyms"
        description={`Suspend all ${overdueGyms.filter((g) => g.status === 'active').length} active gyms whose payment is overdue? They will lose access immediately and receive a suspension notice.`}
        confirmLabel="Suspend all overdue"
        variant="warning"
        isLoading={bulkSuspending}
      />

      {/* KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <KpiCard
          value={totalGyms.toString()}
          label="Total Gyms"
          change={analytics?.kpis?.activeGymsChange}
          icon={<Building2 className="h-5 w-5" />}
          variant="info"
        />
        <KpiCard
          value={formatNumber(totalMembers)}
          label="Total Members"
          change={analytics?.kpis?.memberGrowthRate}
          icon={<Users className="h-5 w-5" />}
          variant="success"
        />
        <KpiCard
          value={formatCurrency(mrr)}
          label="Monthly Revenue"
          change={analytics?.kpis?.mrrChange}
          icon={<DollarSign className="h-5 w-5" />}
          variant="default"
        />
        <KpiCard
          value={totalTrainers.toString()}
          label="Total Trainers"
          change={analytics?.kpis?.trainerGrowthRate}
          icon={<Dumbbell className="h-5 w-5" />}
          variant="default"
        />
      </div>

      {/* Charts Row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <ChartContainer title="Revenue Trend" subtitle="Monthly recurring revenue (EGP)">
          <AreaChart data={revenueTrend}>
            <defs>
              <linearGradient id="colorRevenue" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor="#10B981" stopOpacity={0.3} />
                <stop offset="95%" stopColor="#10B981" stopOpacity={0} />
              </linearGradient>
            </defs>
            <CartesianGrid strokeDasharray="3 3" stroke="#E2E8F0" />
            <XAxis dataKey="date" tick={{ fontSize: 12 }} stroke="#94A3B8" />
            <YAxis tick={{ fontSize: 12 }} stroke="#94A3B8" tickFormatter={(v: number) => `${(v / 1000).toFixed(0)}K`} />
            <Tooltip formatter={(value: number) => formatCurrency(value)} />
            <Area type="monotone" dataKey="value" stroke="#10B981" strokeWidth={2} fill="url(#colorRevenue)" />
          </AreaChart>
        </ChartContainer>

        <ChartContainer title="Gym Growth" subtitle="Total active gyms per month">
          <BarChart data={gymGrowth}>
            <CartesianGrid strokeDasharray="3 3" stroke="#E2E8F0" />
            <XAxis dataKey="date" tick={{ fontSize: 12 }} stroke="#94A3B8" />
            <YAxis tick={{ fontSize: 12 }} stroke="#94A3B8" />
            <Tooltip />
            <Bar dataKey="value" fill="#3B82F6" radius={[4, 4, 0, 0]} />
          </BarChart>
        </ChartContainer>
      </div>

      {/* Bottom Row */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Recent Registrations */}
        <div className="rounded-xl border bg-card p-6 shadow-sm">
          <h3 className="text-sm font-semibold mb-4">Recent Registrations</h3>
          <div className="space-y-3">
            {recentRegistrations.map((gym) => (
              <div key={gym.id} className="flex items-center justify-between py-2 border-b last:border-0">
                <div>
                  <p className="text-sm font-medium">{gym.name}</p>
                  <p className="text-xs text-muted-foreground">{gym.city} &middot; {gym.ownerName}</p>
                </div>
                <StatusBadge variant={gym.status} />
              </div>
            ))}
          </div>
        </div>

        {/* At-Risk Gyms */}
        <div className="rounded-xl border bg-card p-6 shadow-sm">
          <h3 className="text-sm font-semibold mb-4 flex items-center gap-2">
            <AlertTriangle className="h-4 w-4 text-amber-500" />
            At-Risk Gyms
          </h3>
          <div className="space-y-3">
            {atRiskGyms.map((gym) => (
              <div key={gym.id} className="flex items-center justify-between py-2 border-b last:border-0">
                <div>
                  <p className="text-sm font-medium">{gym.name}</p>
                  <p className="text-xs text-muted-foreground">{gym.riskFactors[0]}</p>
                </div>
                <div className="flex items-center gap-2">
                  <span className="text-xs font-bold text-red-500">{gym.riskScore}%</span>
                  <TrendingDown className="h-3.5 w-3.5 text-red-500" />
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Plan Distribution */}
        <ChartContainer title="Plan Distribution" height={200}>
          {planDistribution.length === 0 ? (
            <div className="flex h-full items-center justify-center text-sm text-muted-foreground">
              No plan data yet
            </div>
          ) : (
            <PieChart>
              <Pie
                data={planDistribution}
                cx="50%"
                cy="50%"
                outerRadius={80}
                innerRadius={40}
                dataKey="count"
                nameKey="plan"
                stroke="none"
                isAnimationActive={false}
                label={({ plan, percentage }: { plan: string; percentage: number }) => `${plan} ${percentage}%`}
              >
                {planDistribution.map((entry, index) => (
                  <Cell key={entry.plan} fill={COLORS[index % COLORS.length]} />
                ))}
              </Pie>
              <Tooltip />
            </PieChart>
          )}
        </ChartContainer>
      </div>

      {/* Top Gyms */}
      <div className="rounded-xl border bg-card p-6 shadow-sm">
        <h3 className="text-sm font-semibold mb-4">Top Performing Gyms</h3>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b">
                <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Gym</th>
                <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Plan</th>
                <th className="text-right py-2 px-3 font-semibold text-muted-foreground">Members</th>
                <th className="text-right py-2 px-3 font-semibold text-muted-foreground">Revenue</th>
                <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Status</th>
              </tr>
            </thead>
            <tbody>
              {topGyms.map((gym, index) => (
                <tr key={gym.id} className="border-b last:border-0 hover:bg-muted/50">
                  <td className="py-2.5 px-3">
                    <div className="flex items-center gap-2">
                      <span className="text-xs font-bold text-muted-foreground w-5">#{index + 1}</span>
                      <div>
                        <p className="font-medium">{gym.name}</p>
                        <p className="text-xs text-muted-foreground">{gym.city}</p>
                      </div>
                    </div>
                  </td>
                  <td className="py-2.5 px-3"><StatusBadge variant={gym.planName.toLowerCase()} /></td>
                  <td className="py-2.5 px-3 text-right font-medium">{gym.activeMemberCount}</td>
                  <td className="py-2.5 px-3 text-right font-medium">{formatCurrency(gym.monthlyRevenue)}</td>
                  <td className="py-2.5 px-3"><StatusBadge variant={gym.paymentStatus} dot /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
