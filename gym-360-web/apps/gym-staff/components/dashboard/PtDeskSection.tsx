'use client';

import Link from 'next/link';
import type { ReactNode } from 'react';
import { FEATURE_FLAG_KEYS as K } from '@gym360/ui';
import { AlertTriangle, Gift, LineChart, ThumbsUp } from 'lucide-react';
import { useNotConvertedTrials, usePendingInterests, usePtDashboard, useSessionsForReview } from '@/lib/hooks/usePt';
import { usePtText } from '@/lib/pt-text';

interface DeskCardProps {
  href: string;
  icon: ReactNode;
  title: string;
  value: string;
  hint?: string;
}

function DeskCard({ href, icon, title, value, hint }: DeskCardProps) {
  return (
    <Link href={href} className="flex items-center gap-4 rounded-xl border bg-card p-5 shadow-sm hover:shadow-md hover:border-primary/30 transition-all">
      <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary">{icon}</div>
      <div className="min-w-0">
        <div className="text-2xl font-bold font-heading">{value}</div>
        <div className="text-sm font-medium">{title}</div>
        {hint && <div className="text-xs text-muted-foreground truncate">{hint}</div>}
      </div>
    </Link>
  );
}

interface Props {
  isEnabled: (key: string) => boolean;
  canSeeOwnerDashboard: boolean;
}

/** PT home for the desk/owner: interested trainees, not-converted trials, disputed sessions and the 30-day PT summary. */
export function PtDeskSection({ isEnabled, canSeeOwnerDashboard }: Props) {
  const t = usePtText();
  const showInterests = isEnabled(K.PT_PACKAGE_INTEREST);
  const showTrials = isEnabled(K.PT_TRIAL_END_ALERTS);
  const showReview = isEnabled(K.PT_DUAL_SESSION_LOGGING);
  const showDashboard = canSeeOwnerDashboard && isEnabled(K.PT_OWNER_DASHBOARD);
  const { data: interests = [] } = usePendingInterests(showInterests);
  const { data: trials = [] } = useNotConvertedTrials(showTrials);
  const { data: disputed = [] } = useSessionsForReview(showReview);
  const { data: dashboard } = usePtDashboard(30, showDashboard);

  const showAny = [showInterests, showTrials, showReview, showDashboard].some(Boolean);
  if (!showAny) return null;
  const totals = dashboard?.totals;

  return (
    <section className="space-y-3">
      <h2 className="text-lg font-semibold">{t.deskTitle}</h2>
      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4">
        {showInterests && (
          <DeskCard href="/pt/interests" icon={<ThumbsUp className="h-5 w-5" />} title={t.navInterests} value={String(interests.length)} hint={t.interestsDescription} />
        )}
        {showTrials && (
          <DeskCard href="/pt/trials" icon={<Gift className="h-5 w-5" />} title={t.navTrials} value={String(trials.length)} hint={t.trialsDescription} />
        )}
        {showReview && (
          <DeskCard href="/pt/review" icon={<AlertTriangle className="h-5 w-5" />} title={t.navReview} value={String(disputed.length)} />
        )}
        {showDashboard && (
          <DeskCard
            href="/pt/dashboard"
            icon={<LineChart className="h-5 w-5" />}
            title={`${t.conversion} · ${t.period(30)}`}
            value={`${Math.round(totals?.conversionRate ?? 0)}%`}
            hint={`${t.renewals}: ${totals?.renewals ?? 0} · ${t.sessionsLogged}: ${totals?.sessionsLogged ?? 0}`}
          />
        )}
      </div>
    </section>
  );
}
