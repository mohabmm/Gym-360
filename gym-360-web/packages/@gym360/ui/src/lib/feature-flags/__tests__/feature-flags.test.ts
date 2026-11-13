import { test, expect } from '@playwright/test';
import { PT_MODULE_DEFAULTS, FEATURE_FLAG_KEYS as K } from '../keys';
import { ApiFeatureFlagService, defaultsSnapshot, type FlagStorage } from '../service';
import { featureDisabledKey } from '../errors';
import { flagsForPath, isPathEnabled, type RouteFlagRule } from '../route-guard';
import type { MyFeatureFlags } from '../../../types/feature-flags';

class MemoryStorage implements FlagStorage {
  private map = new Map<string, string>();
  get length() { return this.map.size; }
  key(i: number) { return [...this.map.keys()][i] ?? null; }
  getItem(k: string) { return this.map.get(k) ?? null; }
  setItem(k: string, v: string) { this.map.set(k, v); }
  removeItem(k: string) { this.map.delete(k); }
}

const response = (enabled: string[], gymId = 'g1'): MyFeatureFlags => ({
  gymId,
  preset: 'business',
  flags: Object.fromEntries(enabled.map((k) => [k, true])),
  enabled,
  refreshAfterSeconds: 120,
  resolvedAtUtc: new Date().toISOString(),
});

test.describe('fail-safe defaults', () => {
  test('defaults are exactly the 10 PT features, never all on', () => {
    const snap = defaultsSnapshot('g1');
    expect([...snap.enabled].sort()).toEqual([...PT_MODULE_DEFAULTS].sort());
    expect(snap.enabled.size).toBe(10);
    expect(snap.enabled.has(K.PT_PACKAGE_INTEREST)).toBe(true);
    expect(snap.enabled.has(K.GYM_FINANCE)).toBe(false);
    expect(snap.enabled.has(K.PT_AUTO_EXPIRY)).toBe(false);
  });

  test('no cache + fetch fails → pt_module defaults', async () => {
    const svc = new ApiFeatureFlagService(async () => { throw new Error('offline'); }, () => new MemoryStorage());
    const snap = await svc.refresh('g1');
    expect(snap.source).toBe('defaults');
    expect(snap.enabled.has(K.CLASSES)).toBe(false);
  });

  test('fetch fails after a good fetch → last cache', async () => {
    const storage = new MemoryStorage();
    let fail = false;
    const svc = new ApiFeatureFlagService(async () => {
      if (fail) throw new Error('offline');
      return response([K.CLASSES, K.PT_PACKAGES]);
    }, () => storage);
    expect((await svc.refresh('g1')).source).toBe('server');
    fail = true;
    const snap = await svc.refresh('g1');
    expect(snap.source).toBe('cache');
    expect(snap.enabled.has(K.CLASSES)).toBe(true);
    expect(snap.refreshAfterSeconds).toBe(120);
  });

  test('cache is per gym and cleared on logout', async () => {
    const storage = new MemoryStorage();
    const svc = new ApiFeatureFlagService(async () => response([K.CLASSES], 'g1'), () => storage);
    await svc.refresh('g1');
    expect(svc.load('g2').source).toBe('defaults');
    expect(svc.load('g1').enabled.has(K.CLASSES)).toBe(true);
    svc.clear();
    expect(svc.load('g1').source).toBe('defaults');
  });

  test('corrupt cache falls back to defaults', () => {
    const storage = new MemoryStorage();
    storage.setItem('gym360.ff.v1.g1', '{not json');
    const svc = new ApiFeatureFlagService(async () => response([]), () => storage);
    expect(svc.load('g1').source).toBe('defaults');
  });
});

test.describe('FEATURE_DISABLED errors', () => {
  test('extracts the key from a 403 body', () => {
    const err = { response: { status: 403, data: { errors: [{ code: 'FEATURE_DISABLED:classes' }] } } };
    expect(featureDisabledKey(err)).toBe('classes');
  });

  test('ignores other errors', () => {
    expect(featureDisabledKey({ response: { data: { errors: [{ code: 'FORBIDDEN' }] } } })).toBeNull();
    expect(featureDisabledKey(new Error('x'))).toBeNull();
    expect(featureDisabledKey(null)).toBeNull();
  });
});

test.describe('route guard', () => {
  const rules: RouteFlagRule[] = [
    { prefix: '/pt', flags: [K.PT_PACKAGES] },
    { prefix: '/pt/dashboard', flags: [K.PT_OWNER_DASHBOARD] },
    { prefix: '/gamification', flags: [K.COACH_LEADERBOARD, K.COACH_TESTIMONIALS] },
  ];
  const on = (keys: string[]) => (k: string) => keys.includes(k);

  test('unlisted paths are always on', () => {
    expect(flagsForPath('/members/123', rules)).toBeNull();
    expect(isPathEnabled('/members', rules, on([]))).toBe(true);
  });

  test('longest prefix wins and sub-paths inherit', () => {
    expect(flagsForPath('/pt/dashboard/x', rules)).toEqual([K.PT_OWNER_DASHBOARD]);
    expect(isPathEnabled('/pt/dashboard', rules, on([K.PT_PACKAGES]))).toBe(false);
    expect(isPathEnabled('/pt/trials', rules, on([K.PT_PACKAGES]))).toBe(true);
  });

  test('prefix match is per segment', () => {
    expect(flagsForPath('/ptx', rules)).toBeNull();
  });

  test('a multi-flag page shows when any flag is on', () => {
    expect(isPathEnabled('/gamification', rules, on([K.COACH_TESTIMONIALS]))).toBe(true);
    expect(isPathEnabled('/gamification', rules, on([]))).toBe(false);
  });
});

test('keys mirror the backend FeatureKeys (no drift)', async () => {
  const { readFileSync } = await import('node:fs');
  const { resolve } = await import('node:path');
  const cs = readFileSync(
    resolve(__dirname, '../../../../../../../../src/Common/GymApp.Common.Application/FeatureFlags/FeatureKeys.cs'),
    'utf8',
  );
  const backend = [...cs.matchAll(/public const string \w+ = "([^"]+)";/g)].map((m) => m[1]).sort();
  expect(Object.values(K).sort()).toEqual(backend);
});
