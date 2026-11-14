# Feature flags & PT module (branch `Validation_1_PrivateSessions`)

## How flags work
- **Registry in code:** `src/Common/GymApp.Common.Application/FeatureFlags/FeatureRegistry.cs` (key, Arabic/English text, module, apps). Seeded to `feature_flags.*` on startup by `FeatureFlagSeeder`.
- **Resolution:** override > preset > OFF (`FeatureResolver`). Presets: `pt_module` (7 PT features), `business` (all features). Settings (`pt.auto_expiry`) default OFF everywhere.
- **Read flags only through** `IFeatureFlagService` (.NET), `FeatureFlagService` (Flutter, `gymapp_core`) and `FeatureFlagService` (web, `@gym360/ui` → `lib/feature-flags`). Swap the implementation to change provider (e.g. self-hosted Flagsmith): .NET = register another `IFeatureFlagService`; Flutter = another `FeatureFlagService` class; web = `setFeatureFlagService(...)`. Feature code doesn't change.
- **API enforcement:** `FeatureFlagEnforcementMiddleware` → `403 FEATURE_DISABLED:<key>`. Owner of each route: `.RequireFeature(key)` / `.AlwaysOn()` on the endpoint, else `FeatureRouteMap`. `FeatureRouteCoverageTests` fails if a route has neither.
- **Cache:** Redis `ff:v1:{gymId}` (5 min), invalidated on every change. Apps refresh on start, login, resume and every 5 min.
- **How fast a change shows:** the staff web refreshes at once (SignalR `config_updated`) or on the next tab focus. Mobile apps refresh on resume/next open, or within 5 minutes while open. A screen that calls a now-disabled endpoint refreshes its flags straight away.
- **Fail-safe (all apps):** if the fetch fails, the last cached flags are used. With no cache, the apps use the `pt_module` defaults. They never fall back to all on.

## Add a new flag
1. Add a constant to `FeatureKeys` and an entry to `FeatureRegistry.All` (set `InPtModule`).
2. Tag its endpoints with `.RequireFeature(FeatureKeys.X)` (or add a rule to `FeatureRouteMap`).
3. Gate background jobs/notifications with `IFeatureFlagService.IsEnabledAsync`.
4. Mirror the key in `gymapp_core/.../feature_flag_keys.dart` and `@gym360/ui/src/lib/feature-flags/keys.ts`; a web test fails if the TS keys drift from .NET. Gate the UI with `FeatureFlagGate` and the app's route guard (Flutter `core/feature_flags.dart`, staff web `lib/feature-flag-rules.ts`).
5. Deploy: the seeder adds it to the dashboard and to both presets (values from the registry).

## `progress.inbody` (InBody scan)
- Off in `pt_module`, on in `business`. Legacy plan code `inbody_integration` maps to it.
- API: `POST /progress/body-metrics/inbody` (`.RequireFeature`) + handler check. Trainee app: InBody card on My Progress and route `/progress/inbody` (route guard). See FOUNDER_DECISIONS Q36.
- Existing gym schemas get the new columns from `BodyCompositionSchemaPatch` on startup.

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
- Staff web (Next.js `apps/gym-staff`): flag-driven sidebar, dashboard, quick actions, member/trainer tabs and KPI cards. A route guard covers typed URLs and deep links. New PT pages: `/pt/trials`, `/pt/review`, `/pt/dashboard`. Assign Trainer can start a free 1–3 session trial.

## Tests
- .NET: `dotnet test` (flag resolution, enforcement, Super Admin-only API, PT logic, route coverage, PT journey integration test).
- Flutter: `flutter test` in `packages/gymapp_core` and each app.
- Web logic: `cd gym-360-web && npm run test:unit` (fail-safe defaults, cache fallback, FEATURE_DISABLED parsing, staff route rules, key parity with .NET).

## Screens per app with `pt_module`
Anything not listed is blocked by navigation (no menu item), by the route guard (typed URL or deep link → home) and by the API (`403 FEATURE_DISABLED:<key>`).

| App | Visible with pt_module | Hidden (flag) |
|---|---|---|
| Trainee mobile | login, register, consent, profile setup, link gym, home (PT package + remaining sessions, confirm/dispute), progress + measurements/weight, my sessions + session detail/feedback, packages (`/more/membership`), trainer profile, notifications, settings | workouts/history/exercises (training.workouts), guided workout (training.remote_workout), photos (progress.photos), booking (scheduling.booking), classes, nutrition, chat, announcements, leaderboard (gamification.member_leaderboard), rate trainer (pt.coach_ratings), check-in (gym.attendance) |
| Trainer mobile | login, register, pending approval, home (one-tap session log), clients + client detail + measurements, client packages, notifications, settings, profile | workouts/templates/exercises (training.workouts), programs (training.programs), schedule/availability/booking requests/recurring/leave and live session (scheduling.booking), chat, broadcast (communication.announcements), form check (progress.photos), nutrition, earnings (gym.finance), ratings (pt.coach_ratings), client onboarding/notes (gym.member_records), assign workout (training.workouts) |
| Staff mobile | login, PT desk dashboard, members (list/detail/add/edit, assign coach + trial), trainers (list/detail/pending), PT packages (templates/assign/active), PT trials, disputed sessions, PT dashboard (owner/manager), staff + add staff, gym profile, amenities, notifications, seasonal hours, sections, session policies | classes, exercises (training.workouts), kiosk/check-in/kiosk PIN (gym.attendance), record payment/revenue (gym.finance), announcements, coach leaderboard, ratings (pt.coach_ratings), testimonials, member leaderboard, permission matrix (staff.custom_permissions), branding (gym.branding) |
| Staff web | login, select gym, dashboard (members/trainers KPIs + PT desk cards), members + detail (Profile, Sessions), Assign Trainer (paid package or free trial), trainers + detail (Profile, Members, Sessions), PT trials, disputed sessions, PT dashboard (owner/manager), staff, settings (without branding), my subscription | classes, exercises, messages (communication.chat), billing + member Payments tab + trainer Earnings/rate (gym.finance), gamification (member/coach leaderboards, testimonials), announcements, analytics, check-ins and kiosk PIN (gym.attendance), data import, trainer Ratings tab (pt.coach_ratings), member Progress placeholder (progress.photos/training.workouts) |

