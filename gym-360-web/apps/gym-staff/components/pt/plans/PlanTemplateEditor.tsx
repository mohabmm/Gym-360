'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { toast } from 'sonner';
import { useDocumentLocale } from '@gym360/ui';
import {
  MAX_DAYS, PLAN_GOALS, blankDay, copyDay, copyWeekOneToAll, isDraftValid, setDurationWeeks, splitWeeks, toContent, workoutCount,
  type PlanDay, type PlanDraft,
} from '@/lib/plan-draft';
import { usePlanText, type PlanText } from '@/lib/plan-text';
import { useSavePlanTemplate } from '@/lib/hooks/usePlanTemplates';
import { PlanDayEditor } from './PlanDayEditor';

const INPUT = 'w-full rounded-md border bg-background px-3 py-2 text-sm';

/** The gym template editor: name and goal, weeks, then days and exercises. */
export function PlanTemplateEditor({ templateId, initial }: { templateId: string | null; initial: PlanDraft }) {
  const t = usePlanText();
  const isArabic = useDocumentLocale() === 'ar';
  const router = useRouter();
  const save = useSavePlanTemplate();
  const [draft, setDraft] = useState(initial);
  const [week, setWeek] = useState(0);
  const weekIndex = draft.sameEveryWeek ? 0 : Math.min(week, draft.weeks.length - 1);
  const days = draft.weeks[weekIndex];

  const setDays = (next: PlanDay[]) => setDraft({ ...draft, weeks: draft.weeks.map((w, i) => (i === weekIndex ? next : w)) });

  const submit = () => {
    if (!isDraftValid(draft)) {
      toast.error(t.incomplete);
      return;
    }
    save.mutate({ id: templateId, content: toContent(draft) }, {
      onSuccess: () => { toast.success(t.saved); router.push('/pt/plan-templates'); },
      onError: () => toast.error(t.saveError),
    });
  };

  return (
    <div className="space-y-4">
      <TemplateHeader draft={draft} t={t} onChange={setDraft} />
      <WeekControls draft={draft} t={t} week={weekIndex} onWeek={setWeek} onChange={setDraft} />
      {days.map((day, i) => (
        <PlanDayEditor
          key={day.key || `day-${weekIndex}-${i}`}
          day={day}
          index={i}
          t={t}
          isArabic={isArabic}
          onChange={(next) => setDays(days.map((d, j) => (j === i ? next : d)))}
          onDuplicate={() => setDays([...days.slice(0, i + 1), copyDay(day), ...days.slice(i + 1)])}
          onRemove={() => setDays(days.filter((_, j) => j !== i))}
        />
      ))}
      {days.length < MAX_DAYS && (
        <button type="button" className="rounded-md border px-4 py-2 text-sm font-medium" onClick={() => setDays([...days, blankDay()])}>
          + {t.addDay}
        </button>
      )}
      <div className="sticky bottom-0 flex items-center justify-end gap-4 border-t bg-background py-3">
        <span className="text-sm text-muted-foreground">{t.workouts(workoutCount(draft))}</span>
        <button type="button" disabled={save.isPending} onClick={submit}
          className="rounded-md bg-primary px-5 py-2 text-sm font-medium text-primary-foreground disabled:opacity-50">
          {t.save}
        </button>
      </div>
    </div>
  );
}

function TemplateHeader({ draft, t, onChange }: { draft: PlanDraft; t: PlanText; onChange: (d: PlanDraft) => void }) {
  return (
    <section className="rounded-xl border bg-card p-4 grid gap-3 sm:grid-cols-2">
      <label className="text-sm space-y-1"><span>{t.nameAr}</span>
        <input className={INPUT} value={draft.nameAr} onChange={(e) => onChange({ ...draft, nameAr: e.target.value })} />
      </label>
      <label className="text-sm space-y-1"><span>{t.name}</span>
        <input className={INPUT} dir="ltr" value={draft.name} onChange={(e) => onChange({ ...draft, name: e.target.value })} />
      </label>
      <label className="text-sm space-y-1"><span>{t.goal}</span>
        <select className={INPUT} value={draft.goal} onChange={(e) => onChange({ ...draft, goal: e.target.value as PlanDraft['goal'] })}>
          {PLAN_GOALS.map((g) => <option key={g} value={g}>{t.goals[g]}</option>)}
        </select>
      </label>
      {draft.goal === 'Other' && (
        <label className="text-sm space-y-1"><span>{t.goalText}</span>
          <input className={INPUT} value={draft.goalText} onChange={(e) => onChange({ ...draft, goalText: e.target.value })} />
        </label>
      )}
    </section>
  );
}

function WeekControls({ draft, t, week, onWeek, onChange }: {
  draft: PlanDraft; t: PlanText; week: number; onWeek: (w: number) => void; onChange: (d: PlanDraft) => void;
}) {
  return (
    <section className="rounded-xl border bg-card p-4 space-y-3">
      <div className="flex flex-wrap items-center gap-4">
        <label className="text-sm flex items-center gap-2"><span>{t.duration}</span>
          <input type="number" min={1} max={52} className="w-20 rounded-md border bg-background px-2 py-1.5 text-sm"
            value={draft.durationWeeks} onChange={(e) => onChange(setDurationWeeks(draft, Number(e.target.value)))} />
        </label>
        <label className="text-sm flex items-center gap-2">
          <input type="checkbox" checked={draft.sameEveryWeek}
            onChange={(e) => onChange(e.target.checked ? copyWeekOneToAll(draft) : splitWeeks(draft))} />
          <span>{t.sameEveryWeek}</span>
        </label>
      </div>
      {!draft.sameEveryWeek && (
        <div className="flex flex-wrap items-center gap-2">
          {draft.weeks.map((_, i) => (
            <button key={i} type="button" onClick={() => onWeek(i)}
              className={`rounded-full border px-3 py-1 text-sm ${i === week ? 'bg-primary text-primary-foreground' : ''}`}>
              {t.week(i + 1)}
            </button>
          ))}
          <button type="button" className="text-sm text-primary hover:underline" onClick={() => onChange(copyWeekOneToAll(draft))}>
            {t.copyWeekOne}
          </button>
        </div>
      )}
    </section>
  );
}
