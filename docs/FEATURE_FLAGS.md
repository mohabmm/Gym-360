# Feature flags & PT module (branch `Validation_1_PrivateSessions`)

## How flags work
- **Registry in code:** `src/Common/GymApp.Common.Application/FeatureFlags/FeatureRegistry.cs` (key, Arabic/English text, module, apps). Seeded to `feature_flags.*` on startup by `FeatureFlagSeeder`.
- **Resolution:** override > preset > OFF (`FeatureResolver`). Presets: `pt_module` (7 PT features), `business` (all features). Settings (`pt.auto_expiry`) default OFF everywhere.
- **Read flags only through** `IFeatureFlagService` (.NET) / `FeatureFlagService` (Flutter, `gymapp_core`). Swap the implementation to change provider (e.g. Flagsmith).
- **API enforcement:** `FeatureFlagEnforcementMiddleware` → `403 FEATURE_DISABLED:<key>`. Owner of each route: `.RequireFeature(key)` / `.AlwaysOn()` on the endpoint, else `FeatureRouteMap`. `FeatureRouteCoverageTests` fails if a route has neither.
- **Cache:** Redis `ff:v1:{gymId}` (5 min), invalidated on every change. Apps refresh on start, login, resume and every 5 min.

## Add a new flag
1. Add a constant to `FeatureKeys` and an entry to `FeatureRegistry.All` (set `InPtModule`).
2. Tag its endpoints with `.RequireFeature(FeatureKeys.X)` (or add a rule to `FeatureRouteMap`).
3. Gate background jobs/notifications with `IFeatureFlagService.IsEnabledAsync`.
4. Mirror the key in `gymapp_core/.../feature_flag_keys.dart`; gate UI with `FeatureFlagGate` and the app's route guard.
5. Deploy: the seeder adds it to the dashboard and to both presets (values from the registry).

## Manage flags (Super Admin dashboard → Feature Flags)
- **Gyms:** search, filter by preset, select several gyms → *Apply preset*.
- **Gym page:** preset dropdown; per-flag toggle creates an override; *Reset to preset* removes it.
- **Presets:** edit pt_module / business; warning shows how many gyms are affected.
- **Audit log:** filter by gym and flag. Only `super_admin` sees the section (JWT now carries the real admin role).

## What this branch built
- Flags backend, Super Admin API + dashboard, audit log, migration assigning existing gyms to `pt_module` (with Business seat limits).
- PT: free trials (1–3 sessions, one per trainee), one-tap coach quick-log + trainee confirm/dispute + desk reverse, coach measurements (own clients only), renewal reminders at exactly 3 and 1 left, trial-end alerts + "not converted" list, owner PT dashboard (conversion per coach, 14-day window, renewals, sessions).
- Package auto-expiry only when `pt.auto_expiry` is ON.
- Flutter: trainee, trainer, staff mobile apps driven by flags with PT-focused homes.
