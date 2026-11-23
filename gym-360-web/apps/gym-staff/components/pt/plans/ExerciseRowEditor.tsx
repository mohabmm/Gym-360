'use client';

import { useState } from 'react';
import { type PlanExercise } from '@/lib/plan-draft';
import { type PlanText } from '@/lib/plan-text';
import { useExerciseSearch, type LibraryExercise } from '@/lib/hooks/usePlanTemplates';

const INPUT = 'w-full rounded-md border bg-background px-2 py-1.5 text-sm';
const OPTION = 'w-full px-3 py-2 text-start text-sm hover:bg-muted';

const num = (v: string) => (v.trim() === '' ? null : Number(v));

interface Props {
  exercise: PlanExercise;
  t: PlanText;
  isArabic: boolean;
  onChange: (e: PlanExercise) => void;
  onMove: (delta: -1 | 1) => void;
  onRemove: () => void;
}

/** One exercise: library search or free text, sets, reps or time, rest, target and notes. */
export function ExerciseRowEditor({ exercise: e, t, isArabic, onChange, onMove, onRemove }: Props) {
  const set = (patch: Partial<PlanExercise>) => onChange({ ...e, ...patch });
  const timed = e.seconds != null;
  return (
    <div className="rounded-lg border p-3 space-y-2">
      <div className="flex items-start gap-2">
        <ExerciseNameField exercise={e} t={t} isArabic={isArabic} onChange={set} />
        <button type="button" className="text-xs text-muted-foreground" onClick={() => onMove(-1)} aria-label={t.moveUp}>▲</button>
        <button type="button" className="text-xs text-muted-foreground" onClick={() => onMove(1)} aria-label={t.moveDown}>▼</button>
        <button type="button" className="text-xs text-destructive" onClick={onRemove}>{t.remove}</button>
      </div>
      <div className="grid grid-cols-2 gap-2 sm:grid-cols-6">
        <Field label={t.sets}><input type="number" min={1} max={20} className={INPUT} value={e.sets} onChange={(ev) => set({ sets: Number(ev.target.value) || 1 })} /></Field>
        <Field label={t.timed}>
          <input type="checkbox" checked={timed} onChange={(ev) => set({ seconds: ev.target.checked ? 30 : null, reps: ev.target.checked ? null : '10' })} />
        </Field>
        {timed ? (
          <Field label={t.seconds}><input type="number" min={1} className={INPUT} value={e.seconds ?? ''} onChange={(ev) => set({ seconds: num(ev.target.value) })} /></Field>
        ) : (
          <Field label={t.reps}><input className={INPUT} dir="ltr" value={e.reps ?? ''} onChange={(ev) => set({ reps: ev.target.value })} /></Field>
        )}
        <Field label={t.rest}><input type="number" min={0} className={INPUT} value={e.restSeconds ?? ''} onChange={(ev) => set({ restSeconds: num(ev.target.value) })} /></Field>
        <Field label={t.targetKg}><input type="number" min={0} step={0.5} className={INPUT} value={e.targetWeightKg ?? ''} onChange={(ev) => set({ targetWeightKg: num(ev.target.value) })} /></Field>
        <Field label={t.rpe}><input type="number" min={1} max={10} className={INPUT} value={e.rpe ?? ''} onChange={(ev) => set({ rpe: num(ev.target.value) })} /></Field>
      </div>
      <div className="grid gap-2 sm:grid-cols-2">
        <input className={INPUT} placeholder={t.notes} value={e.notes ?? ''} onChange={(ev) => set({ notes: ev.target.value })} />
        <input className={INPUT} dir="ltr" placeholder={t.video} value={e.videoUrl ?? ''} onChange={(ev) => set({ videoUrl: ev.target.value })} />
      </div>
    </div>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <label className="block text-xs text-muted-foreground space-y-1">
      <span>{label}</span>
      {children}
    </label>
  );
}

const label = (isArabic: boolean, x: { name: string; nameAr?: string | null }) => (isArabic && x.nameAr ? x.nameAr : x.name);

/** Typing searches the library; picking a result links it, otherwise the text is a free-text exercise. */
function ExerciseNameField({ exercise: e, t, isArabic, onChange }: {
  exercise: PlanExercise; t: PlanText; isArabic: boolean; onChange: (p: Partial<PlanExercise>) => void;
}) {
  const [query, setQuery] = useState('');
  const [open, setOpen] = useState(false);
  const shown = label(isArabic, e);
  const isFreeText = !open && !!e.name && !e.exerciseId;
  return (
    <div className="relative flex-1">
      <input
        className={INPUT}
        placeholder={t.searchLibrary}
        value={open ? query : shown}
        onFocus={() => { setQuery(shown); setOpen(true); }}
        onChange={(ev) => setQuery(ev.target.value)}
        onBlur={() => setTimeout(() => setOpen(false), 150)}
      />
      {isFreeText && <span className="text-[11px] text-muted-foreground">{t.freeText}</span>}
      {open && (
        <SearchResults
          query={query}
          t={t}
          isArabic={isArabic}
          onFreeText={() => onChange({ name: query.trim(), nameAr: null, exerciseId: null })}
          onPick={(r) => onChange({ name: r.name, nameAr: r.nameAr ?? null, exerciseId: r.id, videoUrl: e.videoUrl || r.demoMediaUrl || null })}
        />
      )}
    </div>
  );
}

function SearchResults({ query, t, isArabic, onFreeText, onPick }: {
  query: string; t: PlanText; isArabic: boolean; onFreeText: () => void; onPick: (r: LibraryExercise) => void;
}) {
  const { data: results = [] } = useExerciseSearch(query);
  const text = query.trim();
  return (
    <ul className="absolute z-10 mt-1 max-h-60 w-full overflow-auto rounded-md border bg-popover shadow">
      {text && (
        <li>
          <button type="button" className={OPTION} onMouseDown={onFreeText}>“{text}” · {t.freeText}</button>
        </li>
      )}
      {results.map((r) => (
        <li key={r.id}>
          <button type="button" className={OPTION} onMouseDown={() => onPick(r)}>
            {label(isArabic, r)}
            {isArabic && r.nameAr && <span className="ms-2 text-xs text-muted-foreground" dir="ltr">{r.name}</span>}
          </button>
        </li>
      ))}
    </ul>
  );
}
