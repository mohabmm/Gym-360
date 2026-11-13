'use client';

import Link from 'next/link';
import { PageHeader, StatusBadge, formatDate } from '@gym360/ui';
import { useNotConvertedTrials } from '@/lib/hooks/usePt';
import { usePtText } from '@/lib/pt-text';
import { PtCell, PtState, PtTable } from '@/components/pt/PtTable';

/** pt.trial_end_alerts — the desk's "finished trial, not converted" list. */
export default function NotConvertedTrialsPage() {
  const t = usePtText();
  const { data: trials = [], isLoading, isError } = useNotConvertedTrials();

  return (
    <div className="space-y-6">
      <PageHeader title={t.trialsTitle} description={t.trialsDescription} />
      {isLoading && <PtState text="…" />}
      {isError && <PtState text={t.loadError} />}
      {!isLoading && !isError && trials.length === 0 && <PtState text={t.trialsEmpty} />}
      {trials.length > 0 && (
        <PtTable head={[t.member, t.coach, t.finishedOn, '', '']}>
          {trials.map((trial) => (
            <tr key={trial.packageId}>
              <PtCell className="font-medium">{trial.memberName}</PtCell>
              <PtCell>{trial.trainerName}</PtCell>
              <PtCell>
                {formatDate(trial.finishedAt)}
                <span className="block text-xs text-muted-foreground">{t.daysAgo(trial.daysSinceFinished)}</span>
              </PtCell>
              <PtCell>
                <StatusBadge
                  variant={trial.conversionWindowClosed ? 'inactive' : 'warning'}
                  label={trial.conversionWindowClosed ? t.windowClosed : t.windowOpen}
                />
              </PtCell>
              <PtCell className="text-end">
                <Link href={`/members/${trial.memberId}`} className="text-sm font-medium text-primary hover:underline">
                  {t.openProfile}
                </Link>
              </PtCell>
            </tr>
          ))}
        </PtTable>
      )}
    </div>
  );
}
