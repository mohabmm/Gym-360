import { apiGet } from '../api-client';
import { FEATURE_FLAGS } from '../api-endpoints';
import type { MyFeatureFlags } from '../../types/feature-flags';
import { PT_MODULE_DEFAULTS } from './keys';

/** Resolved flags for one gym, as the app uses them. */
export interface FeatureFlagSnapshot {
  gymId: string;
  preset: string;
  enabled: ReadonlySet<string>;
  /** 'server' = fresh fetch, 'cache' = last good fetch, 'defaults' = pt_module fallback. */
  source: 'server' | 'cache' | 'defaults';
  refreshAfterSeconds: number;
}

/**
 * The only way web code reads feature flags. Swap the implementation (e.g. a self-hosted
 * Flagsmith client) without touching pages or components.
 */
export interface FeatureFlagService {
  /** Cached flags for the gym (no network); pt_module defaults when nothing is cached. */
  load(gymId: string): FeatureFlagSnapshot;
  /** Fetches from the server and caches. On failure returns the cache (or defaults). Never "all on". */
  refresh(gymId: string): Promise<FeatureFlagSnapshot>;
  /** Forgets the cached flags (logout). */
  clear(): void;
}

export const DEFAULT_REFRESH_SECONDS = 300;
const CACHE_PREFIX = 'gym360.ff.v1.';

export function defaultsSnapshot(gymId: string): FeatureFlagSnapshot {
  return {
    gymId,
    preset: 'pt_module',
    enabled: new Set(PT_MODULE_DEFAULTS),
    source: 'defaults',
    refreshAfterSeconds: DEFAULT_REFRESH_SECONDS,
  };
}

export function snapshotFromResponse(data: MyFeatureFlags, source: FeatureFlagSnapshot['source']): FeatureFlagSnapshot {
  const enabled = new Set<string>(data.enabled ?? []);
  for (const [key, on] of Object.entries(data.flags ?? {})) {
    if (on) enabled.add(key);
  }
  return {
    gymId: data.gymId,
    preset: data.preset,
    enabled,
    source,
    refreshAfterSeconds: data.refreshAfterSeconds > 0 ? data.refreshAfterSeconds : DEFAULT_REFRESH_SECONDS,
  };
}

/** Minimal storage surface (window.localStorage in the browser; injectable for tests). */
export interface FlagStorage {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
  readonly length: number;
  key(index: number): string | null;
}

function browserStorage(): FlagStorage | null {
  try {
    return typeof window === 'undefined' ? null : window.localStorage;
  } catch {
    return null;
  }
}

/** `GET /feature-flags/me` with a per-gym localStorage cache. */
export class ApiFeatureFlagService implements FeatureFlagService {
  constructor(
    private readonly fetchFlags: () => Promise<MyFeatureFlags> = async () =>
      (await apiGet<MyFeatureFlags>(FEATURE_FLAGS.ME)).data,
    private readonly storage: () => FlagStorage | null = browserStorage,
  ) {}

  load(gymId: string): FeatureFlagSnapshot {
    const raw = this.storage()?.getItem(CACHE_PREFIX + gymId);
    if (!raw) return defaultsSnapshot(gymId);
    try {
      return snapshotFromResponse(JSON.parse(raw) as MyFeatureFlags, 'cache');
    } catch {
      return defaultsSnapshot(gymId);
    }
  }

  async refresh(gymId: string): Promise<FeatureFlagSnapshot> {
    try {
      const data = await this.fetchFlags();
      // The token's gym wins; a stale gymId in the UI must not cache flags under the wrong gym.
      this.storage()?.setItem(CACHE_PREFIX + (data.gymId || gymId), JSON.stringify(data));
      return snapshotFromResponse(data, 'server');
    } catch {
      return this.load(gymId);
    }
  }

  clear(): void {
    const storage = this.storage();
    if (!storage) return;
    const keys: string[] = [];
    for (let i = 0; i < storage.length; i++) {
      const k = storage.key(i);
      if (k?.startsWith(CACHE_PREFIX)) keys.push(k);
    }
    keys.forEach((k) => storage.removeItem(k));
  }
}

let activeService: FeatureFlagService = new ApiFeatureFlagService();

/** Replace the provider (tests, or a future Flagsmith client). */
export function setFeatureFlagService(service: FeatureFlagService): void {
  activeService = service;
}

export function getFeatureFlagService(): FeatureFlagService {
  return activeService;
}
