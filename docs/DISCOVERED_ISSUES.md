# GymApp Platform — Discovered Issues

Issues, ambiguities, and improvements discovered during implementation that were NOT in the original docs.

## Critical (Must Fix Before Production)

### 1. IUnitOfWork DI Ambiguity — **RESOLVED (2026-04-12)**
**Location**: All 9 module registration files
**Was**: Each module registered `IUnitOfWork` as its DbContext: `services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>())`. Since all 9 modules did this, the last registration won in DI, meaning any handler that injected `IUnitOfWork` directly got the wrong DbContext — silent data loss when `SaveChangesAsync()` was called.
**Resolution**: Created 9 per-module marker interfaces (`IIdentityUnitOfWork`, `IGymsUnitOfWork`, …, `IGamificationUnitOfWork`) extending `IUnitOfWork` in each module's `Application/Abstractions/`. Each DbContext implements its module-specific interface. Registrations now bind the unique per-module interface. 100 handler files + 1 service (`NotificationService`) + 19 test mocks updated.

### 2. MediatR Pipeline Behaviors Not Wired — **RESOLVED (2026-04-12)**
**Location**: `src/API/GymApp.Api/Program.cs`
**Was**: `ValidationBehavior<,>` and `LoggingBehavior<,>` from Common.Application were not registered. FluentValidation validators existed but wouldn't run automatically.
**Resolution**: Made both behaviors `public sealed`. Wired via `cfg.AddOpenBehavior(typeof(LoggingBehavior<,>))` and `cfg.AddOpenBehavior(typeof(ValidationBehavior<,>))` in Program.cs MediatR registration.

### 3. No EF Core Migrations — **RESOLVED (2026-04-12)**
**Location**: All 9 Infrastructure projects
**Was**: No migrations existed; database could not be created.
**Resolution**: Generated `Initial` migration for all 9 modules (3 files each: migration, designer, snapshot). Fixed 3 EF configuration issues discovered in the process:
- Gyms: `IReadOnlyList<string/Guid>` backing fields (`_amenities`, `_memberIds`, `_trainerIds`) must be configured via `PrimitiveCollection<List<T>>("_fieldName")` not via the read-only property expression.
- Training: `HasData` seed values must use CLR enum types (not `.ToString()`) and `List<MuscleGroup>` for the custom-converter property.
- Nutrition: `PlannedMeal` and `FoodItem` positional records needed a `private PlannedMeal() : this(...)` constructor so EF Core's JSON deserializer selects the parameterless constructor over the compiler-generated copy constructor.

### 4. Placeholder Services — **RESOLVED (2026-04-12)**
**Location**: Identity.Infrastructure, Communication.Infrastructure
**Was**: All three services were no-op loggers.
**Resolution**:
- `OtpSender` — full SMTP implementation via `System.Net.Mail.SmtpClient`. Configurable: `Otp:Provider = "Log"` (dev) or `"Smtp"` (production). No external packages required. Email template includes Arabic-friendly HTML layout.
- `DataExportService` — rebuilt using `IEnumerable<IUserDataProvider>` aggregation (see #5).
- `PushNotificationService` — full FCM HTTP v1 implementation: RS256 JWT from service account key (`RSA.ImportPkcs8PrivateKey` + `Microsoft.IdentityModel.Tokens.Jwt`), OAuth2 token exchange, per-device send, cached access token with double-check locking. Dev-mode bypass: `Fcm:IsEnabled = false`.

## High Priority

### 5. Cross-Module Data Export — **RESOLVED (2026-04-12)**
**Was**: Export only returned Identity data.
**Resolution**: Created `IUserDataProvider` interface in `Common.Application.Abstractions`. All 9 modules implement it and register via `services.AddScoped<IUserDataProvider, UserDataProvider>()`. `DataExportService` injects `IEnumerable<IUserDataProvider>` and aggregates. Also discovered and fixed: 7 of 9 modules were missing `AddMediatR` + `AddValidatorsFromAssembly` in their module registrations — all handlers in Training, Progress, Scheduling, Nutrition, Communication, Billing, Gamification were never registered.

### 6. Account Deletion Cascade — **RESOLVED (2026-04-12)**
**Was**: Deletion only soft-deleted the Identity user; other modules retained all data.
**Resolution**: Created `AccountDeletionRequestedIntegrationEvent` in Identity.IntegrationEvents. `DeleteAccountCommandHandler` now publishes it via `IPublisher` after save. Handlers added in Training, Progress, Scheduling, Communication, Billing — each anonymizes/cancels relevant user data. Financial records retained per PDPL 7-year rule.

### 7. Integration Events IDomainEvent Base — **RESOLVED (2026-04-12)**
**Was**: 4 integration events were missing `: IDomainEvent`.
**Resolution**: Fixed `UserRegisteredIntegrationEvent`, `UserLinkedToGymIntegrationEvent`, `TrainerLoggedSessionIntegrationEvent`, `LeaderboardRecalculatedIntegrationEvent`.

### 8. Outbox Message Type Resolution — **RESOLVED (2026-04-12)**
**Was**: Domain event assemblies might not be loaded when the outbox job deserializes messages from other modules.
**Resolution**: Added explicit assembly-touch statements in `Program.cs` before app build — forces all 9 Domain assemblies into the AppDomain before the outbox processor starts. Uses `TypeNameHandling.All` in Newtonsoft.Json for full type fidelity.

## Medium Priority

### 9. Redis Cache Key Versioning — **RESOLVED (2026-04-12)**
**Was**: Cache keys had no version prefix; schema changes would serve stale data.
**Resolution**: Changed `GymPlanService` cache key from `plan-features:{gymId}` to `v1:plan-features:{gymId}`.

### 10. Gym Suspension State Machine — **RESOLVED (2026-04-12)**
**Was**: Suspension state machine required manual admin action.
**Resolution**: Added `ProcessGymSubscriptionStatusJob` (Quartz, `[DisallowConcurrentExecution]`, 6-hour interval) in `Gyms.Infrastructure`. Cross-module pattern: reads `billing.gym_subscriptions` via `GymSubscriptionReadModel` without breaking module boundaries. Transitions: Active→Overdue (3d late), Overdue→GracePeriod (7d overdue), GracePeriod→Suspended (grace expired), Suspended→Terminated (30d suspended).

### 11. Session Package Expiry — **RESOLVED (2026-04-12)**
**Was**: Expired packages could still be used; nothing marked them expired.
**Resolution**: Added `ProcessExpiredSessionPackagesJob` (Quartz, daily cron at 02:00 UTC) in `Billing.Infrastructure`. Also added expiry guard in `BookSessionCommandHandler` via `ISessionPackageService` — returns error immediately if package is expired at booking time.

### 12. Missing Authorization Policies — **RESOLVED (2026-04-12)**
**Location**: API/GymApp.Api/Program.cs
**Was**: Endpoints referenced authorization policies (`platform_admin`, `AdminPolicy`, etc.) but these weren't defined. Only `AddAuthorization()` was called, so policy-gated endpoints would fail.
**Resolution**: Replaced `AddAuthorization()` with `AddAuthorizationBuilder()` defining 6 policies keyed off JWT claims from `JwtTokenService`: `platform_admin`, `AdminPolicy`, `gym_owner`, `gym_staff`, `trainer`, `trainee`.

### 13. SignalR Hub Not Mapped — **RESOLVED (2026-04-12)**
**Location**: API/GymApp.Api/Program.cs
**Was**: `AddSignalR()` called but no hub mapped. `ConfigHub` class didn't exist.
**Resolution**: Created `src/API/GymApp.Api/Hubs/ConfigHub.cs` with `[Authorize]`, `SubscribeToGym(gymId)`/`UnsubscribeFromGym(gymId)` methods, and `config_updated` event constant. Mapped at `/hubs/config` via `app.MapHub<ConfigHub>(ConfigHub.HubPath)`.

## Low Priority

### 14. Swagger Endpoint Discovery — **NOT A REAL ISSUE**
Minimal API endpoints mapped via `IEndpoint.MapEndpoint()` are auto-discovered by Swagger since they're registered directly on the `WebApplication` route map. Swagger UI shows all 146 endpoints correctly.

### 15. CORS Too Permissive — **RESOLVED (2026-04-12)**
**Was**: `AllowAnyOrigin()` in all environments.
**Resolution**: CORS now reads `Cors:AllowedOrigins[]` from `appsettings.json`. In Development or when list is empty: allows any origin. In Production with configured list: `WithOrigins(allowedOrigins).AllowCredentials()` (needed for SignalR WebSocket).

### 16. Asp.Versioning.Http Unused — **RESOLVED (2026-04-12)**
**Was**: Package referenced but never configured. URL convention `/api/v1/` was the only versioning.
**Resolution**: Removed `Asp.Versioning.Http` package reference entirely. URL prefix convention is the appropriate strategy for a v1-only API and requires no library enforcement.

### 17. Newtonsoft.Json vs System.Text.Json — **BY DESIGN**
Outbox and Redis cache both use `Newtonsoft.Json` intentionally. `TypeNameHandling.All` in Newtonsoft has no equivalent in `System.Text.Json`, making Newtonsoft mandatory for the outbox (which must preserve full type names for polymorphic domain event deserialization). Using Newtonsoft in both places is consistent. The ASP.NET Core request pipeline uses `System.Text.Json` (default) — this is fine since they serve different purposes.

### 18. Missing Health Check Endpoints — **RESOLVED (2026-04-12)**
**Location**: API/GymApp.Api/Program.cs
**Was**: No health endpoints exposed. Docker-compose health check targeted API but nothing was mapped.
**Resolution**: Added `AspNetCore.HealthChecks.Redis` + `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` packages. Registered `AddHealthChecks().AddDbContextCheck<OutboxDbContext>("outbox-db", tags: ["ready"]).AddRedis(…, tags: ["ready"])`. Mapped `/health/live` (liveness, no checks) and `/health/ready` (readiness, filters on `ready` tag).

### 19. Admin Dashboard Endpoints (`/gyms/pending`, `/admin/analytics`) Returning 404 — **RESOLVED (2026-04-17)**
**Location**: `src/Modules/Gyms/...Presentation/Endpoints/` and `src/Modules/Billing/...Presentation/Endpoints/Admin/`
**Was**: Admin dashboard hooks `usePendingGyms` (→ `GET /api/v1/gyms/pending`) and `usePlatformAnalytics` (→ `GET /api/v1/admin/analytics`) both returned 404. `/gyms/pending` had no backing endpoint; `/admin/analytics` had a Billing-module `GetPlatformAnalyticsEndpoint` but its route wasn't being discovered until a clean rebuild re-exercised the explicit `GymApp.Modules.*.Presentation.dll` load in Program.cs.
**Resolution**: Added `ListPendingGymsQuery` + handler + `ListPendingGymsEndpoint` in the Gyms module (pure EF Core, filters `GymStatus == PendingApproval`, ordered by `CreatedAt`). Extended `IGymQueryService` with `ListPendingGymsAsync`. Verified the existing `GetPlatformAnalyticsEndpoint` maps correctly post-rebuild — it returns the full `PlatformAnalyticsDto` shape the admin dashboard expects (KPIs + revenue/growth trends + plan distribution + at-risk gyms). Both endpoints now return 200 with real data under the `platform_admin` policy (`RequireAuthenticatedUser().RequireClaim("adminRole")`, registered in `Program.cs`).

### 20. Frontend Role-Claim Mismatch (login rejected / infinite redirect) — **RESOLVED (2026-04-17)**
**Location**: `gymapp-web/apps/admin/{lib/auth.ts, middleware.ts}`, `gymapp-web/apps/gym-staff/lib/auth.ts`
**Was**: NextAuth adapters expected `user.role === 'platform_admin'` / `'gym_owner'` / `'gym_staff'` but the API login response returns short-form values: `admin`, `owner`, `staff`, `trainer`, `trainee`. Every admin and staff login was rejected with "Invalid credentials or insufficient permissions". Worse, the admin `middleware.ts` duplicated the same check, so even after a successful sign-in it fell into an infinite `/login ↔ /` redirect loop that presented as "infinite loading".
**Resolution**: Admin auth and middleware now accept both `'admin'` and `'platform_admin'` to tolerate either convention. Admin auth decodes `adminRole` from the JWT payload since the API doesn't return it in the user object. Gym-staff `normalizeRole` now maps lowercase short-form (`owner`/`staff`) to the canonical `gym_owner`/`gym_staff` used internally; `staffRole` and `gymId` are pulled from the JWT. See docs/01 "Login response `user.role` values" note for the canonical mapping.

### 21. Paginated Gyms List Breaks Admin Dashboard — **RESOLVED (2026-04-17)**
**Location**: `gymapp-web/apps/admin/lib/hooks/use-gyms.ts`, dashboard page
**Was**: `GET /api/v1/gyms` returns the standard paginated envelope `{ data: { items: [...], page, pageSize, totalCount, totalPages } }`, but `useGyms` was typed as `useApiQuery<GymListItem[]>` so `gymsData.data` was the envelope object. Dashboard threw `TypeError: gyms.filter is not a function`.
**Resolution**: Replaced `useApiQuery` with a custom `useQuery` that unwraps `data.items` and normalises casing (`Active` → `active`, `Paid` → `paid`) so `StatusBadge` variants match, and fills missing display fields (`ownerName`, `planName`, `city`, `monthlyRevenue`) with defaults. `usePendingGyms` now hits the real `/gyms/pending` endpoint (see #19) instead of filtering the list client-side.

## Staff Mobile App — MobAI QA Run (2026-10-10)

End-to-end run of `gymapp_gym` on the iOS simulator through MobAI against the remote dev environment (`https://staging-api.gym-365.com`), using a dedicated QA gym on the Pro plan with owner / manager / receptionist / trainer / member accounts. Every screen in docs/02 PART 6 was exercised and compared with PRODUCT_SPEC.md, docs/01 and docs/02. "Verified" means re-tested on the simulator against staging after the fix.

### Fixed in the app (verified on device)

| # | Issue | Root cause | Fix |
|---|---|---|---|
| S1 | Wrong password showed no error and wiped the login form (all three apps) | `AuthInterceptor` ran the refresh/sign-out flow on a 401 from the public `/identity/login`, which rebuilt the router | 401s from public auth endpoints are passed through to the caller |
| S2 | Owner logging in landed on an empty, zeroed dashboard with no way to sign out | Mobile never called `POST /identity/select-gym`; owner tokens carry no `gymId` | Login scopes the owner to their gym (picker when they own several) |
| S3 | Account without a gym: silent all-zero dashboard, Settings showed "no data" and no sign-out | Sign-out lived inside the gym-loaded branch only | Explicit "not linked to a gym" state on dashboard and Settings, sign-out always reachable |
| S4 | Receptionist got the full manager UI (revenue, staff, templates, moderation) | Login mapped every `staff` to `manager`, ignoring the `staffSubRole` JWT claim | Role resolved from the token; More menu, Settings tiles and the revenue KPI are permission-gated |
| S5 | Permission matrix disagreed with docs/01 and PRODUCT_SPEC §1.4 | Manager was denied packages / staff / branding; receptionist denied add / edit / freeze member | Matrix aligned with the docs |
| S6 | Add Member created the member but showed "already added" and stayed on the form | Redundant `POST /gyms/{id}/members` after `staff-create` (which already links) | Link only when `staff-create` did not return a member number |
| S7 | "All" chip could not clear the status filter | `copyWith(status: null)` keeps the old value | `MemberFilter.withStatus` |
| S8 | Assign Trainer (member detail) always failed | Path hard-coded without `/api/v1` | `ApiEndpoints.quickAssignPackage` |
| S9 | Frozen members had no badge and could not be reactivated | No unfreeze action | Frozen chip + Unfreeze (`POST .../unfreeze`) |
| S10 | Record Payment / Assign Package: "member not found" for existing members | Lookup searched whatever page / filter the Members tab was showing | Server-side search across the roster; leading zeros ignored |
| S11 | "Session package" payments were always rejected | Backend needs `trainerId` + `sessionCount`; the form had neither | Trainer and session-count inputs for that purpose |
| S12 | Feature gates were open on every plan | Caching plan features in Hive threw (nested objects), the provider errored and `FeatureGate` fails open; several app feature codes also differed from the backend's | Network-first plan features with a JSON-safe cache; feature codes aligned (`qr_checkin`, `member_leaderboards`, `coach_of_the_month`, `remote_workout_mode`, `white_label_branding`, `churn_prediction`) |
| S13 | Custom Exercises showed an open form on Pro | Screen not wrapped in a gate | `FeatureGate(custom_exercise_library)` |
| S14 | Check-in log times were 3 hours off | API timestamps are UTC without `Z` and were shown as local | `DateFormatter.apiUtcToLocal` |
| S15 | Gym sections switches did nothing | Screen was a stub | Wired to `GET` / `PUT /gyms/{id}/sections` |
| S16 | Bottom-nav highlight was wrong on screens opened from More | Shell read its own route state, not the pushed route | Reads the top-most match |
| S17 | Kiosk numpad was mirrored in Arabic; keys were not buttons for assistive tech | Numpad inherited RTL | Numpad forced LTR, keys expose button semantics |
| S18 | Arabic UI issues: "اعتدال الشهادات" for testimonial moderation, founder-question tags ("(Q5…)", "(Q31)", "(Q32)") in labels, English payment methods, raw `Exception:` / `ApiException(403):` text | — | Strings corrected; one error-text helper for all staff snackbars with Arabic for common backend messages |
| S19 | `gymapp_core` tests did not compile | Exhaustive switch over `DioExceptionType` broke on dio 5.10+ | `default` branch |

### Fixed in the backend (unit + integration tests; needs deploy before the app benefits)

| # | Issue | Fix |
|---|---|---|
| S20 | `GET /billing/packages` with only a gym returned `[]` (handler fell back to `trainerId = Guid.Empty`), so staff "Active Packages" is always empty | `ISessionPackageRepository.GetByGymAsync`; the app now calls this endpoint instead of the trainer-scoped `/packages/my-clients` |
| S21 | `PUT /gyms/{id}` rejected managers (policy `gym_owner_or_admin`); adding an amenity as manager returned 403 | New `gym_owner_manager_or_admin` policy |

### Open — backend / environment

| # | Issue | Notes |
|---|---|---|
| S22 | **Staff sub-role is not enforced server-side.** A receptionist token gets 200 on `/billing/revenue`, `/gyms/{id}/staff`, `/gyms/{id}/analytics` and passes authorization on `POST` package-templates / classes / announcements and `PUT` session-policy; `PUT /gyms/{id}/sections` only requires authentication | The app now hides these, but the API must apply `gym_owner_or_manager` to the manager-only rows of the docs/01 matrix |
| S23 | A staff or owner token with no gym can list other owners' gyms (including contact email) via `GET /gyms` | Tenant isolation |
| S24 | On staging, creating an owner through `POST /admin/owners` did not auto-provision a gym (#101) within several minutes | Check outbox processing / deployed build |
| S25 | Staging demo accounts are unusable: `staff@demo.local` and `owner@demo.local` have no gym, `receptionist@demo.local` is rejected | Re-seed or document |
| S26 | Error messages are English regardless of `Accept-Language` | App maps the common ones; proper fix is server-side localisation |
| S27 | `GET /gyms/{id}/analytics` returns only counts and member growth, so the dashboard's revenue chart, top classes, at-risk members, Coach of the Month and activity feed never render | docs/02 PART 6 |
| S28 | Receipt numbers are `RCP-{yyyyMMdd}-{hex}`; PRODUCT_SPEC story 72 says `REC-{GymCode}-{YYYYMMDD}-{SEQ}` | |
| S29 | Duplicate check-in is blocked for the whole day; AC-S27 specifies a 1-hour throttle with `409 GYMS.CHECKIN_THROTTLED` | |
| S30 | No update-member endpoint (`PUT /gyms/{id}/members/{memberId}`) and no next-member-number endpoint | Edit Member screen stays unreachable; stories 22 / 23 |
| S31 | `staff-create` returns `initialPassword: "123456"` for every created user | |

### Open — staff app gaps against the docs

| # | Gap | Doc |
|---|---|---|
| S32 | Add Member has no member-number suggestion / override and no section picker | Stories 22, Q31 |
| S33 | Edit Member is unreachable (blocked by S30) | Stories 21, 23 |
| S34 | Revenue dashboard has no today / week / month view, no memberships-vs-packages split, no void action | Story 73, Q9 |
| S35 | No receipt is shown after recording a payment | Story 72 |
| S36 | Create Class has no recurring option; class cards show time without date; no edit | Story 69 |
| S37 | Package template: no expiry field, no price-per-session, no active toggle | docs/02 PART 6 |
| S38 | Seasonal hours "add" is still a stub; no operating-hours editor; gym profile and branding colours are read-only | Q32, docs/01 matrix |
| S39 | No language switch in the app (Arabic only in practice) | Rule 6 |
| S40 | Manual check-in is number-only; kiosk welcome omits the gym name and photo; a frozen member is told "membership expired" | Story 26, docs/02 PART 6 |
| S41 | Exercise library has no muscle-group / equipment filter and content is English-only | docs/02 PART 6 |
| S42 | Cold start briefly shows the login screen while the saved session is validated | |
| S43 | Icon-only buttons (add FABs, app-bar actions) have no accessibility labels | |
| S44 | PDPL export shows a sandbox file path instead of a share sheet | Rule 9 |
| S45 | The same Hive plan-features caching bug (S12) exists in the trainee and trainer apps | Not touched in this run |

Not coverable in this run: QR scanning (simulator has no camera), trainer approval (owner-added trainers are auto-approved; needs a trainer self-registration), account deletion (destructive), FCM-driven plan refresh, suspension banner.

## Trainee Mobile App — MobAI QA Run (2026-10-14)

Full member cycle of `gymapp_trainee` on the iOS simulator through MobAI against the remote dev environment (`https://staging-api.gym-365.com`) with the demo trainee + trainer (gym `g1`): login (wrong + right password), Home, ad-hoc workout log, weight log, booking a session against the trainer's availability, trainer confirmation, cancellation, sign-out. Trainer-side steps (availability, confirm) were driven through the API because the trainer app on the shared simulator was signed in to another gym.

### Fixed

| # | Issue | Root cause | Fix |
|---|---|---|---|
| M1 | **Any authenticated user could grant session packages.** A trainee token got `201` on `POST /billing/packages/quick-assign` for a free 8-session package with their trainer, then booked against it | `quick-assign` and `assign` only called `RequireAuthorization()` | Both require the `gym_staff` policy (owner / manager / receptionist); functional tests for trainee / trainer `403` |
| M2 | Workout streak, Home weekly activity and Progress "current streak" stayed at 0 after logging workouts | `WorkoutLog` raised a domain event but nothing published `WorkoutLoggedIntegrationEvent`, which Progress's streak handler listens to | `LogWorkoutCommandHandler` enqueues the event (workout date, not UTC clock) in the same transaction; unit + outbox integration test |
| M3 | Home "next session" card stayed empty after booking (and kept a cancelled session) until the app was restarted | Home cached its own copy of `/scheduling/sessions/upcoming`; booking / cancel / feedback only invalidate the Schedule tab's provider | Home's provider mirrors the Schedule provider; verified on device (cancel clears the card) |
| M4 | Wrong password showed the backend's English "The provided credentials are invalid." on the Arabic login screen | Trainee login showed `ApiException.message` as-is (trainer app was fixed in #105) | 401 localized |

### Open

| # | Issue | Notes |
|---|---|---|
| M5 | Start Workout: template picker vanished on the first visit after login and never retried | `workoutsProvider` error branch renders `SizedBox.shrink()`; not reproduced on a second visit |
| M6 | Numeric keypads (reps / kg / RPE, weight) have no Done key, so the keyboard can only be dismissed by tapping Save | |
| M7 | Units and package names are English on the Arabic UI: `82.4 kg`, `0.0L`, quick-assigned packages are named `Custom: 8 sessions` | |
| M8 | Weight curve is blank with a single entry | |
| M9 | Booking slot chips and the week-navigation arrows have no button semantics / labels | Assistive tech and automation see plain text |
| M10 | `quick-assign` / `assign` still trust `AssignedBy` from the request body | Should come from the caller's token |
| M11 | Session `scheduledAt` is gym-local time serialized with a `Z` suffix | Apps display it as-is; any client converting UTC→local will be off by 2–3 h |

Staging data left behind: the demo trainee holds an 8-session package with Demo Trainer (created through M1), the demo trainer has availability 09:00–23:00 every day, and one cancelled session on 16 Oct.

## Spec Ambiguities Found

### A1. Trainer Earnings Read-Only
FOUNDER_DECISIONS.md Q9 says no refunds and earnings are read-only, but doesn't specify how earnings are calculated. Are they: total payments where trainer is assigned? A percentage? Commission-based? Currently, Gamification module has `TrainerMonthlyScore` but no earnings entity.

### A2. Notification Preference Granularity
docs/01 lists `NotificationPreference` per category, but doesn't define all categories. Current implementation uses the 15 notification types as categories. May need a separate preference category enum.

### A3. Multi-Gym Trainer Session Packages
Q3 says trainers work at multiple gyms. If a trainee has a session package at Gym A and their trainer also works at Gym B, can the trainee use the package at Gym B? Current implementation scopes packages per gym.

### A4. Kiosk Mode Security
The gym staff app has KIOSK mode (full-screen QR/numpad check-in). No spec on how to prevent members from accessing the staff app behind the kiosk. May need a separate kiosk authentication mode.
