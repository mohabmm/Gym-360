# Modular Monolith Review and Microservices Readiness

_Date: 2026-10-16_

## 1. Database layout: one database per module?

No. All modules share **one SQL Server database**. Every module registers its DbContext against the same `"Database"` connection string (for example `src/Modules/Training/GymApp.Modules.Training.Infrastructure/TrainingModuleRegistration.cs`, with the same pattern in the other 8 modules).

Modules are separated by **DbContext and schema**, not by database:

- **One DbContext per module.** Each module owns its own context, migrations and tables.
- **Per-gym schemas for tenant data.** The schema name is `g{N}_{module}`, for example `g1_training` or `g3_billing`. The prefix comes from `ITenantContext` at runtime.
- **Fixed schemas for global data.** `identity` holds users, owners and contracts. `gyms_registry` holds the gym registry.
- **Cross-tenant reads go through one service.** Only the `TenantManagement` aggregator (`OwnerDashboardService`) queries across gyms, for example for the owner dashboard.

This gives logical isolation inside one physical database. Because modules never reference each other's tables, a module could later move to its own database by giving it its own connection string. See `docs/07_MULTITENANCY.md`.

## 2. Is this a good modular monolith?

Yes. It is better than most. Extracting modules into microservices later is realistic, one module at a time, but it is not free: four areas need rework first (section 3).

### What is done well

- **Boundaries are enforced in code.** No module's project references another module's Domain, Application or Infrastructure. The only cross-module links go through `.Shared` (synchronous contracts) and `.IntegrationEvents` (asynchronous events). `tests/GymApp.Tests.Architecture/ModuleBoundaryTests.cs` guards this.
- **Each module owns its data.** There is one DbContext per module with its own schema and migrations. There is no `BeginTransaction` or `TransactionScope` spanning modules.
- **Modules mostly talk through events.** Events are saved to an outbox table, and 34 integration event handlers consume them. A consumer-tracking table (`outbox_message_consumers`) keeps a handler from re-running on retry.
- **The synchronous contracts are small.** There are 10 read-mostly lookup interfaces: `IGymLookupService`, `IGymMemberLookup`, `IGymMembershipChecker`, `IGymPlanService`, `IStaffSubRoleResolver`, `ITrainerAssignmentLookup`, `ITrainerStatsLookup`, `IUserDisplayLookup`, `IUserProfileLookup` and `IUserProfileUpdater`. These map naturally onto future HTTP or gRPC calls.

## 3. What blocks extracting a module as a service

### 3.1 The outbox is shared and delivers events in-process

There is one outbox per gym (`g{N}_outbox`), owned by `OutboxDbContext` in Common. `ProcessOutboxMessagesJob` hands events to MediatR `INotificationHandler<>` handlers inside the same process. A separate service would need its own outbox and a message broker. RabbitMQ is planned for Phase 2, but nothing is wired up yet (no MassTransit or RabbitMQ packages are referenced).

### 3.2 Synchronous calls become network calls

Cross-module `.Shared` usage:

| Contract project | Used by |
|---|---|
| `Billing.Shared` | Gamification, Gyms, Identity, Nutrition, Progress, Scheduling, Training (7 modules) |
| `Identity.Shared` | Billing, Communication, Gamification, Gyms, Progress (5 modules) |
| `Gyms.Shared` | Billing, Communication, Identity, Nutrition (4 modules) |
| `Gamification.Shared` | Gyms (1 module) |

`IGymPlanService.IsFeatureEnabledAsync` runs on every feature-gated request. If Billing were a separate service, each of those requests would make a network hop, and a Billing outage would take down the other modules.

### 3.3 Multi-tenancy is managed centrally

`GymProvisioningService` holds a hardcoded list of every module's DbContext and provisions all schemas for a new gym. Each extracted service would need its own setup for new gyms and its own migrations across every gym schema.

### 3.4 Common is a shared core

Tenancy, outbox and the owner dashboard aggregator all live in `GymApp.Common.Infrastructure`. After extraction it would have to become a versioned package. The owner dashboard, which reads across modules, would need its own read model or API composition.

## 4. Cheap steps now that make extraction easier later

1. **Abstract the message transport.** Put a broker abstraction behind event publishing now, for example MassTransit with the in-memory transport, so moving to RabbitMQ becomes a config change.
2. **Give each module its own outbox and inbox tables** instead of the shared per-gym outbox.
3. **Replace hot synchronous lookups with local copies.** For hot read paths like the plan check, keep a cached copy inside each module that integration events keep up to date, instead of calling another module synchronously.
4. **Run the architecture tests in CI** so boundary violations fail the build.

## 5. Recommendation

Stay a monolith until there is a concrete need: separate scaling, separate teams, or separate release schedules.

When that happens:

- **Easiest first candidates:** Communication and Gamification. They mostly consume events.
- **Hardest:** Billing and Identity, because so many modules call them directly.
