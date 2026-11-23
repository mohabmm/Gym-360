import { test, expect } from '@playwright/test';
import { canSaveChallenge, draftOf, isChallengeDraftValid, rewardText, type ChallengeSettings } from '../challenge';

const settings: ChallengeSettings = {
  target: 12, windowDays: 30,
  rewardTextAr: 'جلسة متابعة مع المدرب: قياسات جديدة وجدول الشهر الجاي', rewardTextEn: 'A follow-up session with your coach',
  isDefaultReward: true, minTarget: 1, maxTarget: 60, minWindowDays: 7, maxWindowDays: 90, maxRewardLength: 300,
  defaultRewardTextAr: 'جلسة متابعة مع المدرب: قياسات جديدة وجدول الشهر الجاي', defaultRewardTextEn: 'A follow-up session with your coach',
};

test('target and window must be whole numbers inside the gym limits; reward at most 300 characters', () => {
  const ok = draftOf(settings);
  expect(isChallengeDraftValid(ok, settings)).toBe(true);
  expect(isChallengeDraftValid({ ...ok, target: 0 }, settings)).toBe(false);
  expect(isChallengeDraftValid({ ...ok, target: 61 }, settings)).toBe(false);
  expect(isChallengeDraftValid({ ...ok, target: '' }, settings)).toBe(false);
  expect(isChallengeDraftValid({ ...ok, windowDays: 6 }, settings)).toBe(false);
  expect(isChallengeDraftValid({ ...ok, windowDays: 14.5 }, settings)).toBe(false);
  expect(isChallengeDraftValid({ ...ok, rewardTextAr: 'x'.repeat(301) }, settings)).toBe(false);
});

test('save is offered only for a valid change', () => {
  const ok = draftOf(settings);
  expect(canSaveChallenge(ok, settings)).toBe(false);
  expect(canSaveChallenge({ ...ok, target: 10 }, settings)).toBe(true);
  expect(canSaveChallenge({ ...ok, rewardTextAr: 'شهر مجاني' }, settings)).toBe(true);
  expect(canSaveChallenge({ ...ok, rewardTextAr: `${ok.rewardTextAr}  ` }, settings)).toBe(false);
  expect(canSaveChallenge({ ...ok, target: 100 }, settings)).toBe(false);
});

test('the reward shows in the page language and falls back to Arabic', () => {
  expect(rewardText({ rewardTextAr: 'عربي', rewardTextEn: 'English' }, true)).toBe('عربي');
  expect(rewardText({ rewardTextAr: 'عربي', rewardTextEn: 'English' }, false)).toBe('English');
  expect(rewardText({ rewardTextAr: 'عربي', rewardTextEn: '' }, false)).toBe('عربي');
});
