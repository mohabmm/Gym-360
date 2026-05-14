'use client';

import { useSession } from 'next-auth/react';
import { KpiCard, ChartContainer, StatusBadge, formatCurrency, getRelativeTime, formatTime } from '@gymapp/ui';
import { Users, DollarSign, DoorOpen, Dumbbell, UserPlus, CreditCard, CalendarDays, Megaphone, Trophy, Clock } from 'lucide-react';
import { AreaChart, Area, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip } from 'recharts';
import { useMembers } from '@/lib/hooks/useMembers';
import { useTrainers } from '@/lib/hooks/useTrainers';
import { useClasses } from '@/lib/hooks/useClasses';
import { useCheckIns } from '@/lib/hooks/useCheckIns';
import { useRevenue } from '@/lib/hooks/useRevenue';
import { useTrainerRankings } from '@/lib/hooks/useGamification';
import Link from 'next/link';

export default function DashboardPage() {
  const { data: session } = useSession();
  const userName = session?.user?.name ?? 'there';
  const gymName = session?.user?.gymName ?? 'your gym';
  const currentHour = new Date().getHours();
  const greeting = currentHour < 12 ? 'Good morning' : currentHour < 18 ? 'Good afternoon' : 'Good evening';

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

  const currentMonthRevenue = monthlyRevenue[monthlyRevenue.length - 1]?.revenue ?? 0;
  const prevMonthRevenue = monthlyRevenue[monthlyRevenue.length - 2]?.revenue ?? 0;
  const revenueChange =
    prevMonthRevenue > 0
      ? Number((((currentMonthRevenue - prevMonthRevenue) / prevMonthRevenue) * 100).toFixed(1))
      : undefined;

  const now = new Date();
  const currentMonthIdx = now.getMonth();
  const currentYear = now.getFullYear();
  const prevMonthDate = new Date(currentYear, currentMonthIdx - 1, 1);
  const prevMonthIdx = prevMonthDate.getMonth();
  const prevMonthYear = prevMonthDate.getFullYear();

  const inMonth = (iso: string | undefined, m: number, y: number) => {
    if (!iso) return false;
    const d = new Date(iso);
    return !isNaN(d.getTime()) && d.getMonth() === m && d.getFullYear() === y;
  };

  const newMembersThisMonth = members.filter((m) => inMonth(m.joinDate, currentMonthIdx, currentYear)).length;
  const newMembersPrevMonth = members.filter((m) => inMonth(m.joinDate, prevMonthIdx, prevMonthYear)).length;
  const memberChange =
    newMembersPrevMonth > 0
      ? Number((((newMembersThisMonth - newMembersPrevMonth) / newMembersPrevMonth) * 100).toFixed(1))
      : undefined;

  const sameDay = (iso: string | undefined, ref: Date) => {
    if (!iso) return false;
    const d = new Date(iso);
    return (
      !isNaN(d.getTime()) &&
      d.getFullYear() === ref.getFullYear() &&
      d.getMonth() === ref.getMonth() &&
      d.getDate() === ref.getDate()
    );
  };
  const yesterday = new Date(currentYear, currentMonthIdx, now.getDate() - 1);
  const todayCheckIns = checkIns.filter((c) => sameDay(c.checkInTime, now)).length;
  const yesterdayCheckIns = checkIns.filter((c) => sameDay(c.checkInTime, yesterday)).length;
  const checkInChange =
    yesterdayCheckIns > 0
      ? Number((((todayCheckIns - yesterdayCheckIns) / yesterdayCheckIns) * 100).toFixed(1))
      : undefined;

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
        <KpiCard value={formatCurrency(currentMonthRevenue)} label="Monthly Revenue" change={revenueChange} icon={<DollarSign className="h-5 w-5" />} variant="info" />
        <KpiCard value={todayCheckIns.toString()} label="Today's Check-ins" change={checkInChange} changeSuffix="vs yesterday" icon={<DoorOpen className="h-5 w-5" />} variant="default" />
        <KpiCard value={trainers.filter((t) => t.status === 'active').length.toString()} label="Active Trainers" icon={<Dumbbell className="h-5 w-5" />} variant="default" />
      </div>

      {/* Charts Row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <ChartContainer title="Revenue Trend" subtitle="Last 6 months (EGP)">
          <AreaChart data={monthlyRevenue}>
            <defs>
              <linearGradient id="colorRev" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor="#C41E3A" stopOpacity={0.2} />
                <stop offset="95%" stopColor="#C41E3A" stopOpacity={0} />
              </linearGradient>
            </defs>
            <CartesianGrid strokeDasharray="3 3" stroke="#E5E7EB" />
            <XAxis dataKey="month" tick={{ fontSize: 12 }} stroke="#9CA3AF" />
            <YAxis tick={{ fontSize: 12 }} stroke="#9CA3AF" tickFormatter={(v: number) => `${(v / 1000).toFixed(0)}K`} />
            <Tooltip formatter={(value: number) => formatCurrency(value)} />
            <Area type="monotone" dataKey="revenue" stroke="#C41E3A" strokeWidth={2} fill="url(#colorRev)" />
          </AreaChart>
        </ChartContainer>

        {/* Today's Schedule */}
        <div className="rounded-xl border bg-card p-6 shadow-sm">
          <h3 className="text-sm font-semibold mb-4 flex items-center gap-2">
            <Clock className="h-4 w-4 text-primary" />
            Today&apos;s Schedule
          </h3>
          <div className="space-y-3">
            {todaySchedule.map((cls) => (
              <div key={cls.id} className="flex items-center gap-3 py-2 border-b last:border-0">
                <div className="w-1 h-10 rounded-full" style={{ backgroundColor: cls.color ?? '#C41E3A' }} />
                <div className="flex-1">
                  <p className="text-sm font-medium">{cls.name}</p>
                  <p className="text-xs text-muted-foreground">
                    {formatTime(cls.startTime)} - {formatTime(cls.endTime)} &middot; {cls.trainerName}
                  </p>
                </div>
                <div className="text-right">
                  <p className="text-sm font-medium">{cls.enrolledCount}/{cls.capacity}</p>
                  <StatusBadge variant={cls.status === 'full' ? 'warning' : 'active'} label={cls.status} className="mt-0.5" />
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Quick Actions */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <Link href="/members" className="flex flex-col items-center gap-2 rounded-xl border bg-card p-4 shadow-sm hover:shadow-md hover:border-primary/30 transition-all">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary"><UserPlus className="h-5 w-5" /></div>
          <span className="text-sm font-medium">Add Member</span>
        </Link>
        <Link href="/billing" className="flex flex-col items-center gap-2 rounded-xl border bg-card p-4 shadow-sm hover:shadow-md hover:border-primary/30 transition-all">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary"><CreditCard className="h-5 w-5" /></div>
          <span className="text-sm font-medium">Record Payment</span>
        </Link>
        <Link href="/classes" className="flex flex-col items-center gap-2 rounded-xl border bg-card p-4 shadow-sm hover:shadow-md hover:border-primary/30 transition-all">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary"><CalendarDays className="h-5 w-5" /></div>
          <span className="text-sm font-medium">Create Class</span>
        </Link>
        <Link href="/announcements" className="flex flex-col items-center gap-2 rounded-xl border bg-card p-4 shadow-sm hover:shadow-md hover:border-primary/30 transition-all">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary"><Megaphone className="h-5 w-5" /></div>
          <span className="text-sm font-medium">Send Announcement</span>
        </Link>
      </div>

      {/* Bottom Row */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Top Classes */}
        <div className="rounded-xl border bg-card p-6 shadow-sm">
          <h3 className="text-sm font-semibold mb-4">Top Classes</h3>
          <div className="space-y-3">
            {classes.sort((a, b) => b.enrolledCount - a.enrolledCount).slice(0, 4).map((cls) => (
              <div key={cls.id} className="flex items-center justify-between py-2 border-b last:border-0">
                <div>
                  <p className="text-sm font-medium">{cls.name}</p>
                  <p className="text-xs text-muted-foreground">{cls.trainerName}</p>
                </div>
                <span className="text-sm font-medium">{cls.enrolledCount}/{cls.capacity}</span>
              </div>
            ))}
          </div>
        </div>

        {/* Needs Attention — driven by live counts; suppressed when nothing to show. */}
        {(() => {
          const pendingTrainers = trainers.filter((t) => t.status === 'pending_approval').length;
          const expiredMembers = members.filter((m) => m.status === 'expired').length;
          const frozenMembers = members.filter((m) => m.status === 'frozen').length;
          const items: { color: string; title: string; subtitle: string }[] = [];
          if (pendingTrainers > 0) {
            items.push({
              color: 'bg-amber-500',
              title: 'Trainer Pending Approval',
              subtitle: `${pendingTrainers} trainer${pendingTrainers === 1 ? '' : 's'} awaiting review`,
            });
          }
          if (expiredMembers > 0) {
            items.push({
              color: 'bg-red-500',
              title: 'Expired Memberships',
              subtitle: `${expiredMembers} member${expiredMembers === 1 ? '' : 's'} with expired memberships`,
            });
          }
          if (frozenMembers > 0) {
            items.push({
              color: 'bg-blue-500',
              title: 'Frozen Members',
              subtitle: `${frozenMembers} member account${frozenMembers === 1 ? '' : 's'} frozen`,
            });
          }
          return (
            <div className="rounded-xl border bg-card p-6 shadow-sm">
              <h3 className="text-sm font-semibold mb-4">Needs Attention</h3>
              {items.length === 0 ? (
                <p className="text-xs text-muted-foreground py-2">All caught up — nothing needs your attention.</p>
              ) : (
                <div className="space-y-3">
                  {items.map((item, idx) => (
                    <div
                      key={item.title}
                      className={`flex items-center gap-3 py-2 ${idx < items.length - 1 ? 'border-b' : ''}`}
                    >
                      <div className={`h-2 w-2 rounded-full ${item.color}`} />
                      <div className="flex-1">
                        <p className="text-sm font-medium">{item.title}</p>
                        <p className="text-xs text-muted-foreground">{item.subtitle}</p>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          );
        })()}

        {/* Coach of Month */}
        <div className="rounded-xl border bg-card p-6 shadow-sm">
          <h3 className="text-sm font-semibold mb-4 flex items-center gap-2">
            <Trophy className="h-4 w-4 text-[#D4AF37]" />
            Coach of the Month
          </h3>
          {trainerRankings[0] && (
            <div className="text-center py-4">
              <div className="h-16 w-16 rounded-full bg-[#D4AF37]/10 flex items-center justify-center text-[#D4AF37] font-heading font-bold text-xl mx-auto mb-3">
                {trainerRankings[0].trainerName?.split(' ').slice(-1)[0]?.[0]}
              </div>
              <p className="font-semibold">{trainerRankings[0].trainerName}</p>
              <p className="text-sm text-muted-foreground mt-1">Score: {trainerRankings[0].monthlyScore}/100</p>
              <div className="flex items-center justify-center gap-1 mt-2">
                {Array.from({ length: 5 }).map((_, i) => (
                  <svg key={i} className={`h-4 w-4 ${i < Math.round(trainerRankings[0]!.rating) ? 'text-[#D4AF37]' : 'text-gray-300'}`} fill="currentColor" viewBox="0 0 20 20">
                    <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                  </svg>
                ))}
                <span className="text-sm ml-1">{trainerRankings[0].rating}</span>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Recent Activity */}
      <div className="rounded-xl border bg-card p-6 shadow-sm">
        <h3 className="text-sm font-semibold mb-4">Recent Activity</h3>
        <div className="space-y-3">
          {recentPayments.slice(0, 5).map((payment) => (
            <div key={payment.id} className="flex items-center justify-between py-2 border-b last:border-0">
              <div className="flex items-center gap-3">
                <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary/10 text-primary">
                  <CreditCard className="h-4 w-4" />
                </div>
                <div>
                  <p className="text-sm font-medium">{payment.memberName} - {payment.type?.replace(/_/g, ' ')}</p>
                  <p className="text-xs text-muted-foreground">{getRelativeTime(payment.createdAt)}</p>
                </div>
              </div>
              <div className="text-right">
                <p className="text-sm font-medium">{formatCurrency(payment.amount)}</p>
                <StatusBadge variant={payment.status} className="mt-0.5" />
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
