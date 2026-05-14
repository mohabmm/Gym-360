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

## Spec Ambiguities Found

### A1. Trainer Earnings Read-Only
FOUNDER_DECISIONS.md Q9 says no refunds and earnings are read-only, but doesn't specify how earnings are calculated. Are they: total payments where trainer is assigned? A percentage? Commission-based? Currently, Gamification module has `TrainerMonthlyScore` but no earnings entity.

### A2. Notification Preference Granularity
docs/01 lists `NotificationPreference` per category, but doesn't define all categories. Current implementation uses the 15 notification types as categories. May need a separate preference category enum.

### A3. Multi-Gym Trainer Session Packages
Q3 says trainers work at multiple gyms. If a trainee has a session package at Gym A and their trainer also works at Gym B, can the trainee use the package at Gym B? Current implementation scopes packages per gym.

### A4. Kiosk Mode Security
The gym staff app has KIOSK mode (full-screen QR/numpad check-in). No spec on how to prevent members from accessing the staff app behind the kiosk. May need a separate kiosk authentication mode.
