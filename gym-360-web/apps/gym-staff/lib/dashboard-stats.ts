import type { MemberListItem, MemberCheckIn } from '@gym360/ui';

export function greetingFor(hour: number) {
  if (hour < 12) return 'Good morning';
  return hour < 18 ? 'Good afternoon' : 'Good evening';
}

/** Percentage change rounded to one decimal; undefined when there is no baseline. */
export function percentChange(current: number, previous: number): number | undefined {
  return previous > 0 ? Number((((current - previous) / previous) * 100).toFixed(1)) : undefined;
}

function parseDate(iso: string | undefined): Date | null {
  if (!iso) return null;
  const d = new Date(iso);
  return isNaN(d.getTime()) ? null : d;
}

export function inMonth(iso: string | undefined, m: number, y: number) {
  const d = parseDate(iso);
  return !!d && d.getMonth() === m && d.getFullYear() === y;
}

export function sameDay(iso: string | undefined, ref: Date) {
  const d = parseDate(iso);
  return !!d && d.toDateString() === ref.toDateString();
}

const countWhere = <T>(items: T[], pred: (item: T) => boolean) => items.filter(pred).length;

export function memberGrowth(members: MemberListItem[], now: Date) {
  const prevMonthDate = new Date(now.getFullYear(), now.getMonth() - 1, 1);
  const thisMonth = countWhere(members, (m) => inMonth(m.joinDate, now.getMonth(), now.getFullYear()));
  const prevMonth = countWhere(members, (m) => inMonth(m.joinDate, prevMonthDate.getMonth(), prevMonthDate.getFullYear()));
  return percentChange(thisMonth, prevMonth);
}

export function checkInStats(checkIns: MemberCheckIn[], now: Date) {
  const yesterday = new Date(now.getFullYear(), now.getMonth(), now.getDate() - 1);
  const today = countWhere(checkIns, (c) => sameDay(c.checkInTime, now));
  const prev = countWhere(checkIns, (c) => sameDay(c.checkInTime, yesterday));
  return { today, change: percentChange(today, prev) };
}

export function revenueStats(monthlyRevenue: { revenue: number }[]) {
  const current = monthlyRevenue[monthlyRevenue.length - 1]?.revenue ?? 0;
  const prev = monthlyRevenue[monthlyRevenue.length - 2]?.revenue ?? 0;
  return { current, change: percentChange(current, prev) };
}

export interface AttentionItem {
  color: string;
  title: string;
  subtitle: string;
}

const plural = (n: number, word: string) => `${n} ${word}${n === 1 ? '' : 's'}`;

/** "Needs Attention" rows driven by live counts; rows with a zero count are omitted. */
export function attentionItems(pendingTrainers: number, members: MemberListItem[]): AttentionItem[] {
  const expiredMembers = countWhere(members, (m) => m.status === 'expired');
  const frozenMembers = countWhere(members, (m) => m.status === 'frozen');
  const candidates: [number, AttentionItem][] = [
    [pendingTrainers, { color: 'bg-amber-500', title: 'Trainer Pending Approval', subtitle: `${plural(pendingTrainers, 'trainer')} awaiting review` }],
    [expiredMembers, { color: 'bg-red-500', title: 'Expired Memberships', subtitle: `${plural(expiredMembers, 'member')} with expired memberships` }],
    [frozenMembers, { color: 'bg-blue-500', title: 'Frozen Members', subtitle: `${plural(frozenMembers, 'member account')} frozen` }],
  ];
  return candidates.filter(([count]) => count > 0).map(([, item]) => item);
}
