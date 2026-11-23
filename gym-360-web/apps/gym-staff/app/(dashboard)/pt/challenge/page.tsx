'use client';

import Link from 'next/link';
import { useState } from 'react';
import { toast } from 'sonner';
import { PageHeader, formatDateTime } from '@gym360/ui';
import { useChallengeAwards, useSetAwardDelivered } from '@/lib/hooks/useChallenge';
import { rewardText, type ChallengeAward } from '@/lib/challenge';
import { useChallengeText, type ChallengeText } from '@/lib/challenge-text';
import { PtCell, PtState, PtTable } from '@/components/pt/PtTable';

/** mem.first_month_challenge — the desk's "earned the reward" list with a "reward delivered" tick. */
export default function ChallengeAwardsPage() {
  const t = useChallengeText();
  const [delivered, setDelivered] = useState(false);
  const { data: awards = [], isLoading, isError } = useChallengeAwards(delivered);

  return (
    <div className="space-y-6">
      <PageHeader title={t.title} description={t.description} />
      <div className="inline-flex rounded-lg border bg-card p-1" role="tablist">
        {[false, true].map((value) => (
          <button key={String(value)} role="tab" aria-selected={delivered === value} onClick={() => setDelivered(value)}
            className={`rounded-md px-3 py-1.5 text-sm ${delivered === value ? 'bg-primary text-primary-foreground' : 'hover:bg-muted'}`}>
            {value ? t.delivered : t.toHandOver}
          </button>
        ))}
      </div>
      {isLoading && <PtState text="…" />}
      {isError && <PtState text={t.loadError} />}
      {!isLoading && !isError && awards.length === 0 && <PtState text={delivered ? t.emptyDelivered : t.emptyPending} />}
      {awards.length > 0 && (
        <PtTable head={[t.member, t.coach, t.workouts, t.reachedOn, t.reward, t.deliveredCol]}>
          {awards.map((a) => <AwardRow key={a.awardId} award={a} t={t} />)}
        </PtTable>
      )}
    </div>
  );
}

function AwardRow({ award, t }: { award: ChallengeAward; t: ChallengeText }) {
  const setDelivered = useSetAwardDelivered();
  const isDelivered = !!award.deliveredAt;
  const toggle = () =>
    setDelivered.mutate({ awardId: award.awardId, delivered: !isDelivered }, { onError: () => toast.error(t.saveError) });

  return (
    <tr>
      <PtCell className="font-medium">
        <Link href={`/members/${award.traineeId}`} className="hover:underline">{award.traineeName}</Link>
      </PtCell>
      <PtCell>{award.trainerName ?? '—'}</PtCell>
      <PtCell>{`${award.count} / ${award.target}`}</PtCell>
      <PtCell>{formatDateTime(award.reachedAt)}</PtCell>
      <PtCell>{rewardText(award, t.isArabic)}</PtCell>
      <PtCell>
        <label className="inline-flex items-center gap-2 text-sm">
          <input type="checkbox" checked={isDelivered} disabled={setDelivered.isPending} onChange={toggle} aria-label={t.deliveredCol} />
          {isDelivered && award.deliveredByName && <span className="text-xs text-muted-foreground">{t.deliveredBy(award.deliveredByName)}</span>}
        </label>
      </PtCell>
    </tr>
  );
}
