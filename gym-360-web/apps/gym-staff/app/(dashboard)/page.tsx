'use client';

import { useSession } from 'next-auth/react';
import { KpiCard, formatCurrency } from '@gym360/ui';
import { Users, DollarSign, DoorOpen, Dumbbell } from 'lucide-react';
import { useMembers } from '@/lib/hooks/useMembers';
import { useTrainers } from '@/lib/hooks/useTrainers';
import { useClasses } from '@/lib/hooks/useClasses';
import { useCheckIns } from '@/lib/hooks/useCheckIns';
import { useRevenue } from '@/lib/hooks/useRevenue';
import { useTrainerRankings } from '@/lib/hooks/useGamification';
import { greetingFor, memberGrowth, checkInStats, revenueStats, attentionItems } from '@/lib/dashboard-stats';
import { RevenueTrendChart } from '@/components/RevenueTrendChart';
import {
  TodaySchedule,
  QuickActions,
  TopClasses,
  NeedsAttention,
  CoachOfMonth,
  RecentActivity,
} from '@/components/dashboard/DashboardSections';

export default function DashboardPage() {
  const { data: session } = useSession();
  const userName = session?.user?.name ?? 'there';
  const gymName = session?.user?.gymName ?? 'your gym';
  const greeting = greetingFor(new Date().getHours());

  const { data: members = [] } = useMembers();
  const { data: trainers = [] } = useTrainers();
  const { data: classes = [] } = useClasses();
  const { data: checkIns = [] } = useCheckIns();
  const { data: revenueData } = useRevenue();
  const monthlyRevenue = revenueData?.monthlyRevenue ?? [];
  const recentPayments = revenueData?.recentPayments ?? [];
  const { data: trainerRankings = [] } = useTrainerRankings();

  const activeMembers = members.filter((m) => m.status === 'active').length;
  const todaySchedule = classes.slice(0, 4);

  const now = new Date();
  const revenue = revenueStats(monthlyRevenue);
  const memberChange = memberGrowth(members, now);
  const checkInsToday = checkInStats(checkIns, now);
  const pendingTrainers = trainers.filter((t) => t.status === 'pending_approval').length;

  return (
    <div className="space-y-6">
      {/* Welcome */}
      <div>
        <h1 className="text-2xl font-bold font-heading">{greeting}, {userName}!</h1>
        <p className="text-muted-foreground mt-1">Here is what is happening at {gymName} today.</p>
      </div>

      {/* KPIs */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <KpiCard value={activeMembers.toString()} label="Active Members" change={memberChange} changeSuffix="new vs last month" icon={<Users className="h-5 w-5" />} variant="success" />
        <KpiCard value={formatCurrency(revenue.current)} label="Monthly Revenue" change={revenue.change} icon={<DollarSign className="h-5 w-5" />} variant="info" />
        <KpiCard value={checkInsToday.today.toString()} label="Today's Check-ins" change={checkInsToday.change} changeSuffix="vs yesterday" icon={<DoorOpen className="h-5 w-5" />} variant="default" />
        <KpiCard value={trainers.filter((t) => t.status === 'active').length.toString()} label="Active Trainers" icon={<Dumbbell className="h-5 w-5" />} variant="default" />
      </div>

      {/* Charts Row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <RevenueTrendChart data={monthlyRevenue} subtitle="Last 6 months (EGP)" gradientId="colorRev" />

        {/* Today's Schedule */}
        <TodaySchedule classes={todaySchedule} />
      </div>

      {/* Quick Actions */}
      <QuickActions />

      {/* Bottom Row */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Top Classes */}
        <TopClasses classes={classes} />

        {/* Needs Attention — driven by live counts; suppressed when nothing to show. */}
        <NeedsAttention items={attentionItems(pendingTrainers, members)} />

        {/* Coach of Month */}
        <CoachOfMonth top={trainerRankings[0]} />
      </div>

      {/* Recent Activity */}
      <RecentActivity payments={recentPayments} />
    </div>
  );
}
