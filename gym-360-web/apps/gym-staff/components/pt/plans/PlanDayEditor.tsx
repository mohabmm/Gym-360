'use client';

import { blankExercise, moveItem, type PlanDay } from '@/lib/plan-draft';
import { type PlanText } from '@/lib/plan-text';
import { ExerciseRowEditor } from './ExerciseRowEditor';

const INPUT = 'w-full rounded-md border bg-background px-3 py-2 text-sm';

interface Props {
  day: PlanDay;
  index: number;
  t: PlanText;
  isArabic: boolean;
  onChange: (day: PlanDay) => void;
  onDuplicate: () => void;
  onRemove: () => void;
}

/** One workout day: its names and its exercises in order. */
export function PlanDayEditor({ day, index, t, isArabic, onChange, onDuplicate, onRemove }: Props) {
  const setExercises = (exercises: PlanDay['exercises']) => onChange({ ...day, exercises });
  return (
    <section className="rounded-xl border bg-card p-4 space-y-3">
      <div className="flex items-center justify-between gap-3">
        <h3 className="font-semibold">{t.day(index + 1)}</h3>
        <div className="space-x-3 rtl:space-x-reverse text-sm">
          <button type="button" className="text-primary hover:underline" onClick={onDuplicate}>{t.duplicateDay}</button>
          <button type="button" className="text-destructive hover:underline" onClick={onRemove}>{t.removeDay}</button>
        </div>
      </div>
      <div className="grid gap-2 sm:grid-cols-2">
        <input className={INPUT} placeholder={t.dayTitleAr} value={day.titleAr ?? ''} onChange={(e) => onChange({ ...day, titleAr: e.target.value })} />
        <input className={INPUT} dir="ltr" placeholder={t.dayTitle} value={day.title} onChange={(e) => onChange({ ...day, title: e.target.value })} />
      </div>
      {day.exercises.map((ex, i) => (
        <ExerciseRowEditor
          key={ex.key || `new-${i}`}
          exercise={ex}
          t={t}
          isArabic={isArabic}
          onChange={(next) => setExercises(day.exercises.map((x, j) => (j === i ? next : x)))}
          onMove={(delta) => setExercises(moveItem(day.exercises, i, i + delta))}
          onRemove={() => setExercises(day.exercises.filter((_, j) => j !== i))}
        />
      ))}
      <button type="button" className="text-sm font-medium text-primary hover:underline" onClick={() => setExercises([...day.exercises, blankExercise()])}>
        + {t.addExercise}
      </button>
    </section>
  );
}
