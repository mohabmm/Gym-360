'use client';

import { KpiCard } from '@gym360/ui';
import { CalendarCheck, ClipboardList, Trophy, Users } from 'lucide-react';
import { usePlanSummary } from '@/lib/hooks/useChallenge';
import { useChallengeText } from '@/lib/challenge-text';

/**
 * pt.owner_dashboard + pt.training_plan — members with a plan, this month's plan workouts and,
 * with mem.first_month_challenge on, who reached the challenge. Nothing when plans are off.
 */
export function PlanSummarySection({ enabled }: { enabled: boolean }) {
  const t = useChallengeText();
  const { data } = usePlanSummary(enabled);
  if (!enabled || !data) return null;

  return (
    <section className="space-y-3">
      <h2 className="text-lg font-semibold">{t.plansTitle}</h2>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <KpiCard value={String(data.activePlans)} label={t.activePlans} icon={<ClipboardList className="h-5 w-5" />} variant="info" />
        <KpiCard value={String(data.workoutsThisMonth)} label={t.workoutsThisMonth} icon={<CalendarCheck className="h-5 w-5" />} variant="default" />
        <KpiCard value={String(data.membersTrainingThisMonth)} label={t.membersTraining} icon={<Users className="h-5 w-5" />} variant="default" />
        {data.challengeEnabled && (
          <KpiCard value={String(data.challengeReached)} label={`${t.challengeReached} · ${t.awaiting(data.challengeAwaitingDelivery)}`}
            icon={<Trophy className="h-5 w-5" />} variant="success" />
        )}
      </div>
    </section>
  );
}
