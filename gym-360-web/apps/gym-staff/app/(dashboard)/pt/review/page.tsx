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
      <ReviewState isLoading={isLoading} isError={isError} isEmpty={sessions.length === 0} />
      {sessions.length > 0 && (
        <PtTable head={[t.member, t.coach, t.sessionDate, '']}>
          {sessions.map((s) => (
            <DisputedRow
              key={s.id}
              member={memberName.get(s.memberId)}
              coach={trainerName.get(s.trainerId)}
              scheduledAt={s.scheduledAt}
              onReverse={() => setTarget(s.id)}
            />
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

/** Loading, error or "nothing to review" line; nothing once there are rows. */
function ReviewState({ isLoading, isError, isEmpty }: { isLoading: boolean; isError: boolean; isEmpty: boolean }) {
  const t = usePtText();
  if (isLoading) return <PtState text="…" />;
  if (isError) return <PtState text={t.loadError} />;
  return isEmpty ? <PtState text={t.reviewEmpty} /> : null;
}

function DisputedRow({ member, coach, scheduledAt, onReverse }: {
  member?: string;
  coach?: string;
  scheduledAt: string;
  onReverse: () => void;
}) {
  const t = usePtText();
  return (
    <tr>
      <PtCell className="font-medium">{member ?? '—'}</PtCell>
      <PtCell>{coach ?? '—'}</PtCell>
      <PtCell>{formatDateTime(scheduledAt)}</PtCell>
      <PtCell className="text-end">
        <button
          onClick={onReverse}
          className="rounded-lg border border-red-200 bg-red-50 px-3 py-1.5 text-sm font-medium text-red-700 hover:bg-red-100"
        >
          {t.reverse}
        </button>
      </PtCell>
    </tr>
  );
}
