import { StatusBadge } from '@gym360/ui';
import { Star, Medal } from 'lucide-react';
import type { LeaderboardEntry, TrainerRankingEntry, Testimonial } from '@/lib/hooks/useGamification';

const PODIUM_CLASS: Record<number, string> = {
  1: 'bg-[#D4AF37] text-white',
  2: 'bg-gray-300 text-gray-800',
};

function RankBadge({ rank }: { rank: number }) {
  if (rank > 3) return <span className="text-muted-foreground font-medium">{rank}</span>;
  return (
    <span className={`inline-flex h-7 w-7 items-center justify-center rounded-full text-xs font-bold ${PODIUM_CLASS[rank] ?? 'bg-amber-700 text-white'}`}>
      {rank}
    </span>
  );
}

function RankChange({ change: raw }: { change?: number | null }) {
  const change = raw ?? 0;
  if (change > 0) return <span className="text-emerald-500 text-xs font-medium">+{change}</span>;
  if (change < 0) return <span className="text-red-500 text-xs font-medium">{change}</span>;
  return <span className="text-muted-foreground text-xs">-</span>;
}

export function MemberLeaderboardTab({ leaderboard }: { leaderboard: LeaderboardEntry[] }) {
  return (
    <div className="rounded-xl border bg-card shadow-sm overflow-hidden">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b bg-muted/50">
            <th className="text-center py-2.5 px-4 font-semibold text-muted-foreground w-16">Rank</th>
            <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground">Member</th>
            <th className="text-right py-2.5 px-4 font-semibold text-muted-foreground">Score</th>
            <th className="text-right py-2.5 px-4 font-semibold text-muted-foreground">Workouts</th>
            <th className="text-right py-2.5 px-4 font-semibold text-muted-foreground">Streak</th>
            <th className="text-center py-2.5 px-4 font-semibold text-muted-foreground">Change</th>
          </tr>
        </thead>
        <tbody>
          {leaderboard.length === 0 && (
            <tr className="border-b last:border-0">
              <td colSpan={6} className="py-6 px-4 text-center text-muted-foreground text-sm">
                No leaderboard entries yet.
              </td>
            </tr>
          )}
          {leaderboard.map((entry) => (
            <tr key={entry.memberId} className="border-b last:border-0 hover:bg-muted/50">
              <td className="py-3 px-4 text-center">
                <RankBadge rank={entry.rank} />
              </td>
              <td className="py-3 px-4">
                <p className="font-medium">{entry.memberName}</p>
                <p className="text-xs text-muted-foreground">{entry.memberNameAr}</p>
              </td>
              <td className="py-3 px-4 text-right font-bold text-primary">{entry.score}</td>
              <td className="py-3 px-4 text-right">{entry.workoutsCompleted}</td>
              <td className="py-3 px-4 text-right">{entry.streakDays} days</td>
              <td className="py-3 px-4 text-center">
                <RankChange change={entry.change} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function CoachRankingsEmpty() {
  return (
      <div className="rounded-xl border bg-card p-6 shadow-sm text-center md:col-span-3">
        <p className="text-sm text-muted-foreground">No coach rankings yet.</p>
        <div className="grid grid-cols-2 gap-2 mt-4 text-sm max-w-xs mx-auto">
          <div className="bg-muted/50 rounded-lg p-2">
            <p className="font-bold">—</p>
            <p className="text-[10px] text-muted-foreground">Score</p>
          </div>
          <div className="bg-muted/50 rounded-lg p-2">
            <p className="font-bold">—</p>
            <p className="text-[10px] text-muted-foreground">Rating</p>
          </div>
        </div>
      </div>
  );
}

function CoachCard({ trainer }: { trainer: TrainerRankingEntry }) {
  return (
    <div className="rounded-xl border bg-card p-6 shadow-sm text-center">
      <div className={`inline-flex h-14 w-14 items-center justify-center rounded-full mb-3 text-xl font-bold ${trainer.rank === 1 ? 'bg-[#D4AF37]/10 text-[#D4AF37]' : 'bg-primary/10 text-primary'}`}>
        {trainer.trainerName?.split(' ').slice(-1)[0]?.[0]}
      </div>
      {trainer.rank === 1 && <Medal className="h-5 w-5 text-[#D4AF37] mx-auto mb-1" />}
      <h3 className="font-semibold">{trainer.trainerName}</h3>
      <p className="text-xs text-muted-foreground">{trainer.trainerNameAr}</p>
      <div className="grid grid-cols-2 gap-2 mt-4 text-sm">
        <div className="bg-muted/50 rounded-lg p-2">
          <p className="font-bold">{trainer.monthlyScore}</p>
          <p className="text-[10px] text-muted-foreground">Score</p>
        </div>
        <div className="bg-muted/50 rounded-lg p-2">
          <p className="font-bold flex items-center justify-center gap-1"><Star className="h-3 w-3 text-[#D4AF37]" fill="#D4AF37" />{trainer.rating}</p>
          <p className="text-[10px] text-muted-foreground">Rating</p>
        </div>
        <div className="bg-muted/50 rounded-lg p-2">
          <p className="font-bold">{trainer.sessionsCompleted}</p>
          <p className="text-[10px] text-muted-foreground">Sessions</p>
        </div>
        <div className="bg-muted/50 rounded-lg p-2">
          <p className="font-bold">{trainer.totalRatings}</p>
          <p className="text-[10px] text-muted-foreground">Reviews</p>
        </div>
      </div>
    </div>
  );
}

export function CoachRankingsTab({ rankings }: { rankings: TrainerRankingEntry[] }) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
      {rankings.length === 0 && <CoachRankingsEmpty />}
      {rankings.map((trainer) => <CoachCard key={trainer.trainerId} trainer={trainer} />)}
    </div>
  );
}

interface TestimonialCardProps {
  testimonial: Testimonial;
  onApprove: (id: string) => void;
  onReject: (id: string) => void;
  moderating: boolean;
}

function TestimonialCard({ testimonial: t, onApprove, onReject, moderating }: TestimonialCardProps) {
  return (
    <div className="rounded-xl border bg-card p-6 shadow-sm">
      <div className="flex items-start justify-between">
        <div>
          <p className="font-medium">{t.memberName} <span className="text-muted-foreground font-normal">about</span> {t.trainerName}</p>
          <div className="flex items-center gap-1 mt-1">
            {Array.from({ length: 5 }).map((_, i) => (
              <Star key={i} className={`h-3.5 w-3.5 ${i < t.rating ? 'text-[#D4AF37]' : 'text-gray-300'}`} fill={i < t.rating ? '#D4AF37' : 'none'} />
            ))}
          </div>
        </div>
        <StatusBadge variant={t.status} />
      </div>
      <p className="text-sm mt-3 text-muted-foreground">&quot;{t.content}&quot;</p>
      {t.status === 'pending' && (
        <div className="flex gap-2 mt-4">
          <button onClick={() => onReject(t.id)} className="rounded-lg border border-red-200 bg-red-50 px-3 py-1.5 text-xs font-medium text-red-600 hover:bg-red-100">Reject</button>
          <button onClick={() => onApprove(t.id)} disabled={moderating} className="rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-emerald-700 disabled:opacity-50">Approve</button>
        </div>
      )}
    </div>
  );
}

interface TestimonialsTabProps extends Omit<TestimonialCardProps, 'testimonial'> {
  testimonials: Testimonial[];
}

export function TestimonialsTab({ testimonials, ...actions }: TestimonialsTabProps) {
  return (
    <div className="space-y-4">
      {testimonials.length === 0 && (
        <div className="rounded-xl border bg-card p-8 text-center text-sm text-muted-foreground">
          No testimonials yet
        </div>
      )}
      {testimonials.map((t) => <TestimonialCard key={t.id} testimonial={t} {...actions} />)}
    </div>
  );
}
