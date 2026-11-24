'use client';

import { toast } from 'sonner';
import { formatDate, useDocumentLocale } from '@gym360/ui';
import { useMemberProgramUnlocks, useSetProgramUnlock } from '@/lib/hooks/useStarterPlan';
import { starterPlanText, unlockState, unlockStatusText, type ProgramUnlock, type StarterPlanText, type UnlockLength } from '@/lib/starter-plan';

/** pt.starter_plan — the desk unlocks one week or 30 days from the templates for this member's coach (member stopped coming). */
export function MonthlyProgramsCard({ memberId }: { memberId: string }) {
  const locale = useDocumentLocale();
  const t = starterPlanText(locale);
  const { data } = useMemberProgramUnlocks(memberId);
  const coaches = data?.coaches ?? [];
  if (coaches.length === 0) return null;

  return (
    <section data-testid="monthly-programs-card" dir={locale === 'ar' ? 'rtl' : 'ltr'} className="rounded-lg border bg-card p-4 space-y-3">
      <div>
        <h3 className="font-semibold">{t.title}</h3>
        <p className="text-sm text-muted-foreground">{t.hint}</p>
      </div>
      {coaches.map((row) => <CoachRow key={row.trainerId} row={row} memberId={memberId} t={t} />)}
    </section>
  );
}

function CoachRow({ row, memberId, t }: { row: ProgramUnlock; memberId: string; t: StarterPlanText }) {
  const setUnlock = useSetProgramUnlock();
  const state = unlockState(row);
  const ended = state.endsWith('Ended');
  // length set = unlock that program; null = take the unlock back.
  const save = (length: UnlockLength | null) =>
    setUnlock.mutate(
      { trainerId: row.trainerId, traineeId: memberId, unlocked: length !== null, length: length ?? row.length },
      {
        onSuccess: () => toast.success(length ? t.unlockedToast(t.lengthName[length]) : t.lockedToast),
        onError: () => toast.error(t.saveError),
      },
    );

  return (
    <div className="flex items-center justify-between gap-3">
      <div>
        <p className="font-medium">{row.trainerName}</p>
        <p className={`text-sm ${ended ? 'text-amber-600' : 'text-muted-foreground'}`}>{unlockStatusText(row, t, formatDate)}</p>
      </div>
      <div className="flex gap-2">
        {row.unlocked ? (
          <button type="button" onClick={() => save(null)} disabled={setUnlock.isPending} className="rounded-md border px-3 py-1.5 text-sm hover:bg-muted">
            {t.lock}
          </button>
        ) : (
          (['OneWeek', 'ThirtyDays'] as const).map((length) => (
            <button key={length} type="button" onClick={() => save(length)} disabled={setUnlock.isPending}
              className="rounded-md bg-primary px-3 py-1.5 text-sm text-primary-foreground">
              {length === 'OneWeek' ? t.unlockWeek : t.unlockMonth}
            </button>
          ))
        )}
      </div>
    </div>
  );
}
