import Link from 'next/link';
import { StatusBadge, formatCurrency, getRelativeTime, formatTime } from '@gym360/ui';
import type { GymClass, PaymentListItem } from '@gym360/ui';
import { UserPlus, CreditCard, CalendarDays, Megaphone, Trophy, Clock } from 'lucide-react';
import type { TrainerRankingEntry } from '@/lib/hooks/useGamification';
import type { AttentionItem } from '@/lib/dashboard-stats';

export function TodaySchedule({ classes }: { classes: GymClass[] }) {
  return (
    <div className="rounded-xl border bg-card p-6 shadow-sm">
      <h3 className="text-sm font-semibold mb-4 flex items-center gap-2">
        <Clock className="h-4 w-4 text-primary" />
        Today&apos;s Schedule
      </h3>
      <div className="space-y-3">
        {classes.map((cls) => (
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
  );
}

const QUICK_ACTIONS = [
  { href: '/members', label: 'Add Member', Icon: UserPlus },
  { href: '/billing', label: 'Record Payment', Icon: CreditCard },
  { href: '/classes', label: 'Create Class', Icon: CalendarDays },
  { href: '/announcements', label: 'Send Announcement', Icon: Megaphone },
];

export function QuickActions() {
  return (
    <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
      {QUICK_ACTIONS.map(({ href, label, Icon }) => (
        <Link key={href} href={href} className="flex flex-col items-center gap-2 rounded-xl border bg-card p-4 shadow-sm hover:shadow-md hover:border-primary/30 transition-all">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary"><Icon className="h-5 w-5" /></div>
          <span className="text-sm font-medium">{label}</span>
        </Link>
      ))}
    </div>
  );
}

export function TopClasses({ classes }: { classes: GymClass[] }) {
  return (
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
  );
}

export function NeedsAttention({ items }: { items: AttentionItem[] }) {
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
}

function RatingStars({ rating }: { rating: number }) {
  return (
    <div className="flex items-center justify-center gap-1 mt-2">
      {Array.from({ length: 5 }).map((_, i) => (
        <svg key={i} className={`h-4 w-4 ${i < Math.round(rating) ? 'text-[#D4AF37]' : 'text-gray-300'}`} fill="currentColor" viewBox="0 0 20 20">
          <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
        </svg>
      ))}
      <span className="text-sm ml-1">{rating}</span>
    </div>
  );
}

export function CoachOfMonth({ top }: { top: TrainerRankingEntry | undefined }) {
  return (
    <div className="rounded-xl border bg-card p-6 shadow-sm">
      <h3 className="text-sm font-semibold mb-4 flex items-center gap-2">
        <Trophy className="h-4 w-4 text-[#D4AF37]" />
        Coach of the Month
      </h3>
      {top && (
        <div className="text-center py-4">
          <div className="h-16 w-16 rounded-full bg-[#D4AF37]/10 flex items-center justify-center text-[#D4AF37] font-heading font-bold text-xl mx-auto mb-3">
            {top.trainerName?.split(' ').slice(-1)[0]?.[0]}
          </div>
          <p className="font-semibold">{top.trainerName}</p>
          <p className="text-sm text-muted-foreground mt-1">Score: {top.monthlyScore}/100</p>
          <RatingStars rating={top.rating} />
        </div>
      )}
    </div>
  );
}

export function RecentActivity({ payments }: { payments: PaymentListItem[] }) {
  return (
    <div className="rounded-xl border bg-card p-6 shadow-sm">
      <h3 className="text-sm font-semibold mb-4">Recent Activity</h3>
      <div className="space-y-3">
        {payments.slice(0, 5).map((payment) => (
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
  );
}
