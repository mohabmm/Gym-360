# 07 — Multi-Tenancy: Schema-per-Gym Hard Isolation

> **Authority:** This file describes how tenancy works in the GymApp platform after the 2026-05-01 migration. It supersedes prior multi-tenancy notes in `01_GYMAPP_BACKEND_COMPLETE.md` and `02_GYMAPP_FRONTEND_COMPLETE.md`. Conflicts resolve in favor of this file.

## 1. Model Overview

The platform uses **schema-per-Gym hard isolation** on a single SQL Server database:

- **One Gym Owner** (tenant root) can run **one or more Gyms**.
- **Each Gym is fully isolated** — its own tables (members, staff, trainers, classes, sessions, billing, outbox) live under its own per-module schemas, prefixed `g{N}_`.
- **Trainees, trainers, and staff belong to exactly one Gym** (`FOUNDER_DECISIONS.md` Q3 — revised 2026-05-01).
- **Owners scope across many gyms** via `User.OwnerId`, not via `GymMembership`.
- **Cross-tenant data** (Users, Owners, Contracts, Gym registry) lives in fixed global schemas: `identity`, `gyms_registry`.

### Why schema-per-Gym
- True data isolation at the DB level (no `GymId` filter bugs leak across tenants).
- Clean PDPL data deletion: `DROP SCHEMA g{N}_*` purges a gym entirely.
- Per-gym backups and restores via T-SQL filtering on schema name.
- Owner aggregation possible via `UNION ALL` across schemas inside the same database (cheaper than cross-DB).

### Trade-offs
- **Model cache memory** — EF compiles one model per `(DbContextType, SchemaPrefix)`. ~9 contexts × N gyms. Acceptable up to a few hundred gyms; revisit with telemetry beyond that.
- **Migration ceremony** — each new gym runs all 9 module migrations against its own schemas (fast in practice; `EnsureCreated` + `Migrate` per provisioning).
- **Cross-schema joins** allowed only inside the same gym (same `g{N}` prefix). Cross-gym aggregation routed through `TenantManagement` aggregator service.

## 2. Schemas

### Global (single instance, never per-gym)
| Schema | Owner | Contents |
|---|---|---|
| `identity` | Identity module | Users, RefreshTokens, OtpCodes, PlatformAdmins, PlatformSettings, UserConsents, **Owners**, **Contracts** |
| `gyms_registry` | `Common.Infrastructure.Tenancy.Registry` | Lightweight gym registry: `GymId`, `OwnerId`, `Name`, `Slug`, `SchemaPrefix`, `SequentialNumber`, `Status` |

### Per-Gym (created on provisioning, prefix `g{N}_`)
| Module | Schema (example for gym 1) |
|---|---|
| Gyms | `g1_gyms` |
| Training | `g1_training` |
| Progress | `g1_progress` |
| Scheduling | `g1_scheduling` |
| Nutrition | `g1_nutrition` |
| Communication | `g1_communication` |
| Billing | `g1_billing` |
| Gamification | `g1_gamification` |
| Outbox | `g1_outbox` |

`SchemaPrefix` is validated against `^g\d+$` at every insertion to prevent dynamic-SQL injection on schema names.

## 3. Tenant Resolution Flow

### Request lifecycle
1. **Login** (`POST /api/v1/identity/login`) issues a JWT.
   - **Owner principal** (`User.OwnerId.HasValue` and `Role == GymOwner`): JWT carries no `gymId`. Response includes `OwnedGyms[]`. Client must call select-gym next.
   - **Other principals** (Trainer/Trainee/Staff/PlatformAdmin): JWT carries `gymId` and `schemaPrefix` resolved from the user's single active `GymMembership`.

2. **Owner select** (`POST /api/v1/identity/select-gym`): Owner picks a gym from `OwnedGyms[]`. New JWT issued with `gymId` and `schemaPrefix`.

3. **Per request**, `TenantResolutionMiddleware` (in `GymApp.Api/Middleware/`) reads the JWT claims and populates the scoped `ITenantContextAccessor`:
   - `GymId` (Guid)
   - `SchemaPrefix` (e.g. `g1`)
   - `IsOwnerScope` (bool)

4. **Each per-gym `DbContext`** injects `ITenantContext` and resolves its schema in `OnModelCreating`:
   ```csharp
   var schema = _tenantContext.SchemaFor("training"); // → "g1_training" or "training" in Soft mode
   modelBuilder.HasDefaultSchema(schema);
   ```

5. **Model cache** is keyed by `(DbContextType, SchemaPrefix)` via `TenantModelCacheKeyFactory` so the per-tenant `HasDefaultSchema` is honored after the first build.

### JWT claims
| Claim | Set when | Purpose |
|---|---|---|
| `sub` | always | userId |
| `email` | always | display |
| `role` | always | trainer/trainee/staff/owner/admin |
| `principalType` | always | Owner / Trainer / Trainee / Staff / PlatformAdmin |
| `ownerId` | when User has Owner aggregate | Cross-gym scope key |
| `gymId` | non-Owner OR Owner-after-select-gym | Active tenant id |
| `schemaPrefix` | when `gymId` present and Tenancy:Mode=Hard | Per-gym schema name (`g{N}`) |
| `staffRole` | gym staff/owner | Authorization policies |
| `adminRole` | platform admin | Authorization policies |

## 4. Provisioning (New Gym)

`IGymProvisioningService.ProvisionAsync(gymId, ownerId, name, slug)` does:

1. Allocate next `SequentialNumber` and derive `SchemaPrefix = $"g{N}"`.
2. `SchemaCreator.CreateSchemasAsync` runs raw `CREATE SCHEMA [{prefix}_{module}]` for all 9 module schemas (EF does not auto-create schemas).
3. Insert row into `gyms_registry.gyms`.
4. On next request scoped to that gym, EF migrates module tables into the new schemas (auto-migrate in Development; explicit `Migrate()` in Production).

Endpoint: `POST /api/v1/owners/{ownerId}/gyms` (Owner/Admin only). Body: `{ "gymId", "ownerId", "name", "slug" }`.

PDPL deprovisioning: `IGymProvisioningService.DeprovisionAsync(gymId)` drops every table inside each `g{N}_*` schema and then the schemas themselves, then removes the registry row.

## 5. Outbox Processing

- **Per-gym outbox**: each gym's domain events accumulate in `g{N}_outbox.outbox_messages` (managed by per-gym `OutboxDbContext`).
- **Quartz job** `ProcessOutboxMessagesJob` runs every 10s:
  - **Hard mode**: queries `gyms_registry`, iterates active gyms, sets `ITenantContextAccessor` per gym, drains that gym's outbox.
  - **Soft mode**: drains the central legacy outbox once per tick.
- Events are published via MediatR after deserialization (`Newtonsoft.Json` + `TypeNameHandling.All`).
- Concurrency: gyms processed sequentially per tick; raise `SemaphoreSlim` parallelism when N > ~50.

## 6. Owner Unified Dashboard

For owners on `Contract.DashboardMode ∈ {Unified, Both}`, the admin web aggregates across owned gyms:

- `OwnerDashboardQuery` (planned, Phase 4 follow-up) builds raw SQL with `UNION ALL` across `g{N}_billing.payments`, `g{N}_gyms.gym_members`, `g{N}_scheduling.sessions`, dynamically substituting validated `SchemaPrefix` values from `gyms_registry`.
- Cached in `IDistributedCache` keyed `owner-dashboard:{ownerId}`, TTL 5 min, invalidated on `PaymentReceivedIntegrationEvent`.
- Schema-prefix injection prevention: regex `^g\d+$` validation enforced at registry insert.

## 7. Migration Playbook (Soft → Hard)

Safe rollout (one DB instance per environment):

1. **Code rollout** — deploy build with `Tenancy:Mode = "Soft"` (default). All DbContexts use the legacy `module` schema names; nothing changes at runtime.
2. **Maintenance window**: stop API, run `db/migrations/2026_05_01_hard_isolation_bootstrap.sql`. The script:
   - Creates `gyms_registry` schema and Owner/Contract rows for the existing demo owner.
   - Stamps the existing demo gym as `g1` and `ALTER SCHEMA g1_{module} TRANSFER` every per-module table.
   - Writes `dbo.__tenancy_version (Version=1)`.
3. **Switch config** to `Tenancy:Mode = "Hard"` and restart. The startup gate refuses to boot if `__tenancy_version < 1`.
4. **Validate** with smoke checks (cross-tenant isolation tests, MobAI sweeps).
5. **Rollback (if needed)**: stop API → run `db/migrations/2026_05_01_rollback.sql` → revert config to `Soft` → restart.

## 8. Test Isolation Strategy

- **Cross-tenant isolation tests** (`tests/GymApp.IntegrationTests/Tenancy/`):
  - Trainer of Gym 1 logs in → spoofs `X-Gym-Id` to Gym 2 → expects 403 (JWT `gymId` claim wins; header is defense-in-depth).
  - Provision `g{N+1}` → write data → query Gym 1 → assert no leakage.
  - Outbox: emit event in Gym 1 context → message lands only in `g1_outbox.outbox_messages`.
- **Test fixtures** use `WebApplicationFactory<Program>` with `Tenancy:Mode=Hard` and a `TestTenantContext` that pins to `g_test`.
- **MobAI / Playwright** baselines re-captured after the bootstrap; no selector changes, only data reseed.

## 9. Open Items (non-blocking)

- Owner unified dashboard aggregator service (Phase 4) — sketched but not yet implemented.
- Admin web `GymSwitcher.tsx` + `/select-gym` chooser page — UI path pending.
- Cross-gym query telemetry (model cache size, per-gym migration latency).
- Decision: when N gyms grows past ~500, evaluate splitting hot gyms into dedicated SQL Server databases (DB-per-Gym escape hatch). Current architecture supports this via per-gym connection-string override on `IGymProvisioningService` (not yet implemented).

## 10. Related Files

| File | Purpose |
|---|---|
| `src/Common/GymApp.Common.Application/Tenancy/ITenantContext.cs` | Scope abstraction |
| `src/Common/GymApp.Common.Infrastructure/Tenancy/TenantContextAccessor.cs` | Scoped impl |
| `src/Common/GymApp.Common.Infrastructure/Tenancy/TenantModelCacheKeyFactory.cs` | EF model cache key |
| `src/Common/GymApp.Common.Infrastructure/Tenancy/TenancyServiceCollectionExtensions.cs` | DI registration |
| `src/Common/GymApp.Common.Infrastructure/Tenancy/Registry/TenantRegistryDbContext.cs` | gyms_registry schema |
| `src/Common/GymApp.Common.Infrastructure/Tenancy/Provisioning/SchemaCreator.cs` | Raw CREATE SCHEMA / DROP SCHEMA |
| `src/Common/GymApp.Common.Infrastructure/Tenancy/Provisioning/GymProvisioningService.cs` | Orchestrator |
| `src/API/GymApp.Api/Middleware/TenantResolutionMiddleware.cs` | Reads JWT → populates scope |
| `src/API/GymApp.Api/Endpoints/OwnerGymsEndpoint.cs` | POST /api/v1/owners/{ownerId}/gyms |
| `src/Modules/Identity/.../Commands/SelectGym/SelectGymCommandHandler.cs` | Owner gym chooser |
| `db/migrations/2026_05_01_hard_isolation_bootstrap.sql` | Soft → Hard migration |
| `db/migrations/2026_05_01_rollback.sql` | Hard → Soft rollback |
