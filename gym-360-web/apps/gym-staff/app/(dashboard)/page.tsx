'use client';

import type { ReactNode } from 'react';
import { useSession } from 'next-auth/react';
import { KpiCard, formatCurrency, isPathEnabled, FEATURE_FLAG_KEYS as K } from '@gym360/ui';
import { Users, DollarSign, DoorOpen, Dumbbell } from 'lucide-react';
import { useMembers } from '@/lib/hooks/useMembers';
import { useTrainers } from '@/lib/hooks/useTrainers';
import { useClasses } from '@/lib/hooks/useClasses';
import { useCheckIns } from '@/lib/hooks/useCheckIns';
import { useRevenue } from '@/lib/hooks/useRevenue';
import { useTrainerRankings } from '@/lib/hooks/useGamification';
import { greetingFor, memberGrowth, checkInStats, revenueStats, attentionItems } from '@/lib/dashboard-stats';
import { STAFF_ROUTE_FLAGS, useStaffFlags } from '@/lib/feature-flags';
import { RevenueTrendChart } from '@/components/RevenueTrendChart';
import { PtDeskSection } from '@/components/dashboard/PtDeskSection';
import {
  TodaySchedule,
  QuickActions,
  TopClasses,
  NeedsAttention,
  CoachOfMonth,
  RecentActivity,
} from '@/components/dashboard/DashboardSections';

/** Grid that only renders the cells it is given, so hidden features leave no empty columns. */
function Row({ cells, cols }: { cells: (ReactNode | false)[]; cols: Record<number, string> }) {
  const shown = cells.filter(Boolean) as ReactNode[];
  if (shown.length === 0) return null;
  return <div className={`grid grid-cols-1 gap-6 ${cols[shown.length] ?? ''}`}>{shown}</div>;
}

const KPI_COLS: Record<number, string> = { 2: 'md:grid-cols-2', 3: 'md:grid-cols-3', 4: 'md:grid-cols-2 lg:grid-cols-4' };
const PAIR_COLS: Record<number, string> = { 2: 'lg:grid-cols-2' };
const TRIO_COLS: Record<number, string> = { 2: 'lg:grid-cols-2', 3: 'lg:grid-cols-3' };

export default function DashboardPage() {
  const { data: session } = useSession();
  const userName = session?.user?.name ?? 'there';
  const gymName = session?.user?.gymName ?? 'your gym';
  const subRole = session?.user?.staffSubRole ?? 'owner';
  const greeting = greetingFor(new Date().getHours());
  const { isEnabled } = useStaffFlags();

  const finance = isEnabled(K.GYM_FINANCE);
  const attendance = isEnabled(K.GYM_ATTENDANCE);
  const classesOn = isEnabled(K.CLASSES);
  const coachBoard = isEnabled(K.COACH_LEADERBOARD);

  const { data: members = [] } = useMembers();
  const { data: trainers = [] } = useTrainers();
  const { data: classes = [] } = useClasses(classesOn);
  const { data: checkIns = [] } = useCheckIns(attendance);
  const { data: revenueData } = useRevenue(finance);
  const { data: trainerRankings = [] } = useTrainerRankings(coachBoard);
  const monthlyRevenue = revenueData?.monthlyRevenue ?? [];
  const recentPayments = revenueData?.recentPayments ?? [];

  const now = new Date();
  const revenue = revenueStats(monthlyRevenue);
  const checkInsToday = checkInStats(checkIns, now);
  const pendingTrainers = trainers.filter((t) => t.status === 'pending_approval').length;
  const activeMembers = members.filter((m) => m.status === 'active').length;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold font-heading">{greeting}, {userName}!</h1>
        <p className="text-muted-foreground mt-1">Here is what is happening at {gymName} today.</p>
      </div>

      <Row
        cols={KPI_COLS}
        cells={[
          <KpiCard key="m" value={activeMembers.toString()} label="Active Members" change={memberGrowth(members, now)} changeSuffix="new vs last month" icon={<Users className="h-5 w-5" />} variant="success" />,
          finance && <KpiCard key="r" value={formatCurrency(revenue.current)} label="Monthly Revenue" change={revenue.change} icon={<DollarSign className="h-5 w-5" />} variant="info" />,
          attendance && <KpiCard key="c" value={checkInsToday.today.toString()} label="Today's Check-ins" change={checkInsToday.change} changeSuffix="vs yesterday" icon={<DoorOpen className="h-5 w-5" />} variant="default" />,
          <KpiCard key="t" value={trainers.filter((t) => t.status === 'active').length.toString()} label="Active Trainers" icon={<Dumbbell className="h-5 w-5" />} variant="default" />,
        ]}
      />

      <PtDeskSection isEnabled={isEnabled} canSeeOwnerDashboard={subRole === 'owner' || subRole === 'manager'} />

      <Row
        cols={PAIR_COLS}
        cells={[
          finance && <RevenueTrendChart key="rev" data={monthlyRevenue} subtitle="Last 6 months (EGP)" gradientId="colorRev" />,
          classesOn && <TodaySchedule key="sch" classes={classes.slice(0, 4)} />,
        ]}
      />

      <QuickActions isAllowed={(href) => isPathEnabled(href, STAFF_ROUTE_FLAGS, isEnabled)} />

      <Row
        cols={TRIO_COLS}
        cells={[
          classesOn && <TopClasses key="top" classes={classes} />,
          <NeedsAttention key="att" items={attentionItems(pendingTrainers, members, isEnabled(K.GYM_MEMBERSHIPS))} />,
          coachBoard && <CoachOfMonth key="coach" top={trainerRankings[0]} />,
        ]}
      />

      {finance && <RecentActivity payments={recentPayments} />}
    </div>
  );
}
