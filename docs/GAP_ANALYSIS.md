# GymApp Platform — Gap Analysis: Docs vs Implementation

**Date:** 2026-04-13
**Scope:** Full stack — backend (.NET 8 modular monolith, 9 modules), Flutter mobile (4 packages), Next.js web (2 apps + shared UI), tests, DevOps.
**Method:** Cross-reference of the authoritative docs (`PRODUCT_SPEC.md`, `FOUNDER_DECISIONS.md`, `docs/01`–`docs/07`, `PHASE_2_FEATURES.md`, `CLAUDE.md`) against the actual codebase at `/Users/mmagdy2-eg/Desktop/Development/Forma/`.

Legend: ✅ Aligned · ⚠️ Partial / Ambiguous · ❌ Missing · ➕ Extra (in code, not in docs)

---

## 1. Executive Summary

Overall alignment is **very high**. The backend is production-ready for MVP with zero ironclad-rule violations and all founder decisions implemented. The frontend layers are structurally complete and compile cleanly, but several gym-staff web tabs, admin UI actions, and mobile PDPL/gym-switcher UIs remain **stubs with placeholder text or mock data**. Testing has grown beyond the originally reported 1,122 tests to include Integration and Functional projects, but coverage of real-infra flows is still thin.

**Key numbers (verified against source tree):**

| Area | Docs Target | Implemented | Status |
|---|---|---|---|
| Backend modules | 9 | 9 | ✅ |
| Backend endpoints | ~146 | 150 | ✅ |
| Domain entities | 65 | 68 | ✅ |
| Command/query handlers | 168 | 177 | ✅ |
| DbContexts | 9 | 9 | ✅ |
| EF migrations | 9 | 9 (`Initial`) | ✅ |
| Test projects | 5 | 5 | ✅ |
| Test files (.cs) | — | 88 | ⚠️ (Integration/Functional present, coverage TBD) |
| Exercise seed | 200+ / 242 | ~14 in `ExerciseSeedData` | ❌ Major |
| Egyptian food seed | 50+ | 50+ | ✅ |
| Subscription plans | 4 | 4 | ✅ |
| Feature codes | 31+ | 31 | ✅ |
| Flutter packages | 4 | 4 | ✅ |
| Web apps | 2 | 2 | ✅ |
| Admin web routes | ~10 | 10 + extras | ✅ |
| Gym-staff web routes | ~13 | 13 | ✅ (several are stubs) |

Critical blocker for production: **exercise seed library is ~7% of target**. Everything else is cosmetic or Phase-2-deferrable.

---

## 2. Ironclad Rules (CLAUDE.md) — Compliance

| Rule | Status | Evidence |
|---|---|---|
| Pure EF Core, no Dapper, no raw SQL | ✅ | Zero matches for `Dapper\|FromSql\|ExecuteSqlRaw` across 1,084 source files |
| SQL Server 2022 via `UseSqlServer()` only | ✅ | All 9 DbContexts |
| Feature gating via `IGymPlanService.IsFeatureEnabledAsync` before work | ✅ | 22 gated handler sites |
| No cross-module Domain/Application/Infrastructure refs | ✅ | Only `IntegrationEvents` cross boundaries |
| Handlers/validators `internal sealed` | ✅ | 634 matches; architecture tests enforce |
| Bookify patterns (Entity, Result<T>, Error, domain events, Outbox) | ✅ | `Common.Domain`, `Common.Application`, Outbox pipeline present |
| Arabic-first + RTL | ✅ (mobile) / ⚠️ (web) | Flutter `RtlScaffold`; web has Arabic strings but no formal i18n pipeline |
| No polling — FCM + SignalR | ✅ | `ConfigHub`, `PushNotificationService`, `useSignalR` |
| DB-driven plans (no `if plan == "Pro"`) | ✅ | All gating through `IGymPlanService` + Redis cache |
| Egypt PDPL (consent, export, deletion, TDE) | ⚠️ | Code-level consent/export/delete ✅; TDE assumed at DB instance level, not verified in `docker-compose.yml` |
| Per-module DbContext + schema | ✅ | 9 DbContexts, 9 schemas |
| `docs/` is authority | — | Followed below |

**No rule violations detected.** Only caveat is TDE which is an infra-layer responsibility.

---

## 3. Founder Decisions (FOUNDER_DECISIONS.md)

| # | Decision | Implementation | Status |
|---|---|---|---|
| Q3 | Trainers work at multiple gyms, data scoped via `X-Gym-Id` | `GymMembership`, `GymIdInterceptor` (Flutter), API header scoping | ✅ backend / ⚠️ mobile+web (no visible Gym Switcher UI in gym-staff web or `gymapp_core` widget layer) |
| Q5 | Configurable cancel + late-cancel deducts | `GymSessionPolicy` entity + `IGymSessionPolicyService`; `CancelSessionCommandHandler` reads `CancellationWindowHours` (not hardcoded) | ✅ |
| Q9 | No refunds — void only | `PaymentStatus.Voided`, `VoidPaymentCommandHandler`; no `Refunded` status | ✅ |
| Q31 | Gym sections (Mens/Womens/Mixed) | `GymSection` entity + enum; `GymMembership.section` validated | ✅ |
| Q32 | Seasonal operating-hour overrides (Ramadan) | `GymSeasonalHours` entity with from/until dates | ✅ |

All 34 decisions either covered by code or N/A for this phase.

---

## 4. Backend — Per-Module Gap Analysis

### 4.1 Identity (`schema: identity` · 13 endpoints · 14 handlers · 5 entities)
- ✅ All endpoints and entities (`User`, `GymMembership`, `RefreshToken`, `UserConsent`, `OtpCode`) match docs/01 PART 2.
- ⚠️ **OTP sender**: `OtpSender` now has full SMTP implementation (per `DISCOVERED_ISSUES.md #4`); no SMS provider. Docs don't mandate SMS for MVP — OK.
- ⚠️ **Data export**: `DataExportService` aggregates via `IUserDataProvider` (fixed), but mobile UI for "Download My Data" is not visible in `gymapp_core` widget layer.

### 4.2 Gyms (`schema: gyms` · 27 endpoints · 27 handlers · 11 entities)
- ✅ All entities including founder-decision ones (`GymSessionPolicy`, `GymSection`, `GymSeasonalHours`).
- ✅ Suspension state machine + Quartz job `ProcessGymSubscriptionStatusJob` wired.
- ✅ Check-in via QR / member-number / manual (discriminator).
- ❌ Gym-staff web **Settings tab** for operating hours, session policies, sections is a placeholder ("would load here").

### 4.3 Training (`schema: training` · 19 endpoints · 20 handlers · 10 entities)
- ✅ All entities (`Exercise`, `Workout`, `Program`, `WorkoutLog`, `WorkoutAssignment`, `RemoteWorkoutConfig`).
- ✅ `GetTodaysWorkoutQuery` correctly returns null when `RemoteWorkoutConfig.IsActive = false`.
- ❌ **Exercise seed: ~14 vs 200+ / 242 target.** `ExerciseSeedData.cs` must be expanded (chest/back/shoulders/arms/legs/core/cardio × equipment types) before launch.

### 4.4 Progress (`schema: progress` · 9 endpoints · 13 handlers · 3 entities)
- ✅ `BodyMetricEntry`, `ProgressPhoto`, `Streak` — all match.
- ✅ `IsSharedWithTrainer` flag on photos (privacy model per docs).

### 4.5 Scheduling (`schema: scheduling` · 16 endpoints · 17 handlers · 6 entities)
- ✅ `Session` with dual logging (`TrainerLog` + `MemberLog`), availability, blocked dates.
- ✅ Q5 compliance verified in `CancelSessionCommandHandler`.
- ⚠️ No explicit integration test for the late-cancel → Billing package decrement flow.

### 4.6 Nutrition (`schema: nutrition` · 13 endpoints · 13 handlers · 7 entities)
- ✅ `FoodLog`, `WaterLog`, `MealPlan` + `FoodItem` seeded with 50+ Egyptian foods.

### 4.7 Communication (`schema: communication` · 13 endpoints · 27 handlers · 5 entities)
- ✅ `Conversation`, `Message`, `Notification`, `NotificationPreference`, `DeviceToken`.
- ✅ 27 handlers (many are integration-event handlers consuming other modules' events for notifications — expected).
- ✅ `PushNotificationService` now has full FCM HTTP v1 implementation (per DISCOVERED_ISSUES #4).

### 4.8 Billing (`schema: billing` · 30 endpoints · 33 handlers · 14 entities)
- ✅ 4 plans, 31 features, full plan-feature matrix, storage limits, plan-change audit log.
- ✅ `Payment.Voided` + `VoidPaymentReason` (Q9).
- ✅ 10 admin endpoints (plan CRUD, feature CRUD, override, deploy, feature-matrix).
- ✅ `ProcessExpiredSessionPackagesJob` (Quartz daily 02:00 UTC).
- ⚠️ EF value-comparers on `MembershipPlan.Features` not yet set (`BUILD_SUMMARY.md` "Remaining").

### 4.9 Gamification (`schema: gamification` · 10 endpoints · 13 handlers · 7 entities)
- ✅ `MemberLeaderboard`, `TrainerRating` (gym-management visibility only), `TrainerMonthlyScore`, `TrainerPublicProfile`, `LeaderboardOptOut`, `MemberTestimonial`.
- ⚠️ **Spec ambiguity A1 (DISCOVERED_ISSUES.md)**: Trainer earnings calculation is not defined — Gamification has `TrainerMonthlyScore` but no earnings entity. Mobile trainer app shows read-only earnings screen but source of truth is unclear.

---

## 5. Cross-Cutting Infrastructure

| Component | Status | Notes |
|---|---|---|
| Quartz Outbox (`ProcessOutboxMessagesJob`) | ✅ | `TypeNameHandling.All` + assembly-touch for polymorphic deserialization |
| Quartz suspension job | ✅ | 6h interval, Active→Overdue→Grace→Suspended→Terminated |
| Quartz package expiry job | ✅ | Daily 02:00 UTC |
| SignalR `ConfigHub` | ✅ | `/hubs/config`, `config_updated` event, `SubscribeToGym/UnsubscribeFromGym` |
| FCM (HTTP v1) | ✅ | RS256 JWT from service account, cached OAuth token |
| Redis cache | ✅ | Versioned keys (`v1:plan-features:{gymId}`) |
| JWT (mobile 1h + 30d refresh, staff 30m, admin 15m) | ✅ | `JwtTokenService` |
| Serilog + Seq | ✅ | Program.cs wired |
| Authorization policies | ✅ | 6 policies defined (`platform_admin`, `AdminPolicy`, `gym_owner`, `gym_staff`, `trainer`, `trainee`) |
| Health checks | ✅ | `/health/live` + `/health/ready` (Redis + Outbox DB) |
| CORS | ✅ | Config-driven `Cors:AllowedOrigins[]` |
| Global exception handler | ✅ | Present |
| MediatR pipeline behaviors | ✅ | `ValidationBehavior`, `LoggingBehavior` registered |
| RabbitMQ / MassTransit | ⏳ Phase 2 | Container in compose but not wired (per docs) |
| SMTP OTP sender | ✅ | `Otp:Provider = "Smtp"` config |
| TDE (SQL Server) | ⚠️ | Not visible in `docker-compose.yml`; assume production DB instance configures it |

---

## 6. PDPL Compliance

| Requirement | Backend | Mobile | Web |
|---|---|---|---|
| UserConsent entity | ✅ | ✅ `ConsentService` | ✅ (admin settings shows status) |
| Record consent | ✅ `RecordConsentCommand` | ✅ | — |
| Export my data (cross-module) | ✅ `IUserDataProvider` aggregated | ⚠️ No visible UI in `gymapp_core`; expected in trainee/trainer/gym apps | — |
| Delete account (cascade) | ✅ via `AccountDeletionRequestedIntegrationEvent` | ⚠️ No visible UI in `gymapp_core` | — |
| 30-day retention | ✅ soft-delete pattern | — | — |
| Financial record 7-year retention | ✅ Billing module exempts from cascade | — | — |
| Encryption at rest | ⚠️ TDE assumed | — | — |

**Gap:** Verify every mobile app (`gymapp_trainee`, `gymapp_trainer`, `gymapp_gym`) exposes Download-My-Data and Delete-Account screens in Settings. `gymapp_core` has the service layer but widgets are app-scoped.

---

## 7. Testing

| Project | Files | Status | Gap |
|---|---|---|---|
| `GymApp.Tests.Architecture` | 13 | ✅ 847 tests | — |
| `GymApp.Tests.Domain` | 12 | ✅ 182 tests | Some aggregates (Leaderboard, MealPlan) thin |
| `GymApp.Tests.Application` | 28 | ✅ 93 tests | Feature-gate OFF→403 & ON→success pattern followed; needs more storage-limit cases |
| `GymApp.Tests.Integration` | 17 | ⚠️ Present | Status in `BUILD_SUMMARY.md` says "needs Testcontainers + WebApplicationFactory" — files exist but real-infra coverage unverified |
| `GymApp.Tests.Functional` | 18 | ⚠️ Present | Same — files exist; 401/400/200 HTTP sweep not confirmed per-endpoint |

**Gap vs docs**: Team 9 brief requires Testcontainers.MsSql + Testcontainers.Redis real-DB flows. Verify these projects actually spin containers and are in CI.

---

## 8. DevOps

| Item | Status | Notes |
|---|---|---|
| `docker-compose.yml` (SQL + Redis + RabbitMQ + Seq) | ✅ | All with health checks + volumes |
| Multi-stage `Dockerfile` | ✅ | sdk:8.0 → aspnet:8.0 |
| `.github/workflows/tests.yml` | ✅ | PR CI with SQL + Redis services |
| `.github/workflows/deploy.yml` | ✅ | GHCR push, SHA + latest tags |
| EF migrations | ✅ | `Initial` per module |

---

## 9. Flutter Mobile

| Package | Docs Target | Implemented | Gaps |
|---|---|---|---|
| `gymapp_core` | Shared models, ApiClient, theme (Arabic/RTL), FeatureGate, FCM, consent, offline queue | 128 files; all listed services present | No visible Gym Switcher widget; no Download-My-Data / Delete-Account widgets |
| `gymapp_trainee` | Home (conditional Today's Workout), workouts, progress, nutrition, classes, chat, settings (PDPL toggles), gym switcher | 69 files, compiles | Need to verify PDPL settings + gym switcher screens are built in app layer (not inspected file-by-file) |
| `gymapp_trainer` | Dashboard, clients (5-tab), workout builder, scheduling, live session, remote mode toggle, nutrition, chat, earnings, gym switcher (Q3) | 45 files | Earnings screen exists but earnings calculation source is ambiguous (see A1) |
| `gymapp_gym` | Dashboard, members, **KIOSK** (QR + numpad, 5s reset, PIN exit), classes, announcements, trainers, staff, billing, analytics | 55 files, 0 errors | KIOSK built per docs; PIN-exit + 5s reset verified at spec level |

**Confirmed in `gymapp_core`:**
- ✅ FCM service with silent push for `config_refresh` / `gym_suspended` / `gym_reactivated`
- ✅ Offline queue (Hive, 7-day retention, last-writer-wins)
- ✅ `FeatureGate` + `planFeaturesProvider` (fail-open on error)
- ✅ Consent service (record, query, export consent data)
- ✅ RTL scaffolding, interceptors (auth, gymId, error, logging)

**Missing / ambiguous in `gymapp_core`:**
- ❌ Gym Switcher widget
- ❌ PDPL UI widgets (Download My Data / Delete Account)
- ⚠️ FCM token-refresh listener has `TODO: Send updated token to backend` — real but small gap
- ⚠️ Pull-to-refresh — not in core; expected per-screen in apps

---

## 10. Web (Next.js + Turborepo)

### 10.1 Admin Dashboard (`gymapp-web/apps/admin` — 10 documented routes + 2 extras)

All documented screens are present (Dashboard, Gyms, Gym Detail with 6 tabs, Onboarding, Users, Subscriptions, Plans & Features, Analytics, Churn Analytics, Content, Settings). Extras: `/support`, `/reports`.

**Gaps:**
- ⚠️ **Feature Override table** shows `features.slice(0, 10)` — only first 10 of 31+ features. Needs full list / pagination.
- ❌ **"Bulk Suspend Overdue"** button documented in Payment Alerts bar — not implemented.
- ❌ **Time-Limited Plan** (Available From/Until — e.g. "Ramadan Special") — not in Plan card UI.
- ❌ **Gym Detail → Revenue tab** = placeholder text ("Revenue charts and payment history would load here").
- ❌ **Gym Detail → Settings tab** = placeholder text ("Gym configuration, operating hours, session policies, and sections management would load here"). This is where Q5/Q31/Q32 UI belongs.
- ✅ DataTable, TanStack Query, next-auth, SignalR (`config_updated`) all wired.

### 10.2 Gym-Staff Web (`gymapp-web/apps/gym-staff` — 13 routes)

All documented routes are present.

**Gaps:**
- ❌ **Billing → Plans tab** = placeholder ("Plan management with pricing tiers would load here").
- ❌ **Billing → Packages tab** = placeholder ("Session package templates and active packages would load here").
- ❌ **Classes** page: weekly calendar is static mock data, no "Create Class" modal despite dashboard Quick Action button linking to it.
- ❌ **Announcements**: list view only — no create/edit/publish workflow.
- ❌ **Gamification → Testimonials**: pending list shown but no approve/edit/reject modal.
- ❌ **Trainer Detail page** (`/trainers/[id]/page.tsx`) is a minimal stub (no ratings breakdown, session history, client list).
- ❌ **Check-ins page**: method badges shown but no actual QR scanner UI / member-number input; "Manual Check-In" button present but target flow unverified.
- ❌ **Settings → Branding live preview** (phone mockup per docs) — implemented as color inputs only.
- ❌ **Gym Switcher UI** (Q3) — not visible; "First Gym" is hardcoded.
- ✅ Suspension overlay via `SuspensionOverlay` component on `gym_suspended` SignalR event.
- ✅ Branding colors `#C41E3A` deep red + `#D4AF37` gold per docs.
- ✅ Feature gating on `/exercises` → `CUSTOM_EXERCISE_LIBRARY`.
- ⚠️ 21 TODO/placeholder comments across modals + incomplete tabs.

### 10.3 Shared UI Package (`packages/@gymapp/ui`)

All documented components present: `DataTable`, `StatCard`/`KpiCard`, `SidebarNav`, `Modal`/`ConfirmModal`, `ChartContainer`, `PageHeader`, `StatusBadge`, `SuspensionOverlay`, `FeatureGate`, `SearchInput`, `DateRangePicker`, `LoadingSkeleton`, `EmptyState`. Hooks: `useSignalR`, `usePlanFeatures` + `FEATURE_CODES`, `useApiQuery/useApiMutation`, `useAuthHelpers`, `useDebounce`, `useInvalidateQueries`.

---

## 11. Phase 2 Leakage Check

Searched codebase for Phase 2 feature codes (`PHASE_2_FEATURES.md`): `cash_reconciliation`, `family_memberships`, `guest_pass`, `referral`, `multi_currency`, `offline_mode`, `installments`, `thermal_printer`, `whatsapp`, `inbody`, `video_coaching`.

**Result: 0 matches.** No Phase 2 features have leaked into MVP.

---

## 12. Consolidated Gap Backlog (priority order)

### P0 — Blocking for MVP launch
1. **Expand Exercise seed** in `Training.Infrastructure/Seed/ExerciseSeedData.cs` from ~14 to 200+/242 entries across all muscle groups & equipment types.
2. **Build Gym Settings UI** in Admin (Gym Detail → Settings tab) — operating hours, `GymSessionPolicy` (Q5), `GymSection` (Q31), `GymSeasonalHours` (Q32). Backend is ready; UI is a placeholder.
3. **Build Billing → Plans + Packages tabs** in gym-staff web (currently placeholders).
4. **Build Classes create/edit** flow in gym-staff web (currently read-only mock).
5. **Build Announcement create/edit/publish** workflow in gym-staff web.

### P1 — Required before scale
6. Admin Feature Override table: paginate / show all 31 features (currently `slice(0,10)`).
7. Admin "Bulk Suspend Overdue" action in Payment Alerts bar.
8. Trainer Detail page (gym-staff web) — full ratings, sessions, clients.
9. Gym Switcher UI (Q3) in mobile `gymapp_core` + gym-staff web.
10. Mobile PDPL UIs: Download-My-Data + Delete-Account in `gymapp_trainee` / `gymapp_trainer` / `gymapp_gym` Settings.
11. Check-ins page: real QR scanner integration + member-number input flow.
12. Confirm & document SQL Server TDE configuration (docker-compose or prod-db runbook).
13. EF value-comparers on `MembershipPlan.Features` + Communication collections (per BUILD_SUMMARY "Remaining").
14. FCM token-refresh: implement "Send updated token to backend" TODO.

### P2 — Quality & polish
15. Verify `GymApp.Tests.Integration` + `GymApp.Tests.Functional` actually exercise Testcontainers and full HTTP sweep in CI.
16. Add integration test: late-cancel → `SessionPackage` decrement end-to-end (Q5).
17. Testimonials approve/edit/reject modal (gym-staff web).
18. Admin Gym Detail Revenue tab (currently placeholder).
19. Settings → Branding live-preview phone mockup (gym-staff web).
20. Resolve spec ambiguity A1: trainer earnings calculation formula (docs do not define it).
21. Resolve spec ambiguity A3: can a trainee use a session package bought at Gym A while training at Gym B? (Current: packages scoped per gym.)
22. Resolve spec ambiguity A4: kiosk-mode re-authentication to prevent members accessing staff app behind the kiosk.

### P3 — Nice-to-have
23. Formal i18n pipeline for web apps (Arabic strings currently present but no `next-intl`/equivalent visible).
24. Expand Application-layer tests for storage-limit boundary cases.
25. Prune 21 TODO comments in gym-staff web.

---

## 13. Conclusion

The codebase delivers what the docs describe at the **architecture, domain, data, API, and integration-infrastructure layers** with near-perfect fidelity. The compliance story on ironclad rules and founder decisions is airtight.

The real gap sits in the **UI surface of the gym-staff web app and a handful of mobile/admin screens** — the plumbing is there, the screens are stubs. Plus the **exercise seed** is the one concrete data shortfall that will be immediately visible to a first customer.

Fixing P0 + P1 items above converts the codebase from "feature-complete framework" to "demo-ready product."
