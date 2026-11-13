import { test, expect } from '@playwright/test';
import { PT_MODULE_DEFAULTS, FEATURE_FLAG_KEYS as K } from '../../../../packages/@gym360/ui/src/lib/feature-flags/keys';
import { isPathEnabled } from '../../../../packages/@gym360/ui/src/lib/feature-flags/route-guard';
import { STAFF_ROUTE_RULES } from '../feature-flag-rules';

const pt = (k: string) => PT_MODULE_DEFAULTS.includes(k);
const all = () => true;

const OFF_IN_PT = ['/classes', '/exercises', '/messages', '/billing', '/gamification', '/announcements', '/analytics', '/check-ins', '/kiosk-pin', '/data-import'];
const ALWAYS_ON = ['/', '/members', '/members/abc', '/trainers', '/trainers/abc', '/staff', '/settings', '/subscription'];
const PT_PAGES = ['/pt/trials', '/pt/review', '/pt/dashboard'];

test('pt_module: every OFF-module route is blocked', () => {
  for (const path of OFF_IN_PT) expect(isPathEnabled(path, STAFF_ROUTE_RULES, pt), path).toBe(false);
});

test('pt_module: always-on and PT routes stay reachable', () => {
  for (const path of [...ALWAYS_ON, ...PT_PAGES]) expect(isPathEnabled(path, STAFF_ROUTE_RULES, pt), path).toBe(true);
});

test('business preset: everything reachable', () => {
  for (const path of [...OFF_IN_PT, ...ALWAYS_ON, ...PT_PAGES]) expect(isPathEnabled(path, STAFF_ROUTE_RULES, all), path).toBe(true);
});

test('one override turns on exactly its route', () => {
  const withClasses = (k: string) => pt(k) || k === K.CLASSES;
  expect(isPathEnabled('/classes', STAFF_ROUTE_RULES, withClasses)).toBe(true);
  expect(isPathEnabled('/billing', STAFF_ROUTE_RULES, withClasses)).toBe(false);
});

test('deep sub-routes inherit their module flag', () => {
  expect(isPathEnabled('/classes/123/edit', STAFF_ROUTE_RULES, pt)).toBe(false);
  expect(isPathEnabled('/pt/dashboard?days=7'.split('?')[0], STAFF_ROUTE_RULES, (k) => k !== K.PT_OWNER_DASHBOARD && pt(k))).toBe(false);
});
