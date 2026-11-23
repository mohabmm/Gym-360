/** mem.first_month_challenge — what the API returns, and the settings form's rules. */
export interface ChallengeSettings {
  target: number;
  windowDays: number;
  rewardTextAr: string;
  rewardTextEn: string;
  isDefaultReward: boolean;
  minTarget: number;
  maxTarget: number;
  minWindowDays: number;
  maxWindowDays: number;
  maxRewardLength: number;
  defaultRewardTextAr: string;
  defaultRewardTextEn: string;
}

export interface ChallengeAward {
  awardId: string;
  traineeId: string;
  traineeName: string;
  trainerId?: string | null;
  trainerName?: string | null;
  count: number;
  target: number;
  windowStart: string;
  windowEnd: string;
  reachedAt: string;
  rewardTextAr: string;
  rewardTextEn: string;
  deliveredAt?: string | null;
  deliveredByName?: string | null;
}

export interface PlanSummary {
  activePlans: number;
  workoutsThisMonth: number;
  membersTrainingThisMonth: number;
  challengeEnabled: boolean;
  challengeReached: number;
  challengeAwaitingDelivery: number;
}

/** The form as typed; '' while a number field is empty. */
export interface ChallengeDraft {
  target: number | '';
  windowDays: number | '';
  rewardTextAr: string;
  rewardTextEn: string;
}

export function draftOf(s: ChallengeSettings): ChallengeDraft {
  return { target: s.target, windowDays: s.windowDays, rewardTextAr: s.rewardTextAr, rewardTextEn: s.rewardTextEn };
}

const inRange = (v: number | '', min: number, max: number): v is number =>
  typeof v === 'number' && Number.isInteger(v) && v >= min && v <= max;

export function isChallengeDraftValid(d: ChallengeDraft, s: ChallengeSettings): boolean {
  return (
    inRange(d.target, s.minTarget, s.maxTarget) &&
    inRange(d.windowDays, s.minWindowDays, s.maxWindowDays) &&
    d.rewardTextAr.trim().length <= s.maxRewardLength &&
    d.rewardTextEn.trim().length <= s.maxRewardLength
  );
}

/** Save only a valid change. */
export function canSaveChallenge(d: ChallengeDraft, s: ChallengeSettings): boolean {
  const before = draftOf(s);
  const changed = (Object.keys(before) as (keyof ChallengeDraft)[]).some((k) => String(d[k]).trim() !== String(before[k]).trim());
  return changed && isChallengeDraftValid(d, s);
}

/** The reward in the page's language; English falls back to the Arabic. */
export function rewardText(award: Pick<ChallengeAward, 'rewardTextAr' | 'rewardTextEn'>, isArabic: boolean): string {
  return isArabic || !award.rewardTextEn ? award.rewardTextAr : award.rewardTextEn;
}
