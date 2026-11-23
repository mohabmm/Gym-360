/**
 * pt.training_plan — plan schema v1 as the API sends it, and the pure editing logic of the gym
 * template editor (mirrors the trainer app's PlanDraft).
 */
export type PlanGoal = 'GeneralFitness' | 'FatLoss' | 'MuscleGain' | 'Other';

export const PLAN_GOALS: readonly PlanGoal[] = ['GeneralFitness', 'FatLoss', 'MuscleGain', 'Other'];

export interface PlanExercise {
  key: string;
  exerciseId?: string | null;
  name: string;
  nameAr?: string | null;
  sets: number;
  reps?: string | null;
  seconds?: number | null;
  restSeconds?: number | null;
  targetWeightKg?: number | null;
  rpe?: number | null;
  notes?: string | null;
  videoUrl?: string | null;
}

export interface PlanDay {
  key: string;
  title: string;
  titleAr?: string | null;
  exercises: PlanExercise[];
}

export interface PlanWeek {
  week: number;
  days: PlanDay[];
}

export interface PlanDocument {
  schemaVersion: number;
  weeks: PlanWeek[];
}

export interface PlanDraft {
  name: string;
  nameAr: string;
  goal: PlanGoal;
  goalText: string;
  durationWeeks: number;
  sameEveryWeek: boolean;
  /** One entry when sameEveryWeek; otherwise one per week. */
  weeks: PlanDay[][];
}

export const MAX_WEEKS = 52;
export const MAX_DAYS = 7;

export const blankExercise = (): PlanExercise => ({ key: '', name: '', sets: 3, reps: '10', restSeconds: 60 });

export const blankDay = (): PlanDay => ({ key: '', title: '', exercises: [] });

export const blankDraft = (): PlanDraft => ({
  name: '', nameAr: '', goal: 'GeneralFitness', goalText: '', durationWeeks: 4, sameEveryWeek: true, weeks: [[blankDay()]],
});

const shape = (days: PlanDay[]) =>
  days.map((d) => [d.title, ...d.exercises.map((e) => `${e.name}|${e.sets}|${e.reps}|${e.seconds}|${e.targetWeightKg}`)].join(',')).join(';');

/** Opens a template; identical weeks collapse into "same days every week". */
export function draftFromDocument(doc: PlanDocument, meta: Pick<PlanDraft, 'name' | 'nameAr' | 'goal' | 'goalText'>): PlanDraft {
  const weeks = [...doc.weeks].sort((a, b) => a.week - b.week).map((w) => w.days);
  const same = weeks.length <= 1 || weeks.every((w) => shape(w) === shape(weeks[0]));
  return {
    ...meta,
    durationWeeks: Math.max(weeks.length, 1),
    sameEveryWeek: same,
    weeks: (same ? weeks.slice(0, 1) : weeks).map((days) => days.map(cloneDay(true))),
  };
}

/** A deep copy; without keys it is a new day the server gives fresh keys. */
const cloneDay = (keepKeys: boolean) => (d: PlanDay): PlanDay => ({
  ...d,
  key: keepKeys ? d.key : '',
  exercises: d.exercises.map((e) => ({ ...e, key: keepKeys ? e.key : '' })),
});

export const copyDay = cloneDay(false);

export function splitWeeks(draft: PlanDraft): PlanDraft {
  if (!draft.sameEveryWeek) return draft;
  const first = draft.weeks[0];
  return {
    ...draft,
    sameEveryWeek: false,
    weeks: [first, ...Array.from({ length: draft.durationWeeks - 1 }, () => first.map(copyDay))],
  };
}

/** "Copy week 1 to all weeks". */
export const copyWeekOneToAll = (draft: PlanDraft): PlanDraft => ({ ...draft, sameEveryWeek: true, weeks: [draft.weeks[0]] });

export function setDurationWeeks(draft: PlanDraft, value: number): PlanDraft {
  const durationWeeks = Math.min(Math.max(Math.round(value) || 1, 1), MAX_WEEKS);
  if (draft.sameEveryWeek) return { ...draft, durationWeeks };
  const weeks = draft.weeks.slice(0, durationWeeks);
  while (weeks.length < durationWeeks) weeks.push(weeks[weeks.length - 1].map(copyDay));
  return { ...draft, durationWeeks, weeks };
}

export function moveItem<T>(items: T[], from: number, to: number): T[] {
  if (to < 0 || to >= items.length) return items;
  const next = [...items];
  const [item] = next.splice(from, 1);
  next.splice(to, 0, item);
  return next;
}

export const isExerciseValid = (e: PlanExercise) =>
  e.name.trim() !== '' && e.sets >= 1 && (e.seconds != null ? e.seconds >= 1 : (e.reps ?? '').trim() !== '');

/** A day needs a name (English or Arabic) and at least one complete exercise. */
const dayTitle = (d: PlanDay) => d.title.trim() || (d.titleAr ?? '').trim();

export const isDayValid = (d: PlanDay) => dayTitle(d) !== '' && d.exercises.length > 0 && d.exercises.every(isExerciseValid);

export const isDraftValid = (draft: PlanDraft) =>
  draft.name.trim() !== '' &&
  (draft.goal !== 'Other' || draft.goalText.trim() !== '') &&
  draft.weeks.every((w) => w.length > 0 && w.length <= MAX_DAYS && w.every(isDayValid));

export const workoutCount = (draft: PlanDraft) =>
  draft.sameEveryWeek ? draft.weeks[0].length * draft.durationWeeks : draft.weeks.reduce((n, w) => n + w.length, 0);

const blankToNull = (v?: string | null) => (v && v.trim() !== '' ? v.trim() : null);

/** Body of POST/PUT /training/plan-templates (PlanContentInput). */
export function toContent(draft: PlanDraft) {
  return {
    name: draft.name.trim(),
    nameAr: blankToNull(draft.nameAr),
    goal: draft.goal,
    goalText: draft.goal === 'Other' ? draft.goalText.trim() : null,
    repeatWeekOneForWeeks: draft.sameEveryWeek ? draft.durationWeeks : null,
    document: {
      schemaVersion: 1,
      weeks: draft.weeks.map((days, i) => ({
        week: i + 1,
        days: days.map((d) => ({
          ...d,
          title: dayTitle(d),
          titleAr: blankToNull(d.titleAr),
          exercises: d.exercises.map((e) => ({
            ...e,
            name: e.name.trim(),
            reps: e.seconds != null ? null : blankToNull(e.reps),
            notes: blankToNull(e.notes),
            videoUrl: blankToNull(e.videoUrl),
          })),
        })),
      })),
    },
  };
}
