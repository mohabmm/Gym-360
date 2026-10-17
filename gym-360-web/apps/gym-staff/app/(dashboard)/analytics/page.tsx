'use client';

import { PageHeader, KpiCard, formatCurrency, formatNumber } from '@gym360/ui';
import { Users, DollarSign, DoorOpen, BarChart3 } from 'lucide-react';
import { useRevenue } from '@/lib/hooks/useRevenue';
import { useGymAnalytics, useClassesForUtilization, usePeakHours } from '@/lib/hooks/useAnalytics';
import { toHeatRows, toClassUtilization } from '@/lib/analytics';
import { RevenueTrendChart } from '@/components/RevenueTrendChart';
import { MemberGrowthChart, PeakHoursHeatmap, ClassUtilizationChart } from '@/components/analytics/AnalyticsCharts';

export default function AnalyticsPage() {
  const { data: revenueData } = useRevenue();
  const monthlyRevenue = revenueData?.monthlyRevenue ?? [];
  const totalRevenue = monthlyRevenue.reduce((sum, m) => sum + m.revenue, 0);
  const { data: analytics } = useGymAnalytics();
  const { data: classes } = useClassesForUtilization();
  const classUtilization = toClassUtilization(classes);
  const memberGrowth = (analytics?.memberGrowth as { month: string; active: number; new: number }[] | undefined) ?? [];
  const { data: peakHoursRaw } = usePeakHours();
  const peakHours = toHeatRows(peakHoursRaw?.buckets);

  return (
    <div className="space-y-6">
      <PageHeader title="Analytics" description="First Gym performance insights" />

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <KpiCard value={formatNumber(analytics?.totalMembers ?? 0)} label="Active Members" icon={<Users className="h-5 w-5" />} variant="success" />
        <KpiCard value={formatCurrency(totalRevenue)} label="Revenue" icon={<DollarSign className="h-5 w-5" />} variant="info" />
        <KpiCard value={formatNumber(analytics?.checkInsToday ?? 0)} label="Daily Check-ins" icon={<DoorOpen className="h-5 w-5" />} />
        <KpiCard value={formatNumber(analytics?.totalClasses ?? 0)} label="Total Classes" icon={<BarChart3 className="h-5 w-5" />} />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <MemberGrowthChart data={memberGrowth} />

        <RevenueTrendChart data={monthlyRevenue} subtitle="Monthly revenue (EGP)" gradientId="colorStaffRev" />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Peak Hours */}
        <PeakHoursHeatmap rows={peakHours} />

        {/* Class Utilization */}
        <ClassUtilizationChart data={classUtilization} />
      </div>
    </div>
  );
}
