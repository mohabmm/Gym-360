'use client';

import { useState } from 'react';
import { KpiCard, PageHeader, ConfirmModal, formatCurrency, formatNumber, type GymListItem, type PlatformKpis } from '@gym360/ui';
import { Building2, Users, DollarSign, Dumbbell } from 'lucide-react';
import { useGyms, usePlatformAnalytics, useBulkSuspendGyms } from '@/lib/hooks';
import { PlanDistributionChart } from '@/components/plan-distribution-chart';
import { AreaTrendCard, BarTrendCard, thousands } from '@/components/charts';
import { AtRiskGyms, PaymentAlertsBar, RecentRegistrations, TopGymsTable, gyms as gymCount } from '@/components/dashboard-panels';

const sumBy = (gyms: GymListItem[], pick: (g: GymListItem) => number) =>
  gyms.reduce((sum, g) => sum + pick(g), 0);

// Platform KPIs from analytics, falling back to totals over the gym list.
function platformTotals(gyms: GymListItem[], kpis?: PlatformKpis) {
  return {
    totalGyms: gyms.filter((g) => g.status !== 'terminated').length,
    totalMembers: kpis?.totalMembers ?? sumBy(gyms, (g) => g.activeMemberCount),
    totalTrainers: kpis?.totalTrainers ?? sumBy(gyms, (g) => g.activeTrainerCount),
    mrr: kpis?.mrr ?? sumBy(gyms, (g) => g.monthlyRevenue),
  };
}

function bulkSuspendMessage(successCount: number, failCount: number) {
  if (failCount > 0) return `Suspended ${gymCount(successCount)}. ${failCount} failed.`;
  return `Suspended ${successCount} overdue gym${successCount === 1 ? '' : 's'}.`;
}

// Lists derived from the gym registry for the dashboard panels + payment alerts.
function dashboardLists(gyms: GymListItem[]) {
  const overdueGyms = gyms.filter((g) => g.paymentStatus === 'overdue');
  return {
    recentRegistrations: gyms
      .filter((g) => g.status !== 'terminated')
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      .slice(0, 5),
    topGyms: [...gyms]
      .filter((g) => g.status === 'active')
      .sort((a, b) => b.monthlyRevenue - a.monthlyRevenue)
      .slice(0, 5),
    overdueCount: overdueGyms.length,
    warningCount: gyms.filter((g) => g.paymentStatus === 'warning').length,
    suspendableIds: overdueGyms.filter((g) => g.status === 'active').map((g) => g.id),
  };
}

function useBulkSuspendOverdue(suspendableIds: string[]) {
  const bulkSuspend = useBulkSuspendGyms();
  const [showBulkSuspend, setShowBulkSuspend] = useState(false);
  const [bulkResult, setBulkResult] = useState<string | null>(null);
  const [bulkSuspending, setBulkSuspending] = useState(false);

  const bulkSuspendOverdue = async () => {
    setShowBulkSuspend(false);
    if (suspendableIds.length === 0) return;
    setBulkSuspending(true);
    try {
      const { successCount, failCount } = await bulkSuspend.mutateAsync({
        gymIds: suspendableIds,
        reason: 'Bulk suspend — payment overdue',
      });
      setBulkResult(bulkSuspendMessage(successCount, failCount));
      setTimeout(() => setBulkResult(null), 5000);
    } finally {
      setBulkSuspending(false);
    }
  };

  return {
    showBulkSuspend,
    openBulkSuspend: () => setShowBulkSuspend(true),
    closeBulkSuspend: () => setShowBulkSuspend(false),
    bulkResult,
    bulkSuspending,
    bulkSuspendOverdue,
  };
}

function DashboardKpis({ gyms, kpis }: { gyms: GymListItem[]; kpis?: PlatformKpis }) {
  const { totalGyms, totalMembers, totalTrainers, mrr } = platformTotals(gyms, kpis);
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
      <KpiCard
        value={totalGyms.toString()}
        label="Total Gyms"
        change={kpis?.activeGymsChange}
        icon={<Building2 className="h-5 w-5" />}
        variant="info"
      />
      <KpiCard
        value={formatNumber(totalMembers)}
        label="Total Members"
        change={kpis?.memberGrowthRate}
        icon={<Users className="h-5 w-5" />}
        variant="success"
      />
      <KpiCard
        value={formatCurrency(mrr)}
        label="Monthly Revenue"
        change={kpis?.mrrChange}
        icon={<DollarSign className="h-5 w-5" />}
        variant="default"
      />
      <KpiCard
        value={totalTrainers.toString()}
        label="Total Trainers"
        change={kpis?.trainerGrowthRate}
        icon={<Dumbbell className="h-5 w-5" />}
        variant="default"
      />
    </div>
  );
}

export default function DashboardPage() {
  const { data: gymsData } = useGyms();
  const { data: analyticsData } = usePlatformAnalytics();

  const gyms = gymsData?.data ?? [];
  const analytics = analyticsData?.data;
  const kpis = analytics?.kpis;
  const { recentRegistrations, topGyms, overdueCount, warningCount, suspendableIds } = dashboardLists(gyms);
  const bulk = useBulkSuspendOverdue(suspendableIds);

  return (
    <div className="space-y-6">
      <PageHeader
        title="Dashboard"
        description="Platform overview and key metrics"
      />

      <PaymentAlertsBar
        overdueCount={overdueCount}
        warningCount={warningCount}
        suspendableCount={suspendableIds.length}
        onBulkSuspend={bulk.openBulkSuspend}
      />

      {bulk.bulkResult && (
        <div className="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-2 text-sm font-medium text-emerald-800">
          {bulk.bulkResult}
        </div>
      )}

      <ConfirmModal
        open={bulk.showBulkSuspend}
        onClose={bulk.closeBulkSuspend}
        onConfirm={bulk.bulkSuspendOverdue}
        title="Bulk Suspend Overdue Gyms"
        description={`Suspend all ${suspendableIds.length} active gyms whose payment is overdue? They will lose access immediately and receive a suspension notice.`}
        confirmLabel="Suspend all overdue"
        variant="warning"
        isLoading={bulk.bulkSuspending}
      />

      {/* KPI Cards */}
      <DashboardKpis gyms={gyms} kpis={kpis} />

      {/* Charts Row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <AreaTrendCard
          title="Revenue Trend"
          subtitle="Monthly recurring revenue (EGP)"
          data={analytics?.revenueTrend ?? []}
          gradientId="colorRevenue"
          yTickFormatter={thousands}
          valueFormatter={formatCurrency}
        />
        <BarTrendCard
          title="Gym Growth"
          subtitle="Total active gyms per month"
          data={analytics?.gymGrowth ?? []}
          color="#3B82F6"
        />
      </div>

      {/* Bottom Row */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <RecentRegistrations gyms={recentRegistrations} />
        <AtRiskGyms gyms={analytics?.atRiskGyms ?? []} />
        <PlanDistributionChart data={analytics?.planDistribution} height={200} />
      </div>

      <TopGymsTable gyms={topGyms} />
    </div>
  );
}
