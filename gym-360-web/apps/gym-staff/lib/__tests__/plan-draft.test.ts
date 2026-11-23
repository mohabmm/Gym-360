import { test, expect } from '@playwright/test';
import {
  blankDraft, copyWeekOneToAll, draftFromDocument, isDraftValid, moveItem, setDurationWeeks, splitWeeks, toContent, workoutCount,
  type PlanDocument,
} from '../plan-draft';

const doc = (weeks: number): PlanDocument => ({
  schemaVersion: 1,
  weeks: Array.from({ length: weeks }, (_, i) => ({
    week: i + 1,
    days: ['A', 'B', 'C'].map((d) => ({
      key: `w${i + 1}-${d}`,
      title: `Day ${d}`,
      exercises: [{ key: `w${i + 1}-${d}-1`, name: 'Goblet Squat', sets: 3, reps: '10-12', restSeconds: 90 }],
    })),
  })),
});

const meta = { name: 'Beginner full body', nameAr: 'فول بادي', goal: 'GeneralFitness' as const, goalText: '' };

test('identical weeks open as "same days every week" and keep week 1 keys', () => {
  const draft = draftFromDocument(doc(4), meta);
  expect(draft.sameEveryWeek).toBe(true);
  expect(draft.weeks).toHaveLength(1);
  expect(draft.weeks[0][0].key).toBe('w1-A');
  expect(workoutCount(draft)).toBe(12);
});

test('the request repeats week 1 for every week', () => {
  const content = toContent(draftFromDocument(doc(4), meta));
  expect(content.repeatWeekOneForWeeks).toBe(4);
  expect(content.document.weeks).toHaveLength(1);
  expect(content.nameAr).toBe('فول بادي');
});

test('split weeks copy week 1 without keys; copy week 1 collapses them again', () => {
  const split = splitWeeks(draftFromDocument(doc(4), meta));
  expect(split.weeks).toHaveLength(4);
  expect(split.weeks[2][0].key).toBe('');
  expect(toContent(split).repeatWeekOneForWeeks).toBeNull();
  expect(copyWeekOneToAll(split).weeks).toHaveLength(1);
});

test('duration changes add copies of the last week or drop weeks', () => {
  const split = splitWeeks(draftFromDocument(doc(2), meta));
  split.weeks[1][0] = { ...split.weeks[1][0], title: 'Heavier' };
  expect(setDurationWeeks(split, 4).weeks.map((w) => w[0].title)).toEqual(['Day A', 'Heavier', 'Heavier', 'Heavier']);
  expect(setDurationWeeks(split, 99).durationWeeks).toBe(52);
  expect(setDurationWeeks(split, 0).durationWeeks).toBe(1);
});

test('moving an exercise up or down', () => {
  expect(moveItem(['a', 'b', 'c'], 0, 1)).toEqual(['b', 'a', 'c']);
  expect(moveItem(['a', 'b', 'c'], 0, -1)).toEqual(['a', 'b', 'c']);
});

test('a blank template is invalid until it has a name and a day with exercises', () => {
  const draft = blankDraft();
  expect(isDraftValid(draft)).toBe(false);
  const filled = { ...draft, name: 'House plan', weeks: [[{ key: '', title: 'Day A', exercises: [{ key: '', name: 'Plank', sets: 3, seconds: 45 }] }]] };
  expect(isDraftValid(filled)).toBe(true);
  expect(toContent(filled).document.weeks[0].days[0].exercises[0].reps).toBeNull();
});
