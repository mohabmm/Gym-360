/**
 * pt.starter_plan — the coach starts a member on a 3-day plan. When the member stops coming the
 * staff unlock one week or 30 days: the coach gives a program from the templates (and a diet) to
 * bring them back. Each unlock allows one program; the coach giving it locks it again.
 */

export type PlanLimit = 'Starter' | 'OneWeek' | 'ThirtyDays';
export type UnlockLength = 'OneWeek' | 'ThirtyDays';

export interface LimitedPlanStatus {
  planId: string;
  limit: PlanLimit;
  startDate: string;
  endsOn: string;
  hasEnded: boolean;
}

export interface ProgramUnlock {
  trainerId: string;
  trainerName: string;
  unlocked: boolean;
  length: UnlockLength;
  changedAt: string | null;
  limitedPlan: LimitedPlanStatus | null;
}

export interface MemberProgramUnlocks {
  traineeId: string;
  coaches: ProgramUnlock[];
}

export type UnlockState =
  | 'unlocked' | 'noPlan' | 'starterRunning' | 'starterEnded' | 'weekRunning' | 'weekEnded' | 'monthRunning' | 'monthEnded';

const PREFIX: Record<PlanLimit, 'starter' | 'week' | 'month'> = { Starter: 'starter', OneWeek: 'week', ThirtyDays: 'month' };

/** Where the coach–member pair stands, for the card's status line. */
export function unlockState(row: ProgramUnlock): UnlockState {
  if (row.unlocked) return 'unlocked';
  const plan = row.limitedPlan;
  if (!plan) return 'noPlan';
  return `${PREFIX[plan.limit]}${plan.hasEnded ? 'Ended' : 'Running'}`;
}

const TEXT = {
  en: {
    title: "Coach's program: one week or 30 days",
    hint: 'Member stopped coming? Unlock one week or 30 days from the templates so the coach gives a plan and a diet to bring them back.',
    lengthName: { OneWeek: 'One-week program', ThirtyDays: '30-day program' } as Record<UnlockLength, string>,
    unlocked: (name: string) => `Unlocked: ${name} – the coach can give it`,
    noPlan: 'No plan right now',
    starterRunning: (date: string) => `Starter plan running · last day ${date}`,
    starterEnded: 'The 3-day starter plan is over',
    weekRunning: (date: string) => `One-week program running · last day ${date}`,
    weekEnded: 'One-week program is over – unlock another if they are still away',
    monthRunning: (date: string) => `30-day program running · last day ${date}`,
    monthEnded: '30-day program is over – unlock another if they are still away',
    unlockWeek: 'One week',
    unlockMonth: '30 days',
    lock: 'Lock',
    unlockedToast: (name: string) => `${name} unlocked. The coach is notified.`,
    lockedToast: 'Program locked.',
    saveError: 'Could not save. Try again.',
  },
  ar: {
    title: 'برنامج للمدرب: أسبوع أو ٣٠ يوم',
    hint: 'العضو غاب؟ افتح للمدرب أسبوع أو ٣٠ يوم من القوالب عشان يدّيله خطة وأكل ويرجع الجيم.',
    lengthName: { OneWeek: 'برنامج أسبوع', ThirtyDays: 'برنامج ٣٠ يوم' } as Record<UnlockLength, string>,
    unlocked: (name: string) => `مفتوح: ${name} – المدرب يقدر يدّيه`,
    noPlan: 'مفيش خطة دلوقتي',
    starterRunning: (date: string) => `خطة البداية شغالة · آخر يوم ${date}`,
    starterEnded: 'خلصت خطة البداية ٣ أيام',
    weekRunning: (date: string) => `برنامج أسبوع شغال · آخر يوم ${date}`,
    weekEnded: 'خلص برنامج الأسبوع – لو لسه غايب افتح برنامج تاني',
    monthRunning: (date: string) => `برنامج ٣٠ يوم شغال · آخر يوم ${date}`,
    monthEnded: 'خلص برنامج الـ٣٠ يوم – لو لسه غايب افتح برنامج تاني',
    unlockWeek: 'أسبوع',
    unlockMonth: '٣٠ يوم',
    lock: 'اقفله',
    unlockedToast: (name: string) => `تم فتح ${name} ووصل إشعار للمدرب.`,
    lockedToast: 'اتقفل البرنامج.',
    saveError: 'تعذّر الحفظ، حاول مرة أخرى.',
  },
};

export type StarterPlanText = (typeof TEXT)['en'];

export function starterPlanText(locale: string): StarterPlanText {
  return TEXT[locale === 'ar' ? 'ar' : 'en'];
}

/** The status line for one coach. */
export function unlockStatusText(row: ProgramUnlock, t: StarterPlanText, formatDate: (iso: string) => string): string {
  const state = unlockState(row);
  switch (state) {
    case 'unlocked':
      return t.unlocked(t.lengthName[row.length]);
    case 'starterRunning':
    case 'weekRunning':
    case 'monthRunning':
      return t[state](formatDate(row.limitedPlan!.endsOn));
    default:
      return t[state];
  }
}
