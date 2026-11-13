'use client';

import { useState } from 'react';
import { toast } from 'sonner';
import { ConfirmModal, PageHeader, formatDateTime } from '@gym360/ui';
import { useReverseSession, useSessionsForReview } from '@/lib/hooks/usePt';
import { useMembers } from '@/lib/hooks/useMembers';
import { useTrainers } from '@/lib/hooks/useTrainers';
import { usePtText } from '@/lib/pt-text';
import { PtCell, PtState, PtTable } from '@/components/pt/PtTable';

/** pt.dual_session_logging — sessions the trainee disputed; the desk can reverse them. */
export default function DisputedSessionsPage() {
  const t = usePtText();
  const { data: sessions = [], isLoading, isError } = useSessionsForReview();
  const { data: members = [] } = useMembers();
  const { data: trainers = [] } = useTrainers();
  const reverse = useReverseSession();
  const [target, setTarget] = useState<string | null>(null);

  const memberName = new Map(members.map((m) => [m.id, m.name] as const));
  const trainerName = new Map(trainers.map((tr) => [tr.id, tr.name] as const));

  const confirmReverse = () => {
    if (!target) return;
    reverse.mutate(target, {
      onSuccess: () => toast.success(t.reversed),
      onError: (e: unknown) => toast.error(e instanceof Error ? e.message : t.loadError),
      onSettled: () => setTarget(null),
    });
  };

  return (
    <div className="space-y-6">
      <PageHeader title={t.reviewTitle} description={t.reviewDescription} />
      {isLoading && <PtState text="…" />}
      {isError && <PtState text={t.loadError} />}
      {!isLoading && !isError && sessions.length === 0 && <PtState text={t.reviewEmpty} />}
      {sessions.length > 0 && (
        <PtTable head={[t.member, t.coach, t.sessionDate, '']}>
          {sessions.map((s) => (
            <tr key={s.id}>
              <PtCell className="font-medium">{memberName.get(s.memberId) ?? '—'}</PtCell>
              <PtCell>{trainerName.get(s.trainerId) ?? '—'}</PtCell>
              <PtCell>{formatDateTime(s.scheduledAt)}</PtCell>
              <PtCell className="text-end">
                <button
                  onClick={() => setTarget(s.id)}
                  className="rounded-lg border border-red-200 bg-red-50 px-3 py-1.5 text-sm font-medium text-red-700 hover:bg-red-100"
                >
                  {t.reverse}
                </button>
              </PtCell>
            </tr>
          ))}
        </PtTable>
      )}
      <ConfirmModal
        open={!!target}
        onClose={() => !reverse.isPending && setTarget(null)}
        onConfirm={confirmReverse}
        title={t.reverseConfirmTitle}
        description={t.reverseConfirmBody}
        confirmLabel={t.reverse}
        variant="danger"
      />
    </div>
  );
}
