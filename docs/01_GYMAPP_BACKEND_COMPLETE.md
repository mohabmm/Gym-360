# GymApp — Complete Backend Reference

> **Purpose**: Single document for Claude to implement the entire .NET backend. Contains architecture, all 8 modules, database schemas, API endpoints, testing strategy, Docker setup, and agent instructions. Nothing omitted.

---

# PART 1: ARCHITECTURE

## Tech Stack
- .NET 8 | **EF Core 8** with **SQL Server 2022** (pure EF Core for both reads and writes — no Dapper) | MediatR (CQRS) | FluentValidation | Quartz (Outbox jobs) | Serilog + Seq | Redis (caching) | **RabbitMQ** (ready, Phase 2) | Minimal APIs | Docker

## Architecture Style
**Modular Monolith** following Milan Jovanovic's patterns (Evently course) + **Bookify** Clean Architecture primitives. Single deployable host, 8 independent modules, per-module DbContext with SQL Server schema isolation.

## Solution Structure
```
src/
├── Common/
│   ├── GymApp.Common.Domain/           # Entity, IDomainEvent, Result<T>, Error, IUnitOfWork
│   ├── GymApp.Common.Application/      # ICommand, IQuery, ICommandHandler, IQueryHandler, Behaviors (Logging, Validation, Caching), IDateTimeProvider, ICacheService
│   ├── GymApp.Common.Infrastructure/   # Outbox (OutboxMessage, ProcessOutboxMessagesJob with Quartz), Inbox, Interceptors, Auth, Caching (Redis), Clock
│   └── GymApp.Common.Presentation/     # IEndpoint interface for Minimal APIs
├── Modules/
│   ├── Identity/        → Domain, Application, Infrastructure, Presentation, IntegrationEvents, IdentityModule.cs
│   ├── Gyms/            → Domain, Application, Infrastructure, Presentation, IntegrationEvents, GymsModule.cs
│   ├── Training/        → Domain, Application, Infrastructure, Presentation, IntegrationEvents, TrainingModule.cs
│   ├── Progress/        → Domain, Application, Infrastructure, Presentation, IntegrationEvents, ProgressModule.cs
│   ├── Scheduling/      → Domain, Application, Infrastructure, Presentation, IntegrationEvents, SchedulingModule.cs
│   ├── Nutrition/       → Domain, Application, Infrastructure, Presentation, IntegrationEvents, NutritionModule.cs
│   ├── Communication/   → Domain, Application, Infrastructure, Presentation, IntegrationEvents, CommunicationModule.cs
│   ├── Billing/         → Domain, Application, Infrastructure, Presentation, IntegrationEvents, BillingModule.cs
│   └── Gamification/    → Domain, Application, Infrastructure, Presentation, IntegrationEvents, GamificationModule.cs
└── API/
    └── GymApp.Api/      → Program.cs (host), Dockerfile, appsettings, Middleware, Swagger
```

## Module Layer Rules
- **Domain**: Pure C#. References only Common.Domain. Contains: Entities, VOs, Aggregates, Domain Events, Repository interfaces, Domain Services, Errors.
- **Application**: References Domain only. Contains: Commands, Queries, Handlers (internal sealed), Validators (internal), DTOs, Mappers. Uses MediatR.
- **Infrastructure**: References Application + Domain. Contains: EF Core DbContext, Entity Configurations, Repositories, External service clients.
- **Presentation**: References Application only. Contains: Minimal API endpoints implementing `IEndpoint`.
- **IntegrationEvents**: Standalone leaf project. Contains ONLY `record` classes for cross-module async events.
- **Shared** (optional, per module): Standalone leaf project — references nothing. Contains the module's cross-module **sync** contract interfaces + their DTO records (e.g. `Billing.Shared/IGymPlanService`). Implementation lives in that module's Infrastructure; consumers inject the interface via DI.

### Shared vs Internal — the module boundary

A module's **shared surface** is exactly two projects: `.Shared` (sync contracts) + `.IntegrationEvents` (async events). These are the *only* projects another module may reference. Everything else — `Domain`, `Application`, `Infrastructure`, `Presentation` — is **module-internal**. Enforced by `GymApp.Tests.Architecture` (`ModuleBoundaryTests`, `ModuleContractProjectTests`).

Contract ownership: the module whose `.Shared` holds an interface owns it; the implementation lives in that module's Infrastructure. Cross-cutting *platform* abstractions that are not owned by any business module (`ICacheService`, `IDateTimeProvider`, `IChatNotifier`, `IUserDataProvider`) stay in `Common.Application/Abstractions`. Each module's intra-module `Application/Contracts/` folder holds **same-module** service interfaces — not part of the shared surface.

## Cross-Module Communication
- **NEVER** reference another module's Domain/Application/Infrastructure directly — only its `.Shared` and `.IntegrationEvents`
- **Synchronous**: Module A injects an interface from Module B's `.Shared` project (DI); impl lives in B's Infrastructure
- **Asynchronous**: Module A publishes an event from its `.IntegrationEvents`; Module B handles it via `INotificationHandler<T>`
- **Async transport (Phase 2)**: Outbox → RabbitMQ → Inbox pattern via MassTransit
- **Current flow**: DomainEvent → SaveChanges intercepts → OutboxMessage table → Quartz job polls → MediatR.Publish → handlers in consuming modules

## Domain Primitives (Bookify — verbatim)
```csharp
// Entity.cs — base with domain events list
public abstract class Entity {
    private readonly List<IDomainEvent> _domainEvents = new();
    protected Entity(Guid id) => Id = id;
    protected Entity() { }
    public Guid Id { get; init; }
    public IReadOnlyList<IDomainEvent> GetDomainEvents() => _domainEvents.ToList();
    public void ClearDomainEvents() => _domainEvents.Clear();
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
// IDomainEvent.cs
public interface IDomainEvent : INotification { }
// Error.cs
public record Error(string Code, string Name) {
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "Null value was provided");
}
// Result.cs + Result<T> — Bookify pattern with IsSuccess, IsFailure, Error, Value
// IUnitOfWork.cs — Task<int> SaveChangesAsync(CancellationToken)
```

## CQRS Abstractions (Bookify — verbatim)
```csharp
public interface ICommand : IRequest<r>, IBaseCommand { }
public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand { }
public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }
public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result> where TCommand : ICommand { }
public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>> where TCommand : ICommand<TResponse> { }
public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>> where TQuery : IQuery<TResponse> { }
```

## Module Registration Pattern (Milan Jovanovic)
```csharp
// Each module: {Module}Module.cs with AddXxxModule(IServiceCollection, IConfiguration)
// Program.cs calls: builder.Services.AddIdentityModule(cfg).AddGymsModule(cfg).AddTrainingModule(cfg)...
// Each module registers its own DbContext with schema: modelBuilder.HasDefaultSchema("identity")
```

## Per-Module DbContext (SQL Server — Pure EF Core)
```csharp
// Each module's DbContext uses UseSqlServer(), has its own schema, implements IUnitOfWork
// SaveChangesAsync overridden to call AddDomainEventsAsOutboxMessages() (Bookify pattern)
// ALL reads (queries) use EF Core with AsNoTracking() for performance — no Dapper, no raw SQL
// Query handlers inject the module's DbContext directly and use LINQ queries
// Example read-side pattern:
//   var results = await _dbContext.Workouts
//       .AsNoTracking()
//       .Where(w => w.TrainerId == trainerId)
//       .Select(w => new WorkoutResponse(w.Id, w.Name, ...))
//       .ToListAsync(ct);
```

| Module | Schema | Tables (key) |
|--------|--------|-------------|
| Identity | `identity` | users, gym_memberships, refresh_tokens, otp_codes |
| Gyms | `gyms` | gyms, operating_hours, classes, class_bookings, check_ins, announcements, staff |
| Training | `training` | exercises, workouts, workout_exercises, programs, program_day_workouts, workout_assignments, workout_logs, log_sets |
| Progress | `progress` | body_metric_entries, progress_photos, streaks |
| Scheduling | `scheduling` | trainer_availability, availability_slots, blocked_dates, sessions, session_trainer_logs, session_member_logs |
| Nutrition | `nutrition` | food_logs, meal_entries, food_items, water_logs, meal_plans, meal_plan_days |
| Communication | `communication` | conversations, messages, notifications, notification_preferences |
| Billing | `billing` | membership_plans, payments, session_packages, package_usage, invoices |
| Gamification | `gamification` | trainer_ratings, trainer_monthly_scores, member_leaderboard_entries, leaderboard_opt_outs, trainer_testimonials |

## Outbox Pattern (Bookify-inspired, pure EF Core)
```csharp
// OutboxMessage: Id, OccurredOnUtc, Type, Content (JSON), ProcessedOnUtc?, Error?
// ProcessOutboxMessagesJob (Quartz, [DisallowConcurrentExecution]):
//   Uses the shared OutboxDbContext (not Dapper) to query and update outbox messages:
//   var messages = await _outboxDbContext.OutboxMessages
//       .Where(m => m.ProcessedOnUtc == null)
//       .OrderBy(m => m.OccurredOnUtc)
//       .Take(_outboxOptions.BatchSize)
//       .ToListAsync(ct);
//   foreach message: Deserialize → MediatR.Publish → set ProcessedOnUtc → SaveChanges
```

## API Conventions
- Base URL: `/api/v1/{resource}` | Auth: JWT Bearer | Multi-tenancy: `X-Gym-Id` header
- Response: `{ "data": {...}, "errors": [...], "meta": {...} }` | Pagination: `?page=1&pageSize=20`
- Error codes: Module-prefixed (`IDENTITY.EMAIL_EXISTS`) | DateTime: UTC ISO 8601 | IDs: GUID

## Auth & Roles
| Role | Scope | Token Claims |
|------|-------|-------------|
| trainee | Gym member | role, gymId |
| trainer | Trainer at gym | role, gymId |
| gym_staff | Receptionist/manager | role, gymId, staffRole, permissions[] |
| gym_owner | Gym owner | role, gymId |
| platform_admin | System admin | role: platform_admin, adminRole, permissions[] |

### Platform Admin Roles (Admin Dashboard RBAC)

As the platform grows, multiple people will access the Admin Dashboard. Each has a different role with different permissions.

| Admin Role | Who | Can Do | Cannot Do |
|-----------|-----|--------|-----------|
| **super_admin** | You (founder) | Everything — full control | Nothing restricted |
| **admin** | Trusted team member | Manage gyms, users, subscriptions, deploy features, view analytics, manage content | Cannot manage other admins, cannot change platform settings, cannot access PDPL breach log |
| **support** | Support team member | View gyms (read-only), view users, view subscriptions, view analytics | Cannot change plans/features, cannot deploy, cannot approve/reject gyms, cannot change subscriptions |
| **analyst** | Data/analytics person | View analytics, view churn data, view reports | Cannot modify anything — read-only access to all data |

```sql
CREATE TABLE identity.platform_admins (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    user_id UNIQUEIDENTIFIER NOT NULL UNIQUE,
    admin_role VARCHAR(20) NOT NULL,  -- 'super_admin', 'admin', 'support', 'analyst'
    created_by UNIQUEIDENTIFIER,      -- which super_admin created this admin
    created_at DATETIMEOFFSET NOT NULL DEFAULT GETUTCDATE()
);
```

**MVP**: Only `super_admin` (you). Other roles are Phase 2 when you hire a team. The infrastructure is built now so adding team members later is a database insert, not a code change.

**Admin API permission checks**: All admin endpoints check `adminRole` from JWT. Endpoints like `POST /admin/deploy/*`, `PUT /admin/plans/*`, `POST /admin/features/*` require `super_admin` or `admin` role. Read-only endpoints (`GET /admin/analytics/*`) allow all admin roles.

**Login response `user.role` values** (used by frontends to gate login): `admin` (platform admin — `adminRole` claim in JWT holds the admin sub-role: `super_admin`/`admin`/`support`/`analyst`), `owner` / `staff` (gym staff — `staffRole` claim holds `owner`/`manager`/`receptionist`), `trainer`, `trainee`. The legacy string `platform_admin` is accepted as an alias of `admin` in frontend auth checks to tolerate either convention on the wire.

**Auth policy `"platform_admin"`**: registered in `Program.cs` as `RequireAuthenticatedUser().RequireClaim("adminRole")`. Any endpoint that calls `.RequireAuthorization("platform_admin")` is reachable by any user whose JWT carries an `adminRole` claim.

### Staff Roles & Permissions

**MVP (Free/Starter/Pro plans)**: 3 fixed roles with preset permissions. No editing.

| Permission | Owner | Manager | Receptionist |
|-----------|-------|---------|-------------|
| Check-in members | ✅ | ✅ | ✅ |
| View member profiles | ✅ | ✅ | ✅ |
| Record payments | ✅ | ✅ | ✅ |
| Add/edit members | ✅ | ✅ | ✅ |
| Manage trainers | ✅ | ✅ | ❌ |
| Create classes | ✅ | ✅ | ❌ |
| Send announcements | ✅ | ✅ | ❌ |
| View analytics/revenue | ✅ | ✅ | ❌ |
| Manage packages | ✅ | ✅ | ❌ |
| Manage staff | ✅ | ✅ | ❌ |
| Change gym settings | ✅ | ✅ | ❌ |
| Manage branding | ✅ | ✅ | ❌ |
| Change billing/subscription settings | ✅ | ❌ | ❌ |

**Business plan** (feature: `custom_permissions`): Editable permission matrix. Gym owner can customize which permissions each role has. Stored in `gyms.staff_role_permissions` table. Overrides the defaults above.

```sql
-- Fixed roles (all plans)
CREATE TABLE gyms.staff_roles (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    gym_id UNIQUEIDENTIFIER NOT NULL,
    user_id UNIQUEIDENTIFIER NOT NULL,
    staff_role VARCHAR(20) NOT NULL,  -- 'owner', 'manager', 'receptionist'
    UNIQUE(gym_id, user_id)
);

-- Custom permission overrides (Business only, feature: custom_permissions)
CREATE TABLE gyms.staff_role_permissions (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    gym_id UNIQUEIDENTIFIER NOT NULL,
    staff_role VARCHAR(20) NOT NULL,
    permission_code VARCHAR(50) NOT NULL,   -- 'check_in', 'record_payments', 'view_analytics', etc.
    is_allowed BIT NOT NULL,
    UNIQUE(gym_id, staff_role, permission_code)
);
-- If no row exists for a role+permission → fall back to the default matrix above
```

```csharp
// Permission check middleware (applied to gym-staff endpoints)
// 1. Get user's staffRole from JWT
// 2. If gym is on Business+ plan AND custom_permissions feature enabled:
//    → check gyms.staff_role_permissions for override
// 3. Otherwise → use default permission matrix (hardcoded)
// 4. Return 403 if not allowed
```

## DATA PROTECTION & COMPLIANCE (Egypt PDPL Law No. 151/2020)

Egypt's Personal Data Protection Law (PDPL) with Executive Regulations (Nov 2025) is now enforceable. GymApp MUST comply as a data controller/processor handling personal data of Egyptian users.

### Compliance Requirements Implemented
- **Explicit consent**: Users must explicitly consent to data collection during registration. Consent text stored with timestamp. Consent withdrawal available in Settings → deletes personal data within 30 days.
- **Purpose limitation**: Data collected only for stated purposes (gym management, fitness tracking). No selling data to third parties. No cross-gym data sharing without consent.
- **Data minimization**: Only collect what's needed. Progress photos optional. Health conditions optional.
- **Right to access**: Users can request export of all their data (Settings → Download My Data → generates JSON/PDF).
- **Right to correction**: Users can edit their profile anytime.
- **Right to deletion**: Users can delete their account (Settings → Delete Account). Soft-delete with 30-day retention, then hard-delete. Gym owner retains anonymized aggregate data (member count, revenue) but personal data purged.
- **Data breach notification**: If breach occurs, notify affected users within 72 hours. Log breach in billing.platform_audit_log.
- **Data retention**: Active accounts: data retained while active. Deleted accounts: 30 days retention then purge. Inactive accounts (no login 24 months): send warning, then archive after 30 days.
- **Encryption**: All personal data encrypted at rest (SQL Server TDE). All data in transit encrypted (HTTPS/TLS 1.3). Passwords hashed with bcrypt (never stored plain).
- **Sensitive data**: Health conditions, body measurements, progress photos classified as sensitive under PDPL. Extra consent required. Stored in encrypted blob storage. Access logged.

### Implementation in Code
```csharp
// In Identity module — consent tracking
public class UserConsent : Entity {
    public Guid UserId { get; private set; }
    public string ConsentType { get; private set; }    // "data_processing", "marketing", "photo_sharing"
    public bool IsGranted { get; private set; }
    public DateTime GrantedAt { get; private set; }
    public DateTime? WithdrawnAt { get; private set; }
    public string ConsentText { get; private set; }     // Exact text user agreed to
    public string IpAddress { get; private set; }
}

// Data export endpoint
GET /api/v1/identity/my-data        → Generates full data export (JSON) for the requesting user
DELETE /api/v1/identity/account      → Soft-delete, 30-day retention, then hard purge
```

## STORAGE LIMITS PER PLAN

Each plan has storage quotas. Managed from Admin Dashboard. Enforced by the backend.

| Resource | Free | Starter | Pro | Business |
|----------|------|---------|-----|----------|
| Members | 25 | 75 | 200 | 500 |
| Trainers | 1 | Unlimited | Unlimited | Unlimited |
| Staff accounts | 1 | 3 | 10 | Unlimited |
| Progress photos (per member) | 5 | 20 | 50 | 200 |
| Exercise videos (gym custom) | 0 | 0 | 0 | 50 (500MB) |
| File storage (total per gym) | 100MB | 500MB | 2GB | 10GB |
| Check-in history retention | 30 days | 6 months | 1 year | 2 years |
| Workout log retention | 30 days | 1 year | 2 years | 5 years |
| Analytics data retention | 7 days | 3 months | 1 year | 2 years |
| API rate limit (requests/min) | 0 | 0 | 0 | 60 |

### Implementation
```csharp
// In IGymPlanService — add storage methods
Task<long> GetStorageLimitBytesAsync(Guid gymId);
Task<long> GetCurrentStorageUsedBytesAsync(Guid gymId);
Task<int> GetPhotoLimitPerMemberAsync(Guid gymId);
Task<int> GetCheckInRetentionDaysAsync(Guid gymId);

// Enforce in UploadProgressPhotoCommandHandler:
var photoLimit = await _gymPlanService.GetPhotoLimitPerMemberAsync(request.GymId);
var currentCount = await _progressRepo.GetPhotoCountAsync(request.MemberId);
if (currentCount >= photoLimit)
    return Result.Failure<Guid>(PlanErrors.StorageLimitReached("progress_photos", photoLimit));
```

### Storage tables
```sql
-- Add to billing schema
CREATE TABLE billing.plan_storage_limits (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    plan_id UNIQUEIDENTIFIER NOT NULL REFERENCES billing.subscription_plans(id),
    resource_type VARCHAR(50) NOT NULL,     -- 'progress_photos', 'file_storage_bytes', 'checkin_retention_days', etc.
    limit_value BIGINT NOT NULL,            -- -1 = unlimited
    UNIQUE(plan_id, resource_type)
);

CREATE TABLE billing.gym_storage_usage (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    gym_id UNIQUEIDENTIFIER NOT NULL,
    resource_type VARCHAR(50) NOT NULL,
    current_usage BIGINT NOT NULL DEFAULT 0,
    last_calculated_at DATETIMEOFFSET NOT NULL DEFAULT GETUTCDATE(),
    UNIQUE(gym_id, resource_type)
);
```

## DATA SAFETY (App Store / Play Store Requirements)

For app store submissions, document data safety practices:

| Data Type | Collected | Shared | Purpose | Encrypted | Deletable |
|-----------|-----------|--------|---------|-----------|-----------|
| Email, phone | Yes | No | Authentication | Yes (TLS + at rest) | Yes (account deletion) |
| Name, photo | Yes | With gym staff only | Profile | Yes | Yes |
| Body weight, measurements | Yes | With assigned trainer only (if member consents) | Fitness tracking | Yes | Yes |
| Progress photos | Yes (optional) | With trainer only (member controls) | Transformation tracking | Yes (encrypted blob) | Yes |
| Location (gym check-in) | No (QR-based, not GPS) | No | — | — | — |
| Payment data | No (recorded by staff, not card details) | No | — | — | — |
| Health conditions | Yes (optional) | With trainer only | Safety | Yes (sensitive data class) | Yes |
| Workout logs | Yes | With trainer only | Training | Yes | Yes |
| Chat messages | Yes | Between conversation participants | Communication | Yes (TLS) | Yes (account deletion) |

## SUBSCRIPTION PLANS & FEATURE GATING (Critical — All Modules Must Enforce)

> **EVERYTHING IS CONFIGURABLE FROM THE ADMIN DASHBOARD.** Plans are NOT hardcoded enums — they are database records created, edited, and deleted from the Admin Dashboard. Features are NOT hardcoded — they are database records. The admin can create new plans, create new features, assign features to plans, override features per gym, and set limits — all without any code changes or deployments.

### Design Principle: Database-Driven, Zero Hardcoding

```
Plans       → billing.subscription_plans table    → CRUD from Admin Dashboard
Features    → billing.features table              → CRUD from Admin Dashboard
Plan↔Feature→ billing.plan_features table          → Toggle matrix from Admin Dashboard
Gym↔Plan    → billing.gym_subscriptions table     → Assigned from Admin Dashboard
Gym↔Override→ billing.gym_feature_overrides table  → Per-gym controls from Admin Dashboard
Audit       → billing.plan_change_log table       → View-only from Admin Dashboard
```

The backend code NEVER checks `if (plan == "Pro")`. It ALWAYS checks `IsFeatureEnabledAsync(gymId, "feature_code")`. This means the admin can create a "Ramadan Special" plan tomorrow with any combination of features — zero code changes needed.

### Initial Seed Data (Created from Admin Dashboard or DB seed)

**Plans** (admin can add/edit/remove anytime):

| Plan Name | Code | Member Limit | Trainer Limit | Staff Limit | Monthly Price (EGP) | Annual Price (EGP) | Sort Order |
|-----------|------|-------------|--------------|-------------|--------------------|--------------------|-----------|
| Free | `free` | 25 | 1 | 1 | 0 | 0 | 1 |
| Starter | `starter` | 75 | -1 (unlimited) | 3 | 499 | 4,990 | 2 |
| Pro | `pro` | 200 | -1 | 10 | 999 | 9,990 | 3 |
| Business | `business` | 500 | -1 | -1 | 1,999 | 19,990 | 4 |

**Features** (admin can add new features anytime):

| Feature Code | Display Name | Description | Category |
|-------------|-------------|-------------|----------|
| `member_management` | Member Management | Member CRUD, search, filter | Core |
| `qr_checkin` | Check-in System | QR code + Member Number check-in with kiosk mode | Core |
| `exercise_library_global` | Exercise Library | Browse 200+ exercises | Core |
| `basic_reporting` | Basic Reports | Member count, check-in count | Core |
| `unlimited_trainers` | Unlimited Trainers | No limit on trainer accounts | Trainers |
| `workout_builder` | Workout Builder | Create workouts, templates, assign | Training |
| `session_logging` | Session Logging | Dual session logging (trainer + member) | Training |
| `progress_tracking` | Progress Tracking | Weight, body fat, measurements | Progress |
| `progress_photos` | Progress Photos | Upload and compare transformation photos | Progress |
| `class_management` | Class Management | Group classes, booking, waitlist | Classes |
| `push_notifications` | Push Notifications | Push notifications to members | Communication |
| `payment_recording` | Payment Recording | Record payments, generate receipts | Billing |
| `session_packages` | Session Packages | PT session packages with tracking | Billing |
| `basic_analytics` | Basic Analytics | Revenue summary, member trends | Analytics |
| `multi_staff` | Multi-Staff | Multiple staff accounts with fixed role permissions | Staff |
| `custom_permissions` | Custom Permissions | Editable permission matrix per staff role (override defaults) | Staff |
| `gym_branding` | Gym Branding | Custom logo and colors on member app | Branding |
| `nutrition_tracking` | Nutrition Tracking | Food log, water tracking | Nutrition |
| `meal_plans` | Meal Plans | Trainer creates and assigns meal plans | Nutrition |
| `member_leaderboards` | Member Leaderboards | Attendance, weight loss, consistency boards | Gamification |
| `trainer_ratings` | Trainer Ratings | Private client feedback on trainers | Gamification |
| `coach_of_the_month` | Coach of the Month | Monthly coach ranking and award | Gamification |
| `advanced_analytics` | Advanced Analytics | Peak hours, retention, churn alerts | Analytics |
| `remote_workout_mode` | Remote Workout Mode | Trainer activates guided workouts for clients | Training |
| `testimonial_moderation` | Testimonials | Anonymized client testimonials | Gamification |
| `multi_week_programs` | Multi-Week Programs | Progressive multi-week training programs | Training |
| `white_label_branding` | White Label | Full white-label (app name, splash, icon) | Branding |
| `api_access` | API Access | REST API for third-party integrations | Integration |
| `custom_exercise_library` | Custom Exercises | Gym adds own exercises with media | Training |
| `pdf_reports` | PDF Reports | Generate branded PDF progress reports | Reporting |
| `churn_prediction` | Churn Prediction | AI-powered churn risk scoring | Analytics |

**Admin can create new features at any time** — e.g., "InBody Integration", "WhatsApp Bot", "Video Coaching", "Supplement Store". The feature appears immediately in the plan toggle matrix. Developers reference the `feature_code` in their code when they build the actual functionality. This allows the admin to pre-configure feature access BEFORE the code is deployed.

### Implementation: Backend Tables (Fully Dynamic)

```sql
-- PLANS — fully dynamic, CRUD from admin dashboard
CREATE TABLE billing.subscription_plans (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    code VARCHAR(50) NOT NULL UNIQUE,              -- 'free', 'starter', 'pro', 'ramadan_special', etc.
    name NVARCHAR(100) NOT NULL,                   -- 'Free', 'Starter', 'Ramadan Special 2026'
    description NVARCHAR(500),
    monthly_price_egp DECIMAL(10,2) NOT NULL DEFAULT 0,
    annual_price_egp DECIMAL(10,2) NOT NULL DEFAULT 0,
    member_limit INT NOT NULL DEFAULT 25,          -- -1 = unlimited
    trainer_limit INT NOT NULL DEFAULT 1,          -- -1 = unlimited
    staff_limit INT NOT NULL DEFAULT 1,            -- -1 = unlimited
    is_active BIT NOT NULL DEFAULT 1,              -- admin can deactivate a plan entirely
    is_public BIT NOT NULL DEFAULT 1,              -- show on pricing page / gym signup? Or hidden/private plan?
    is_time_limited BIT NOT NULL DEFAULT 0,        -- is this a seasonal/promotional plan with an expiry?
    available_from DATETIMEOFFSET,                 -- when this plan becomes visible (NULL = immediately)
    available_until DATETIMEOFFSET,                -- when this plan auto-hides (NULL = forever)
    sort_order INT NOT NULL DEFAULT 0,
    created_at DATETIMEOFFSET NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIMEOFFSET
);
-- Plan visibility logic:
-- A plan is VISIBLE to gyms for signup when ALL of these are true:
--   1. is_active = 1
--   2. is_public = 1
--   3. If is_time_limited = 1: current date is between available_from and available_until
-- A plan is HIDDEN when:
--   - is_public = 0 (admin toggled it off — private/custom deal plan)
--   - is_active = 0 (deactivated entirely)
--   - is_time_limited = 1 AND current date is outside available_from/available_until window
-- IMPORTANT: Gyms already ON a hidden/expired plan keep their subscription.
-- They just can't see it on the signup page. Their features remain active.
-- To move them off, admin must change their plan manually.

-- FEATURES — fully dynamic, CRUD from admin dashboard
CREATE TABLE billing.features (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    code VARCHAR(50) NOT NULL UNIQUE,               -- 'nutrition_tracking', 'inbody_integration', etc.
    name NVARCHAR(100) NOT NULL,                    -- 'Nutrition Tracking'
    description NVARCHAR(500),                      -- 'Members can log meals and track macros'
    category NVARCHAR(50),                          -- 'Core', 'Training', 'Gamification', 'Integration', etc.
    is_active BIT NOT NULL DEFAULT 1,
    sort_order INT NOT NULL DEFAULT 0,
    created_at DATETIMEOFFSET NOT NULL DEFAULT GETUTCDATE()
);

-- PLAN ↔ FEATURE MAPPING — the toggle matrix, managed from admin dashboard
CREATE TABLE billing.plan_features (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    plan_id UNIQUEIDENTIFIER NOT NULL REFERENCES billing.subscription_plans(id),
    feature_id UNIQUEIDENTIFIER NOT NULL REFERENCES billing.features(id),
    is_enabled BIT NOT NULL DEFAULT 0,
    limit_value INT,                                -- e.g., staff_limit = 3 for Starter
    UNIQUE(plan_id, feature_id)
);

-- GYM SUBSCRIPTIONS — which gym is on which plan
CREATE TABLE billing.gym_subscriptions (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    gym_id UNIQUEIDENTIFIER NOT NULL UNIQUE,
    plan_id UNIQUEIDENTIFIER NOT NULL REFERENCES billing.subscription_plans(id),
    started_at DATETIMEOFFSET NOT NULL DEFAULT GETUTCDATE(),
    expires_at DATETIMEOFFSET,
    is_active BIT NOT NULL DEFAULT 1,
    is_trial BIT NOT NULL DEFAULT 0,
    trial_expires_at DATETIMEOFFSET,
    payment_status VARCHAR(20) NOT NULL DEFAULT 'paid',  -- 'paid', 'overdue', 'grace_period', 'suspended'
    last_payment_at DATETIMEOFFSET,
    next_payment_due DATETIMEOFFSET,
    overdue_since DATETIMEOFFSET,                         -- when payment first became overdue
    grace_period_ends_at DATETIMEOFFSET                   -- 7 days after overdue_since + 14 days = 21 days total
);

-- PER-GYM FEATURE OVERRIDES — admin can force ON/OFF any feature for a specific gym
CREATE TABLE billing.gym_feature_overrides (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    gym_id UNIQUEIDENTIFIER NOT NULL,
    feature_id UNIQUEIDENTIFIER NOT NULL REFERENCES billing.features(id),
    override_value BIT NOT NULL,                    -- 1 = Force ON, 0 = Force OFF
    reason NVARCHAR(500),
    set_by UNIQUEIDENTIFIER NOT NULL,
    set_at DATETIMEOFFSET NOT NULL DEFAULT GETUTCDATE(),
    UNIQUE(gym_id, feature_id)
);

-- AUDIT LOG — every change tracked
CREATE TABLE billing.plan_change_log (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    gym_id UNIQUEIDENTIFIER,                        -- NULL for platform-wide changes (plan edits, feature creation)
    change_type VARCHAR(30) NOT NULL,               -- 'plan_created', 'plan_updated', 'plan_deleted', 'feature_created', 'feature_updated', 'gym_plan_changed', 'feature_override_set', 'feature_override_removed', 'limit_override', 'plan_feature_toggled'
    entity_type VARCHAR(20),                        -- 'plan', 'feature', 'gym', 'override'
    entity_id UNIQUEIDENTIFIER,
    old_value NVARCHAR(500),
    new_value NVARCHAR(500),
    reason NVARCHAR(500),
    changed_by UNIQUEIDENTIFIER NOT NULL,
    changed_at DATETIMEOFFSET NOT NULL DEFAULT GETUTCDATE()
);
```

### Feature Resolution Logic (Override > Plan Default)

```csharp
// Resolution order:
// 1. Check gym_feature_overrides for this gym + feature → if exists, use override_value
// 2. Otherwise, check plan_features for the gym's current plan + feature → use is_enabled
// 3. If no record found at all → feature is OFF (fail-closed)
// 4. Cache the merged result in Redis (key: "plan-features:{gymId}", TTL 5 min)
// 5. Invalidate cache when: plan changed, override added/removed, plan features edited, feature created/deleted

public async Task<bool> IsFeatureEnabledAsync(Guid gymId, string featureCode)
{
    var cached = await _cache.GetAsync<GymFeatureSet>($"plan-features:{gymId}");
    if (cached != null) return cached.IsEnabled(featureCode);

    var subscription = await _db.GymSubscriptions
        .Include(s => s.Plan)
        .FirstOrDefaultAsync(s => s.GymId == gymId && s.IsActive);
    if (subscription == null) return false;

    var feature = await _db.Features.FirstOrDefaultAsync(f => f.Code == featureCode && f.IsActive);
    if (feature == null) return false;

    // Check override first
    var featureOverride = await _db.GymFeatureOverrides
        .FirstOrDefaultAsync(o => o.GymId == gymId && o.FeatureId == feature.Id);
    if (featureOverride != null) return featureOverride.OverrideValue;

    // Fall back to plan default
    var planFeature = await _db.PlanFeatures
        .FirstOrDefaultAsync(pf => pf.PlanId == subscription.PlanId && pf.FeatureId == feature.Id);
    return planFeature?.IsEnabled ?? false;
}
```

### Backend Service Interface

```csharp
public interface IGymPlanService
{
    // Feature checks (used by ALL modules)
    Task<bool> IsFeatureEnabledAsync(Guid gymId, string featureCode);
    Task<List<string>> GetEnabledFeaturesAsync(Guid gymId);
    Task<int> GetMemberLimitAsync(Guid gymId);
    Task<int> GetTrainerLimitAsync(Guid gymId);
    Task<int> GetStaffLimitAsync(Guid gymId);
    Task<string> GetPlanCodeAsync(Guid gymId);
}
```

### Admin API Endpoints (Full CRUD — Everything Configurable)

```
-- PLAN CRUD (platform admin only)
GET    /api/v1/admin/plans                                     # List all plans
POST   /api/v1/admin/plans                                     # Create new plan (name, code, price, limits, description, isPublic)
PUT    /api/v1/admin/plans/{planId}                             # Update plan (name, price, limits, description, sort)
DELETE /api/v1/admin/plans/{planId}                             # Soft-delete plan (only if 0 active gyms on it)

-- FEATURE CRUD (platform admin only — create new features from dashboard)
GET    /api/v1/admin/features                                   # List all features with categories
POST   /api/v1/admin/features                                   # Create new feature (code, name, description, category)
PUT    /api/v1/admin/features/{featureId}                       # Update feature (name, description, category)
DELETE /api/v1/admin/features/{featureId}                       # Soft-delete feature (removes from all plans)

-- PLAN ↔ FEATURE MATRIX (platform admin only)
GET    /api/v1/admin/plans/feature-matrix                       # Full matrix: all plans x all features (the toggle grid)
PUT    /api/v1/admin/plans/{planId}/features                    # Bulk update: { features: [{ featureId, isEnabled, limitValue }] }
PUT    /api/v1/admin/plans/{planId}/features/{featureId}        # Toggle single feature for a plan

-- GYM SUBSCRIPTION MANAGEMENT (platform admin only)
GET    /api/v1/admin/gyms/{gymId}/subscription                  # Gym's current plan + resolved features + overrides
PUT    /api/v1/admin/gyms/{gymId}/subscription/plan             # Change gym's plan → { planId, reason }

-- PER-GYM FEATURE OVERRIDES (platform admin only)
GET    /api/v1/admin/gyms/{gymId}/feature-overrides             # List all overrides for this gym
POST   /api/v1/admin/gyms/{gymId}/feature-overrides             # Add override → { featureId, enabled, reason }
DELETE /api/v1/admin/gyms/{gymId}/feature-overrides/{featureId} # Remove override → revert to plan default
PUT    /api/v1/admin/gyms/{gymId}/limits                        # Override member/trainer/staff limits

-- GYM SUSPENSION MANAGEMENT (platform admin only)
POST   /api/v1/admin/gyms/{gymId}/suspend                       # Suspend gym immediately → { reason } → all apps blocked, data preserved
POST   /api/v1/admin/gyms/{gymId}/reactivate                    # Reactivate suspended gym → all apps work immediately
POST   /api/v1/admin/gyms/{gymId}/terminate                     # Permanently terminate → { confirmGymName } → 30-day data retention then purge
GET    /api/v1/admin/gyms/overdue                                # List all gyms with overdue payments (for bulk action)
POST   /api/v1/admin/gyms/bulk-suspend                          # Bulk suspend overdue gyms past grace period → { gymIds[] }

-- AUDIT LOG (platform admin only)
GET    /api/v1/admin/feature-overrides                          # All overrides across all gyms
GET    /api/v1/admin/plan-change-log                            # Full audit log (plan changes, feature changes, overrides)
GET    /api/v1/admin/plan-change-log?gymId={gymId}              # Audit log for a specific gym

-- GYM-FACING ENDPOINT (used by gym apps — NOT admin)
GET    /api/v1/billing/plan-features                            # Returns gym's resolved features (called on app startup, cached)

-- DEPLOY CHANGES (triggers push to affected apps)
POST   /api/v1/admin/deploy/gym/{gymId}                         # Deploy changes to a specific gym's apps
POST   /api/v1/admin/deploy/plan/{planId}                       # Deploy changes to ALL gyms on this plan
POST   /api/v1/admin/deploy/all                                 # Deploy changes to ALL gyms platform-wide
```

### AUTO-PROPAGATION: Admin Dashboard → Deploy → Push → Apps Update Instantly

**Apps do NOT poll. Apps fetch plan features ONCE on login/startup and cache locally. Changes only reach the apps when the admin clicks "Deploy Changes" — which sends a silent push notification to all affected apps, telling them to re-fetch immediately.**

```
ADMIN DASHBOARD (you)
    │
    │  1. Admin makes changes: toggles features, edits plans, creates features, sets overrides
    │     (changes are saved to DB immediately but NOT yet visible to gym apps)
    │
    │  2. Admin clicks "Deploy Changes" button (green button, always visible at top of Plan & Features page)
    │
    ▼
BACKEND API
    │
    │  3. API invalidates Redis cache for affected gyms
    │  4. API sends SILENT PUSH NOTIFICATION via FCM to all affected apps:
    │     - Data payload: { type: "config_refresh", gymId: "xxx", timestamp: "..." }
    │     - Silent push = no visible notification to the user, just a background signal
    │  5. API sends SIGNALR event to all connected Gym Staff Web / Admin Dashboard browsers:
    │     - Event: "config_updated" with affected gymIds
    │  6. API writes to billing.plan_change_log (audit trail)
    │  7. API returns deployment summary → Admin Dashboard shows toast:
    │     "Changes deployed to X gyms (Y apps notified)"
    │
    ▼
MOBILE APPS (Flutter — Trainee, Trainer, Gym Staff)
    │
    │  8. FCM silent push received in background
    │  9. App calls GET /api/v1/billing/plan-features to get fresh config
    │  10. Local cache updated → UI rebuilds:
    │      - Feature was OFF, now ON → locked tab unlocks, upgrade prompt disappears
    │      - Feature was ON, now OFF → tab shows lock icon + upgrade prompt
    │      - Limit changed → new limit enforced immediately
    │  11. If app is in foreground: instant update. If app is closed: update on next open.
    │
    ▼
WEB APPS (Next.js — Gym Staff Web, Admin Dashboard)
    │
    │  12. SignalR event received in real-time (WebSocket)
    │  13. React Query cache invalidated → auto-refetch → UI updates instantly
    │
    ▼
RESULT: Admin configures features → clicks "Deploy" → all affected apps update within seconds.
        No polling. No wasted bandwidth. No 5-minute delays.
```

**When do apps fetch plan features?**
| Trigger | What Happens |
|---------|-------------|
| User opens app (cold start) | Fetch from API, cache locally |
| User logs in | Fetch from API, cache locally |
| Silent push received (FCM) | Re-fetch from API, update cache, rebuild UI |
| SignalR event received (web only) | Invalidate React Query cache, auto-refetch |
| User pulls to refresh (manual) | Re-fetch from API, update cache |
| App resumes from background (optional) | Re-fetch if last fetch was >1 hour ago |

**What triggers deployment**:
| Admin Action | Deploy Scope | Push Target |
|-------------|-------------|-------------|
| Toggle feature ON/OFF for a plan | All gyms on that plan | FCM to all apps of those gyms + SignalR |
| Edit plan (price, limits) | All gyms on that plan | FCM + SignalR |
| Create new plan | No deploy needed (no gyms on it yet) | — |
| Create new feature | No deploy needed (OFF by default) | — |
| Change gym's plan | That specific gym | FCM to that gym's apps + SignalR |
| Add/remove per-gym override | That specific gym | FCM to that gym's apps + SignalR |
| Delete/deactivate feature | All gyms that had it enabled | FCM + SignalR |

**Deploy button behavior**:
- The "Deploy Changes" button shows a badge with the number of pending (unsaved-to-apps) changes since last deploy
- Clicking shows a confirmation: "Deploy to X gyms? This will push config updates to Y active apps."
- After deploy: badge resets to 0, toast shows "Deployed successfully — X gyms updated"
- Admin can also deploy per-gym from the Gym Detail page ("Deploy to this gym" button)
| Delete/deactivate feature | All gyms that had it enabled | All affected apps |

### Implementation: API Endpoint for Frontend

```
GET /api/v1/billing/plan-features
// Returns: { plan: "pro", features: { "workout_builder": true, "nutrition_tracking": true, ... }, limits: { members: 200, trainers: -1, staff: 10 } }
// Frontend caches this on login and refreshes every 5 minutes
// Used to show/hide UI elements and show upgrade prompts
```

### Implementation: Frontend Feature Gate (Flutter + Next.js)

```dart
// Flutter — Riverpod provider
final planFeaturesProvider = FutureProvider<PlanFeatures>((ref) async {
  return ref.read(billingRepositoryProvider).getPlanFeatures();
});

// Usage in any screen:
final features = ref.watch(planFeaturesProvider);
if (features.isEnabled('nutrition_tracking')) {
  // Show nutrition tab
} else {
  // Show upgrade prompt: "Upgrade to Pro to unlock Nutrition Tracking"
}
```

```typescript
// Next.js — React hook
const { data: planFeatures } = useQuery(['plan-features'], fetchPlanFeatures);

// Usage in any component:
{planFeatures?.features.nutrition_tracking
  ? <NutritionTab />
  : <UpgradePrompt feature="Nutrition Tracking" requiredPlan="Pro" />
}
```

### Upgrade Prompt UX Pattern

When a user tries to access a gated feature, show:
- Feature name + short description of what it does
- Which plan unlocks it
- Price of that plan
- "Upgrade Now" CTA → opens upgrade flow
- "Maybe Later" dismiss

Example: "Nutrition Tracking lets your members log meals and track macros. Available on the Pro plan (EGP 999/mo). [Upgrade Now] [Maybe Later]"

### Error Response for Gated Features

```json
{
  "data": null,
  "errors": [
    {
      "code": "PLAN.FEATURE_NOT_AVAILABLE",
      "message": "The 'nutrition_tracking' feature requires the Pro plan or higher.",
      "meta": {
        "feature": "nutrition_tracking",
        "currentPlan": "starter",
        "requiredPlan": "pro",
        "upgradeUrl": "/api/v1/billing/upgrade"
      }
    }
  ]
}
```

### Contract Exposed

```csharp
// In SharedKernel/Contracts/ — available to ALL modules
public interface IGymPlanService
{
    Task<SubscriptionPlan> GetGymPlanAsync(Guid gymId);
    Task<bool> IsFeatureEnabledAsync(Guid gymId, string featureCode);
    Task<int> GetMemberLimitAsync(Guid gymId);
    Task<int> GetTrainerLimitAsync(Guid gymId);
    Task<int> GetStaffLimitAsync(Guid gymId);
    Task<List<string>> GetEnabledFeaturesAsync(Guid gymId);
}
// Implemented in Billing.Infrastructure, cached in Redis
// Every module that needs to gate features injects this interface
```

## Integration Events Catalog

| Event | Published By | Consumed By |
|-------|-------------|-------------|
| `UserRegisteredIntegrationEvent` | Identity | Members, Communication |
| `UserLinkedToGymIntegrationEvent` | Identity | Gyms, Communication |
| `GymApprovedIntegrationEvent` | Gyms | Communication |
| `MemberAddedToGymIntegrationEvent` | Gyms | Communication |
| `TrainerAssignedIntegrationEvent` | Gyms | Scheduling, Communication |
| `AnnouncementPublishedIntegrationEvent` | Gyms | Communication |
| `CheckInRecordedIntegrationEvent` | Gyms | Progress (streak) |
| `WorkoutAssignedIntegrationEvent` | Training | Communication |
| `WorkoutLoggedIntegrationEvent` | Training | Progress (streak), Scheduling |
| `RemoteWorkoutActivatedIntegrationEvent` | Training | Communication (notify trainee: "Your trainer has enabled remote workouts for you") |
| `RemoteWorkoutDeactivatedIntegrationEvent` | Training | Communication (notify trainee: "Remote workout mode has been turned off") |
| `SessionBookedIntegrationEvent` | Scheduling | Communication |
| `SessionCompletedIntegrationEvent` | Scheduling | Training, Progress, Billing |
| `TrainerLoggedSessionIntegrationEvent` | Scheduling | Communication |
| `StreakMilestoneReachedIntegrationEvent` | Progress | Communication |
| `PaymentReceivedIntegrationEvent` | Billing | Gyms |
| `MembershipExpiringIntegrationEvent` | Billing | Communication |
| `SessionPackageLowIntegrationEvent` | Billing | Communication |
| `TrainerRatedIntegrationEvent` | Gamification | Communication (notify gym manager: "New trainer rating submitted") |
| `CoachOfTheMonthSelectedIntegrationEvent` | Gamification | Communication (notify trainer + all gym members: "Coach of the Month announced!") |
| `LeaderboardRecalculatedIntegrationEvent` | Gamification | Communication (notify top 3 members if position changed) |
| `DataImportMemberIntegrationEvent` | Gyms | Identity (creates User + GymMembership with MemberNumber) |
| `DataImportPaymentIntegrationEvent` | Gyms | Billing (creates Payment) |
| `DataImportSessionPackageIntegrationEvent` | Gyms | Billing (creates SessionPackage with reserved template id) |
| `DataImportInBodyIntegrationEvent` | Gyms | Progress (creates BodyMetricEntry, source=InBodyScan) |

## Docker Compose
```yaml
services:
  gymapp.api:          # .NET 8 host → ports 5000/5001
  gymapp-db:           # mcr.microsoft.com/mssql/server:2022-latest → port 1433, sa/GymApp_P@ss2024!
  gymapp-redis:        # redis:7-alpine → port 6379
  gymapp-rabbitmq:     # rabbitmq:3-management-alpine → ports 5672 (AMQP), 15672 (UI, guest/guest)
  gymapp-seq:          # datalust/seq → ports 5341 (ingest), 8081 (UI)
```

## NuGet Packages
MediatR, FluentValidation, Microsoft.EntityFrameworkCore.SqlServer, Quartz, Serilog, Serilog.Sinks.Seq, StackExchange.Redis, Asp.Versioning, Newtonsoft.Json, MassTransit (Phase 2), SonarAnalyzer.CSharp, coverlet.collector

---

# PART 2: ALL BACKEND MODULES

## MODULE 1: Identity (Schema: `identity` | Priority: P0 | Sprint 1-2)

**Aggregate: User** — Id, Email (VO), PhoneNumber (VO), PasswordHash, FirstName, LastName, ProfilePhotoUrl?, DateOfBirth?, Gender (enum), Role (enum: Trainee/Trainer/GymStaff/GymOwner/PlatformAdmin), Status (enum: Active/Suspended/PendingVerification), GymMemberships (VO list: gymId, role, joinedAt, isActive, section? (string — e.g., "Men's Floor", "Ladies Floor", null for single-section gyms), **memberNumber** (string — gym-specific reference number, e.g., "7824". Editable by gym staff. Unique within gym. NOT auto-generated — the gym owner assigns it. When a gym migrates existing members, they bring their current ID numbers. For new members, the system suggests the next sequential number but staff can override it.)), RefreshTokens (entity list: token, expiresAt, createdAt, revokedAt?), Language (enum: Arabic/English), CreatedAt, UpdatedAt.

**Domain Events**: UserRegisteredDomainEvent, UserLoggedInDomainEvent, UserProfileUpdatedDomainEvent, UserSuspendedDomainEvent, UserLinkedToGymDomainEvent.

**Commands**: RegisterUserCommand (email, password, firstName, lastName, phone, role → Result\<TokenResponse\>), LoginCommand (email, password → Result\<TokenResponse\>), RefreshTokenCommand, SendOtpCommand, VerifyOtpCommand, ForgotPasswordCommand, ResetPasswordCommand, UpdateProfileCommand, SocialLoginCommand (Google/Apple idToken), DeleteAccountCommand (soft-delete), LinkToGymCommand (gymCode).

**Queries**: GetProfileQuery, GetUserByIdQuery (internal).

**Domain Rules**: Email unique. Password ≥8 chars + uppercase + number. OTP expires 5min, rate-limit 3/hour. Access token 1hr, refresh 30 days. Social login links if email exists. Suspended users can't login.

**Endpoints**: POST register, POST login, POST refresh-token, POST send-otp, POST verify-otp, POST forgot-password, POST reset-password, PUT profile, POST social-login, DELETE account, POST link-gym, GET profile.

**Contract**: `IIdentityService` — GetUserByIdAsync, GetUsersByIdsAsync, GetUsersByGymIdAsync, UserExistsAsync.

---

## MODULE 2: Gyms (Schema: `gyms` | Priority: P0 | Sprint 2-3)

**Aggregates**:
- **Gym** — Id, OwnerId, Name, Description?, Address (VO: street/city/state/country/zip/lat/lng), ContactInfo (VO: email/phone/website), OperatingHours (DaySchedule list), Branding (VO: logo/primaryColor/secondaryColor/coverPhoto), GymCode (VO: 6-char unique), Status (enum: PendingApproval/Active/GracePeriod/Suspended/Terminated), PaymentStatus (enum: Paid/Overdue/GracePeriod/Suspended), Amenities (string list), SocialLinks (VO), SuspendedAt?, SuspensionReason?, GracePeriodEndsAt?.

**Gym Suspension Flow (Non-Payment)**:
```
Active + Paid → payment due date passes → status stays Active, PaymentStatus = Overdue
    → 7 days overdue → system sends warning email/notification to gym owner
    → 14 days overdue → Status = GracePeriod, PaymentStatus = GracePeriod
        → Gym still works but shows banner: "Your subscription is overdue. Pay within 7 days to avoid suspension."
        → Admin Dashboard shows gym with amber ⚠️ badge
    → 21 days overdue (grace period ends) → Status = Suspended, PaymentStatus = Suspended
        → ALL gym apps show full-screen "Gym Suspended" message — members/trainers/staff cannot use the app
        → Kiosk check-in blocked — shows "This gym's subscription is suspended. Contact gym management."
        → Gym Staff Web/Mobile shows read-only mode — owner can VIEW data but cannot add/edit/delete anything
        → API returns 403 for all write operations with error: "GYM_SUSPENDED"
        → Admin Dashboard shows gym with red 🔴 badge
        → Data is PRESERVED — nothing deleted. Just frozen.
    → Gym pays → Admin clicks "Reactivate" → Status = Active, PaymentStatus = Paid → all apps work immediately
```

**Admin can also manually suspend** any gym for any reason (not just non-payment) — e.g., Terms of Service violation, fraud, etc. Manual suspension is immediate with a required reason field.

**Terminated** = permanent. Admin can terminate a gym (requires typing gym name to confirm). Data retained for 30 days per PDPL, then purged.
- **GymClass** — Id, GymId, Name, Description?, TrainerId, Schedule (VO: day/startTime/duration/recurring), Capacity, Bookings (entity list: memberId/status/bookedAt), WaitlistEnabled, Status.
- **Announcement** — Id, GymId, Title, Content, ImageUrl?, Category (General/Schedule/Offer/Maintenance), TargetAudience, PublishedAt?, ScheduledAt?.

**Domain Events**: GymRegistered, GymApproved, MemberAdded, MemberRemoved, TrainerAdded, ClassCreated, ClassBooked, ClassCancelled, CheckInRecorded, AnnouncementPublished.

**Commands**: RegisterGym, UpdateGym, AddMember, RemoveMember, AddTrainer, CreateClass, BookClass, CancelBooking, RecordCheckIn, CreateAnnouncement, AddStaff, CreateMembershipPlan.

**Queries**: GetGym, GetGymMembers (paginated+filterable), GetGymTrainers, GetGymClasses, GetCheckInHistory, GetAnnouncements, GetGymAnalytics (active members, revenue, new sign-ups, retention rate, peak hours, top classes), GetGymStaff.

**Domain Rules**: GymCode auto-generated 6-char unique. **MemberNumber is gym-assigned** (staff enters it when adding a member, or it comes from data migration. System suggests next sequential number but staff can override. Must be unique within the gym. Editable by gym staff — allows corrections. This is the gym's reference number, NOT our internal ID.). Class waitlist auto-promote. Check-in blocked if membership expired. Max 1 check-in/member/hour. Trainer must be approved before visible.

**Data Migration (Phase 2)**: When a gym migrates from Excel/paper to GymApp, they can bulk-import existing members via CSV upload: name, phone, email, memberNumber, plan, status. The memberNumber column maps directly to the gym's existing ID system so members keep the same number they've always used. This means "7824" at the kiosk works from day one — zero learning curve for members.

**Data Import (MVP, Pro + Business — feature code `data_import`, Q35)**: The Gym Staff web app surfaces a `/data-import` page on Pro + Business plans. Owners upload a single `.xlsx` workbook (exported from Odoo, Daftra or hand-filled from the supplied template) with four tabs: `Members`, `Payments`, `Sessions`, `InBody`. Correlation across tabs is the gym-scoped `MemberNumber`.
- Aggregate: `DataImportJob` (schema `gyms`, table `data_import_jobs`) — `GymId`, `UploadedByUserId`, `Source` (Excel/Odoo/Daftra), `Status` (Pending/Processing/Completed/CompletedWithErrors/Failed), `OriginalFileName`, `StoragePath`, `FileSizeBytes`, row counts, `ErrorReportPath`, `FailureReason`, timestamps. Domain events: `DataImportJobCreated`, `DataImportJobCompleted`, `DataImportJobFailed`.
- Processing: Quartz job `ProcessDataImportJob` parses the workbook via ClosedXML and publishes one integration event per row: `DataImportMember/Payment/SessionPackage/InBodyIntegrationEvent` from `Gyms.IntegrationEvents`. Identity handles Member (creates/links User via `LinkToGym`, uses `MemberNumber`). Billing handles Payment + SessionPackage. Progress handles InBody. Cross-module resolution uses the new `IGymMemberLookup` contract (implemented in Identity.Infrastructure) to map `MemberNumber` → `UserId`.
- Endpoints (all `[Authorize]`, owner/staff): `POST /api/v1/gyms/{gymId}/imports` (multipart, feature-gated on `data_import`, 10 MB max, returns `jobId`), `GET /api/v1/gyms/{gymId}/imports`, `GET /api/v1/gyms/{gymId}/imports/{jobId}`, `GET /api/v1/gyms/{gymId}/imports/{jobId}/errors.csv`, `GET /api/v1/gyms/imports/template.xlsx`.
- Uploaded files stored under `DataImport:StorageRoot` (default: OS temp). Error rows are written to a per-job CSV (`errors/{jobId}.csv`). Imported session packages use reserved `TemplateId = FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFE`.

**Check-In Methods** (3 ways — all produce the same check-in record):
1. **QR Code** — member opens Trainee App → shows QR code → kiosk tablet scans it → check-in logged. Requires member to have the app.
2. **Member Number** — member walks up to kiosk tablet → types their number (e.g., "7824") on a numpad → system looks up member by memberNumber + gymId → shows photo + name + "Welcome!" → check-in logged. Does NOT require the app — works for members who don't have smartphones.
3. **Manual** — staff types member name or number at the desk → confirms → check-in logged.

All 3 methods produce identical check-in records: { memberId, gymId, method: "QR" | "MEMBER_NUMBER" | "MANUAL", timestamp, staffId? (only for manual) }.

**Contract**: `IGymQueryService` — GetGymByIdAsync, GetGymByCodeAsync, IsMemberOfGymAsync, IsTrainerAtGymAsync, IsStaffAtGymAsync, GetGymBrandingAsync, GetGymMemberIdsAsync.

**Endpoints**: POST register-gym, PUT gyms/{id}, POST gyms/{id}/members, DELETE gyms/{id}/members/{memberId}, POST gyms/{id}/trainers, **PUT gyms/{id}/trainers/{trainerId}/approve** (approve pending trainer registration), **PUT gyms/{id}/trainers/{trainerId}/reject** (reject pending trainer), POST gyms/{id}/classes, POST gyms/{id}/classes/{classId}/book, DELETE gyms/{id}/classes/{classId}/bookings/{bookingId}, POST gyms/{id}/check-in (body: { method: "QR"|"MEMBER_NUMBER"|"MANUAL", qrToken?: string, memberNumber?: string, memberId?: string } — resolves member by whichever identifier is provided), POST gyms/{id}/announcements, POST gyms/{id}/staff, GET gyms/{id}/members (paginated, filterable by status/plan/section), GET gyms/{id}/trainers (includes pending), GET gyms/{id}/classes, GET gyms/{id}/check-ins, GET gyms/{id}/announcements, GET gyms/{id}/analytics, GET gyms/{id}/staff, **GET /gyms/pending** (platform admin — lists gyms in `PendingApproval` status awaiting onboarding; returns flat `List<GymResponse>`, ordered by `CreatedAt`), **PUT gyms/{id}/branding** (upload logo + set primaryColor/secondaryColor/coverPhoto — feature gated: `gym_branding`), **GET gyms/{id}/branding** (returns branding object — called by Trainee/Trainer apps on login to apply gym theme), **GET /admin/analytics** (platform admin — KPIs + revenue/growth trends + plan distribution + at-risk gyms for the admin dashboard; `PlatformAnalyticsDto` shape matches `apps/admin/lib/hooks/use-analytics.ts`), **GET /admin/analytics/churn** (platform admin — churn rate, cohort retention, churn-by-plan), **GET /admin/analytics/feature-usage** (platform admin — which features are most/least used across all gyms, for deciding what to include in lower plans).

---

## MODULE 3: Training (Schema: `training` | Priority: P0 | Sprint 3-6)

**Aggregates**:
- **Exercise** — Id, Name, Description?, Instructions?, DemoMediaUrl?, MuscleGroups (enum list), Equipment (enum), Category (Strength/Cardio/Flexibility/Plyometric), Scope (Global/GymSpecific), GymId?, CreatedBy?, IsActive.
- **Workout** — Id, TrainerId, GymId, Name, Description?, WorkoutExercises (ordered entity list: exerciseId, order, sets, reps (string "12"/"8-12"/"AMRAP"), weight?, durationSeconds?, restSeconds?, notes?, groupType (Superset/Circuit)?), EstimatedDurationMinutes?, TargetMuscleGroups, IsTemplate.
- **Program** — Id, TrainerId, GymId, Name, Description?, DurationWeeks (1-52), ProgramWeeks (entity list: weekNumber, theme?, DayWorkouts (VO list: dayOfWeek, workoutId)), IsTemplate.
- **WorkoutLog** — Id, UserId, GymId, WorkoutId?, Date, StartedAt, CompletedAt?, DurationMinutes?, Notes?, LogSets (entity list: exerciseId, setNumber, reps, weight, weightUnit, durationSeconds?, isCompleted, notes?), MuscleGroupsWorked, PerceivedExertion (RPE 1-10).
- **WorkoutAssignment** — Id, TrainerId, ClientId, WorkoutId?/ProgramId?, AssignmentType, StartDate, EndDate?, Status, Notes?, **IsRemoteMode** (bool, default false — when true, the trainee can see and follow the assigned workout in guided mode. When false, assignment is for trainer's internal reference only and the trainee cannot see it).
- **RemoteWorkoutConfig** — Id, TrainerId, ClientId, GymId, IsActive (bool), ActivatedAt, DeactivatedAt?, Reason? (free text — e.g., "Client traveling", "Trainer on leave", "Supplementary home workouts"). When IsActive=true, the trainee's app shows the "Today's Workout" card and guided workout mode for any assigned workouts. Trainer can activate/deactivate this at any time for any client independently.

**Commands**: CreateExercise, CreateWorkout, UpdateWorkout, SaveAsTemplate, AssignWorkout, CreateProgram, AssignProgram, LogWorkout, **ActivateRemoteWorkoutCommand** (trainerId, clientId, reason? → enables guided workout visibility for this client), **DeactivateRemoteWorkoutCommand** (trainerId, clientId → hides guided workout from client).

**Queries**: GetExercises (filter: muscleGroup/equipment/search, paginated, returns global+gym-specific), GetWorkouts, GetWorkoutDetail, GetPrograms, GetProgramDetail, GetWorkoutLogs (date range), GetTodaysWorkout **(returns data ONLY if RemoteWorkoutConfig.IsActive=true for this client, otherwise returns null)**, GetTemplates, **GetRemoteWorkoutStatusQuery** (clientId → returns whether remote mode is active and who activated it).

**Seed**: 200+ exercises pre-populated (Chest/Back/Shoulders/Arms/Legs/Core/Cardio with name, description, instructions, muscle groups, equipment, category).

**Domain Rules**: Exercise library: global visible to all; gym-specific only to that gym. Trainer can only assign workouts to clients at the same gym. Program weeks 1-52. WorkoutLog: one per user per date per workout. Templates sharable among trainers at same gym. **Remote Workout Mode**: default OFF for all clients. Trainer explicitly activates per-client. When OFF, trainee's app hides "Today's Workout" card and guided workout screen — trainee can still log ad-hoc workouts from exercise library. When ON, trainee sees assigned workouts with exercise images/videos and can follow guided mode. Trainer can activate for any reason (leave, client traveling, supplementary home workouts) — not restricted to leave only.

**Contract**: `ITrainingQueryService` — GetWorkoutCountForTrainerAsync, GetWorkoutLogCountForUserAsync, GetRecentWorkoutLogsAsync, GetTodaysAssignmentAsync **(returns null if RemoteWorkoutConfig.IsActive=false)**, **IsRemoteWorkoutActiveAsync(clientId)**.

**Integration Events**: WorkoutAssignedIntegrationEvent, WorkoutLoggedIntegrationEvent, **RemoteWorkoutActivatedIntegrationEvent** (trainerId, clientId, reason), **RemoteWorkoutDeactivatedIntegrationEvent** (trainerId, clientId).

---

## MODULE 4: Progress (Schema: `progress` | Priority: P0-P1 | Sprint 3-5)

**Aggregates**:
- **BodyMetricEntry** — Id, UserId, GymId, Date, Weight?, WeightUnit, BodyFatPercentage?, Measurements (VO: chest/waist/hips/leftArm/rightArm/leftThigh/rightThigh/leftCalf/rightCalf/neck, MeasurementUnit), Source (SelfReported/TrainerMeasured/InBodyScan), LoggedBy, Notes?.
- **ProgressPhoto** — Id, UserId, PhotoUrl, ThumbnailUrl, Angle (Front/Side/Back), Date, IsSharedWithTrainer, Notes?.
- **Streak** — Id, UserId, GymId, CurrentStreak, LongestStreak, StreakType (DailyWorkout/WeeklyWorkout/DailyCheckIn), LastActivityDate.

**Commands**: LogBodyMetric, LogBodyMetricForClient (trainer), UploadProgressPhoto, DeleteProgressPhoto, UpdateStreak (internal, triggered by WorkoutLogged/CheckIn events).

**Queries**: GetBodyMetrics (date range), GetBodyMetricTrends (chart-ready: labels+values per metric), GetProgressPhotos (paginated), GetProgressDashboard (latest metrics, weight trend, body fat change, photo count, streak, workouts this week/month), GetClientProgress (trainer view).

**Domain Rules**: Max 1 entry per user per date (upsert). Photos in blob storage (URL in DB). Photos private by default. Streaks auto-calculated. Milestone events at 7/30/60/90/180/365. Navy body fat calculation as helper.

**Contract**: `IProgressQueryService` — GetLatestMetricsAsync, GetCurrentWeightAsync, GetProgressPhotoCountAsync, GetWorkoutStreakAsync.

---

## MODULE 5: Scheduling (Schema: `scheduling` | Priority: P0 | Sprint 4-6) — **KEY DIFFERENTIATOR**

**Aggregates**:
- **TrainerAvailability** — Id, TrainerId, GymId, WeeklySlots (entity list: dayOfWeek/startTime/endTime/isActive), SessionDurationMinutes (default 60), BufferMinutes (default 15), BlockedDates (entity list: date/reason/autoNotifyClients).
- **Session** — Id, GymId, TrainerId, MemberId, ScheduledDate, ScheduledTime, DurationMinutes, SessionType (PersonalTraining/Assessment/FollowUp), Focus?, Status (Pending/Confirmed/InProgress/Completed/CancelledByTrainer/CancelledByMember/NoShow), IsRecurring, RecurrenceRule?, **TrainerLog** (entity: exercisesPerformed, actualVsPlanned, clientEffortRating 1-5, feedback, techniqueNotes, areasToImprove, nextSessionFocus, attendanceConfirmed, loggedAt), **MemberLog** (entity: rating 1-5, perceivedExertion RPE 1-10, personalNotes, discomfort?, satisfaction 1-5, loggedAt), CancellationReason?, CancelledAt?.

**Commands**: SetAvailability, BlockDate, BookSession, ConfirmSession, CancelSession (late cancel flag if <24h), MarkNoShow, CompleteSession, LogTrainerSession, LogMemberSession, CreateRecurringSessions (up to 12 weeks).

**Queries**: GetAvailability, GetAvailableSlots (calculate: weekly slots - booked sessions - blocked dates + buffer), GetSessions (date range/status/trainerId/memberId), GetSessionDetail (with both logs), GetSessionLogs, GetUpcomingSessions, GetSessionStats (completed/cancelled/no-show counts).

**Domain Rules**: Booking validates against availability. Late cancel = <24h. Trainer/member logs are independent. Status flow: Pending→Confirmed→InProgress→Completed (or Cancel/NoShow anytime). Available slots = weekly slots minus booked minus blocked with buffer.

**Contract**: `ISchedulingQueryService` — GetCompletedSessionCountAsync, GetNoShowCountAsync, GetNextSessionAsync, GetTrainerSessionsForMonthAsync.

---

## MODULE 6: Nutrition (Schema: `nutrition` | Priority: P1-P2 | Sprint 6-7)

**Aggregates**: FoodLog (UserId, Date, Meals list with FoodItems: name/qty/unit/calories/protein/carbs/fat, DailyTotals), WaterLog (UserId, Date, entries list with amountMl, totalMl, targetMl), MealPlan (TrainerId, ClientId?, GymId, Name, DailyCalorieTarget, MacroTargets, Days list with PlannedMeals + Alternatives, IsTemplate).

**Endpoints**: POST/GET food-log, GET food-log/summary, POST/GET water-log, PUT water-log/target, POST/GET/PUT meal-plans, POST meal-plans/{id}/assign, GET clients/{id}/food-log.

**Rules**: One FoodLog per user per date (upsert). Water target default 2000ml. Basic Egyptian food database seeded (50+ items: ful, koshari, grilled chicken, rice, etc.).

---

## MODULE 7: Communication (Schema: `communication` | Priority: P1 | Sprint 5-7)

**Aggregates**: Conversation (GymId, Participants (2 for 1-on-1), Type (Direct/Broadcast), Messages entity list: senderId/content/mediaUrl?/mediaType?/isRead/readAt?/sentAt), Notification (UserId, Title, Body, Type enum (SessionReminder/TrainerFeedback/Announcement/MembershipExpiry/Achievement/WorkoutAssigned/Message), Data dict, IsRead, IsPushSent), NotificationPreference (per-type toggles + quiet hours).

**Endpoints**: GET/POST conversations + messages, POST broadcast, WS real-time (SignalR), GET notifications, PUT read/read-all, GET unread-count, PUT notification-preferences.

**Implementation**: SignalR for real-time. FCM for push (Android+iOS). Domain events → NotificationHandler → creates Notification + sends FCM. Conversations auto-created on trainer assignment. Broadcast = individual messages per recipient. Media → blob storage.

---

## MODULE 8: Billing (Schema: `billing` | Priority: P0-P1 | Sprint 3-6)

**Aggregates**: MembershipPlan (GymId, Name, DurationDays, Price (Money VO: amount+currency), Features, IsActive, SortOrder), Payment (GymId, MemberId, Amount, PaymentMethod (Cash/Card/Online/BankTransfer), Purpose (Membership/SessionPackage/Product), ReferenceId?, Status (Completed/Pending/Failed/Refunded), ReceiptNumber auto-gen, RecordedBy, PaidAt), SessionPackageTemplate (GymId, Name, SessionCount, PricePerSession, TotalPrice, IsActive — created by gym owner/manager, defines available packages), SessionPackage (GymId, TrainerId, MemberId, TemplateId, Name, TotalSessions, RemainingSessions, PricePerSession, TotalPrice, Status (Active/Exhausted/Expired), ExpiresAt?, UsageHistory list, AssignedBy — assigned by gym staff at point of sale).

**Endpoints**: GET/POST billing/plans, GET/POST billing/payments, GET/POST/PUT billing/package-templates (gym_owner/gym_staff only — CRUD package templates), POST billing/packages/assign (gym_staff only — assigns a template to member+trainer), GET billing/packages (list packages, filterable by trainer/member/status), GET billing/packages/my-clients (trainer only — view-only list of their clients' packages), PATCH packages/{id}/use (internal — triggered by SessionCompleted event), GET revenue (date range), GET revenue/by-trainer, POST/GET invoices, GET members/{id}/balance.

**Package Flow (WHO DOES WHAT)**:
1. Gym Owner/Manager → creates package TEMPLATES (e.g., "10 Sessions - EGP 5,000", "20 Sessions - EGP 8,000") via Gym Staff Web or Mobile
2. Gym Staff (receptionist) → when member pays at the desk, assigns a template to the member + links to a specific trainer
3. Trainer → ONLY views their clients' packages (remaining sessions, expiry). CANNOT create, edit pricing, or delete packages
4. System → auto-decrements on SessionCompleted event, alerts at ≤3 and =1 remaining

**Domain Events**: PaymentReceived, MembershipExpiring (7 days before), MembershipExpired, SessionPackageLow (≤3), SessionPackageExhausted (=0).

**Rules**: Payment recorded (not processed online in MVP — future Stripe/Fawry). Package auto-decrement on SessionCompleted event. Receipt: `REC-{GymCode}-{YYYYMMDD}-{SEQ}`. Only Completed payments count in revenue. Package templates created by gym_owner/gym_staff ONLY — trainers have view-only access to their clients' packages. Trainers CANNOT create, edit pricing, or delete packages.

---

## MODULE 9: Gamification & Ratings (Schema: `gamification` | Priority: P2 | Sprint 7-9)

**Aggregates**:

- **MemberLeaderboard** — GymId, Period (Month/Year), LeaderboardType (enum: Attendance, WeightLoss, Consistency), Entries (entity list: memberId, rank, score, delta?, isOptedIn). Recalculated at end of each period (daily snapshot for live updates). Members who opted out (`ShowOnLeaderboard=false` in their profile settings) are excluded.

- **TrainerRating** — Id, GymId, TrainerId, ClientId, SessionId, Stars (1-5), Comment?, CreatedAt. **Visibility: gym owner and managers ONLY** — trainers do NOT see individual ratings or who gave them. Members do NOT see other members' ratings. This is a management tool.

- **TrainerMonthlyScore** — Id, GymId, TrainerId, Month, Year, AggregatedScore (decimal), TotalRatings (int), UniqueClients (int), SessionCount (int), NormalizedScore (decimal — weighted formula: `(avgStars × 0.6) + (consistencyBonus × 0.2) + (clientRetentionRate × 0.2)`, adjusted so trainers with few clients aren't unfairly compared to trainers with many). IsCoachOfTheMonth (bool).

- **TrainerPublicProfile** — TrainerId, GymId, DisplayRating (decimal, e.g., 4.7 — aggregated, visible to members browsing trainers), AnonymizedTestimonials (list: comment text with no client name, curated/moderated by gym manager), CoachOfTheMonthCount (int).

- **LeaderboardOptOut** — UserId, GymId, ShowOnLeaderboard (bool, default true). Member can toggle this in their privacy settings.

**Domain Events**: LeaderboardRecalculatedDomainEvent, CoachOfTheMonthSelectedDomainEvent, TrainerRatedDomainEvent.

**Commands**: SubmitTrainerRatingCommand (clientId, trainerId, sessionId, stars, comment? → rating saved, visible to gym management only), RecalculateLeaderboardsCommand (triggered by scheduled job — end of day/month), CalculateMonthlyTrainerScoresCommand (triggered at month-end), ToggleLeaderboardOptOutCommand (memberId, showOnLeaderboard bool), ModerateTestimonialCommand (gym manager approves/rejects anonymized comment for trainer's public profile).

**Queries**:
- GetMemberLeaderboardQuery (gymId, type: Attendance/WeightLoss/Consistency, period → returns ranked list with member name, score, rank, avatar. Members who opted out excluded.)
- GetMyLeaderboardPositionQuery (memberId, gymId → returns the member's rank in each leaderboard type even if they opted out, but their name is hidden from others)
- GetTrainerRatingsQuery (gymId, trainerId, dateRange → **gym manager only** — returns all individual ratings with comments, NOT visible to trainer or members)
- GetTrainerMonthlyScoresQuery (gymId, month/year → returns all trainers ranked by normalized score)
- GetCoachOfTheMonthQuery (gymId, month/year → returns winning trainer + score + anonymized testimonials)
- GetTrainerPublicProfileQuery (trainerId → returns display rating + anonymized testimonials + coach-of-month badges — visible to members)
- GetCoachLeaderboardQuery (gymId → all trainers ranked by current period score, visible to gym staff/owner)

**Endpoints**:
```
POST   /api/v1/gamification/trainer-ratings              # Member submits rating after session
GET    /api/v1/gamification/leaderboards/{type}          # Member views leaderboard (attendance/weightloss/consistency)
GET    /api/v1/gamification/leaderboards/my-position     # Member sees own rank
PUT    /api/v1/gamification/leaderboards/opt-out          # Member toggles visibility
GET    /api/v1/gamification/trainers/{id}/public-profile  # Members see trainer rating + testimonials
GET    /api/v1/gamification/trainers/leaderboard          # Gym staff sees coach ranking
GET    /api/v1/gamification/trainers/coach-of-the-month   # Current/past coach of the month
GET    /api/v1/gamification/trainers/{id}/ratings          # Gym manager ONLY — individual ratings
POST   /api/v1/gamification/trainers/{id}/testimonials     # Gym manager moderates testimonials
GET    /api/v1/gamification/trainer-monthly-scores         # Gym manager — all trainers monthly scores
```

**Leaderboard Scoring Logic**:
- **Attendance**: Score = total sessions attended this month. Ties broken by streak length.
- **Weight Loss**: Score = percentage of body weight lost this month (e.g., lost 2kg from 80kg = 2.5%). Only members who logged weight at start and end of month qualify. Prevents gaming by using % not absolute kg.
- **Consistency**: Score = (actual sessions completed / target sessions assigned) × 100. A member who completed 12 out of 12 assigned sessions scores 100%. A member with 10/12 scores 83%. Only members with assigned sessions qualify.

**Trainer Normalized Score Formula**: Prevents unfair comparison between trainers with different client counts.
```
NormalizedScore = (AvgStars × 0.6) + (ConsistencyBonus × 0.2) + (ClientRetentionRate × 0.2)

Where:
- AvgStars: average of all client ratings this month (1.0-5.0 scale)
- ConsistencyBonus: (sessions completed on time / sessions scheduled) × 5.0
- ClientRetentionRate: (clients who renewed or continued / total clients) × 5.0

All components scaled to 0-5, final score is 0-5.
Minimum 3 unique client ratings required to qualify for Coach of the Month.
```

**Visibility Matrix**:
| Data | Member | Trainer | Gym Manager/Owner |
|------|--------|---------|-------------------|
| Member Leaderboards (attendance, weight loss, consistency) | ✅ sees all (opted-in members only) | ✅ sees all | ✅ sees all |
| Own leaderboard position | ✅ always (even if opted out) | — | ✅ |
| Individual trainer ratings (stars + comments) | ❌ | ❌ | ✅ only |
| Trainer aggregated star rating (e.g., "4.7★") | ✅ on trainer profile | ✅ own rating | ✅ |
| Anonymized testimonials | ✅ on trainer profile | ✅ own profile | ✅ + moderate |
| Coach of the Month | ✅ | ✅ | ✅ |
| Coach Leaderboard (ranked scores) | ❌ | ❌ | ✅ only |
| Monthly detailed score breakdown | ❌ | ❌ | ✅ only |

**Contract**: `IGamificationQueryService` — GetTrainerDisplayRatingAsync, GetMemberLeaderboardPositionAsync, IsCoachOfTheMonthAsync.

---

# PART 3: TESTING STRATEGY

## Test Types (5 types, all from Bookify EF source, adapted for SQL Server)

### 1. Domain Unit Tests
- **Pattern**: xUnit + FluentAssertions. Zero mocks. Pure domain logic.
- **BaseTest helper**: `AssertDomainEventWasPublished<T>(Entity entity)` — asserts exactly one domain event of type T was raised.
- **Test Data**: Static `*Data` factory classes (e.g., `UserData.Create()`, `SessionData.Create(trainerId, memberId, gymId)`).
- **What to test**: Entity creation sets properties. Factory methods raise correct domain events. State transitions enforce rules (e.g., Confirm on Cancelled → Failure). Domain services calculate correctly. Value object validation.
- **Packages**: xUnit 2.6.6, FluentAssertions 6.12.0, coverlet.collector 6.0.0
- **Coverage**: 90%+

### 2. Application Unit Tests
- **Pattern**: xUnit + FluentAssertions + **NSubstitute** 5.1.0. Mock all repositories + IUnitOfWork + IDateTimeProvider.
- **What to test per handler**: Null entity → Failure. Invalid state → Failure. UoW exception → correct error. Success → Result.IsSuccess. Success → Repository.Add called via `Received(1)`.
- **Coverage**: 80%+

### 3. Integration Tests
- **Pattern**: `WebApplicationFactory<Program>` + **Testcontainers.MsSql** (SQL Server 2022) + **Testcontainers.Redis**.
- **IntegrationTestWebAppFactory**: Implements `IAsyncLifetime`. Starts MsSql + Redis containers. Replaces ALL 8 module DbContexts with `UseSqlServer(container.GetConnectionString())`.
- **BaseIntegrationTest**: `IClassFixture<IntegrationTestWebAppFactory>`. Gets `ISender` from scoped service provider. Sends real commands/queries through full MediatR pipeline with real SQL Server.
- **What to test**: Full use-case flows: register user, create workout, book session, log metrics. Tests hit real DB, real validation, real domain events.
- **Packages**: Microsoft.AspNetCore.Mvc.Testing 8.0.1, Testcontainers.MsSql 3.7.0, Testcontainers.Redis 3.7.0

### 4. Functional Tests (API / HTTP)
- **Pattern**: Same `WebApplicationFactory` + Testcontainers. Tests use `HttpClient` to call actual HTTP endpoints.
- **FunctionalTestWebAppFactory**: Same as integration + seeds test user on startup via `PostAsJsonAsync("api/v1/identity/register", testUser)`.
- **BaseFunctionalTest**: Has `GetAccessToken()` helper (registers+logs in → returns JWT). Has `AuthenticateAsync()` that sets `Authorization` header.
- **What to test**: Every endpoint: 401 without token, 400 with invalid data (`[Theory]`+`[InlineData]` for edge cases), 200/201 with valid data. Tests real HTTP serialization, middleware, auth.
- **Packages**: Same as integration tests

### 5. Architecture Tests
- **Pattern**: **NetArchTest.Rules** 1.3.2. Tests run against assemblies via reflection.
- **Domain rules**: Domain events must be `sealed` and end with `DomainEvent`. Entities must have private parameterless constructor.
- **Application rules**: CommandHandlers end with `CommandHandler` and are `internal`. QueryHandlers end with `QueryHandler` and are `internal`. Validators end with `Validator` and are `internal`.
- **Layer rules (Bookify verbatim)**: Domain → no dependency on Application/Infrastructure/Presentation. Application → no dependency on Infrastructure/Presentation. Infrastructure → no dependency on Presentation.
- **Module boundary rules (Milan)**: Each module's Domain/Application/Infrastructure must NOT reference any other module's Domain/Application/Infrastructure. Only `.Shared` (sync contracts) and `.IntegrationEvents` (async events) cross-references allowed.

## CI/CD Pipeline
Unit tests + Architecture tests → run on every push (fast, no containers). Integration tests + Functional tests → run after unit tests pass (need Docker for Testcontainers).

---

# PART 4: AGENT ROUTING

| Agent | What to Build | Feed This Document |
|-------|-------------|-------------------|
| **Backend-Identity** | Identity module (all 4 layers + IntegrationEvents + tests) | This doc, focus PART 2 Module 1 |
| **Backend-Gyms** | Gyms module | This doc, focus PART 2 Module 2 |
| **Backend-Training** | Training module + 200 exercise seeds | This doc, focus PART 2 Module 3 |
| **Backend-Progress** | Progress module | This doc, focus PART 2 Module 4 |
| **Backend-Scheduling** | Scheduling module (dual-logging) | This doc, focus PART 2 Module 5 |
| **Backend-Support** | Nutrition + Communication + Billing modules | This doc, focus PART 2 Modules 6-8 |
| **Backend-Shared** | Common.Domain + Common.Application + Common.Infrastructure + Common.Presentation | This doc, PART 1 Architecture |
| **Backend-Tests** | All 5 test types + CI/CD | This doc, PART 3 Testing |

**Execution order**: Shared/Common → Identity → all other modules in parallel → tests.
