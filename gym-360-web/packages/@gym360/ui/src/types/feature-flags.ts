/** Feature flags (Validation_1_PrivateSessions). Shapes mirror the .NET API. */

export type FeatureFlagKind = 'feature' | 'setting';
export type FeatureFlagSource = 'preset' | 'override';

export interface FeatureCatalogItem {
  key: string;
  module: string;
  kind: FeatureFlagKind;
  nameEn: string;
  nameAr: string;
  descriptionEn: string;
  descriptionAr: string;
  apps: string[];
}

export interface GymFlagSummary {
  gymId: string;
  name: string;
  status: string;
  presetCode: string;
  overrideCount: number;
}

export interface GymFlagRow {
  feature: FeatureCatalogItem;
  enabled: boolean;
  source: FeatureFlagSource;
  presetValue: boolean;
  hasOverride: boolean;
  overrideReason?: string | null;
  overrideSetAtUtc?: string | null;
}

export interface GymFlagDetail {
  gymId: string;
  name: string;
  presetCode: string;
  flags: GymFlagRow[];
}

export interface FeaturePreset {
  code: string;
  nameEn: string;
  nameAr: string;
  gymCount: number;
  values: Record<string, boolean>;
}

export interface FlagAuditEntry {
  id: string;
  occurredAtUtc: string;
  actorUserId: string;
  actorEmail?: string | null;
  gymId?: string | null;
  gymName?: string | null;
  changeType: 'preset_assigned' | 'override_set' | 'override_cleared' | 'preset_edited' | string;
  featureKey?: string | null;
  presetCode?: string | null;
  oldValue?: string | null;
  newValue?: string | null;
  reason?: string | null;
}

export interface FlagPage<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

/** GET /feature-flags/me — resolved flags for the signed-in user's gym. */
export interface MyFeatureFlags {
  gymId: string;
  preset: string;
  flags: Record<string, boolean>;
  enabled: string[];
  refreshAfterSeconds: number;
  resolvedAtUtc: string;
}
