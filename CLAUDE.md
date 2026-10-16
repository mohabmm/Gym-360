# GymApp Platform

B2B SaaS gym management platform (Egyptian market). .NET 8 modular monolith + 3 Flutter apps + 2 Next.js apps.

## Architecture

- **.NET 8 modular monolith** — Bookify / Milan Jovanovic patterns
- **9 modules**: Identity, Gyms, Training, Progress, Scheduling, Nutrition, Communication, Billing, Gamification
- Each module = 5 core projects (Domain, Application, Infrastructure, Presentation, IntegrationEvents) + optional `.Shared` (cross-module **sync** contracts)
- **SQL Server 2022** (EF Core only — no Dapper/raw SQL), **Redis** (cache), **Quartz** (outbox), **RabbitMQ** (Phase 2), **Serilog + Seq**
- **MediatR** (CQRS) + **FluentValidation**

See `docs/01_GYMAPP_BACKEND_COMPLETE.md` for full backend reference and solution layout.

## Ironclad Rules

1. **Pure EF Core** — `AsNoTracking()` for reads. `UseSqlServer()` only.
2. **Feature gating** — every gated handler calls `IGymPlanService.IsFeatureEnabledAsync` BEFORE work.
3. **Module boundaries** — NEVER reference another module's Domain/Application/Infrastructure. A module's *shared surface* is exactly its `.Shared` project (sync contracts, consumed via DI) + `.IntegrationEvents` project (async events); those two are the only legal cross-module references. Everything else is module-internal. A `.Shared` project is a leaf — it references nothing. Owner of a contract = the module whose `.Shared` holds it (impl in that module's Infrastructure). Cross-cutting platform abstractions (`ICacheService`, `IDateTimeProvider`, …) stay in `Common.Application`.
4. **Internal sealed** — all handlers and validators.
5. **Bookify patterns** — `Entity`, `Result<T>`, `Error`, domain events, Outbox.
6. **Arabic first** — Arabic primary + English, full RTL.
7. **No polling** — FCM push (mobile) + SignalR (web) after admin Deploy.
8. **DB-driven plans** — never `if (plan == "Pro")`. Always `IsFeatureEnabledAsync`.
9. **Egypt PDPL** — consent tracking, data export, account deletion, TDE at rest.
10. **Per-module DbContext + per-Gym schema isolation (hard multi-tenancy)** — each per-gym DbContext resolves its `SchemaPrefix` from `ITenantContext` and writes to `g{N}_{module}` (e.g., `g1_training`). Global concerns (Users, Owners, Contracts, gym registry) live in fixed cross-tenant schemas: `identity`, `gyms_registry`. Cross-tenant queries (Owner unified dashboard) only via `TenantManagement` aggregator service. See `docs/07_MULTITENANCY.md`.
11. **One-Gym principal rule** — Trainees, trainers, and staff have at most one Active `GymMembership`. Owners scope across many gyms via `User.OwnerId`, not via memberships.
12. **docs/ are authority** — if this file and `docs/` disagree, `docs/` wins.

## Workflow

- Never auto-commit/push — user reviews before each push.
- Ask when requirements are unclear; do not assume.
- Enter plan mode for non-trivial features.
- Run tests after every implementation; integration tests mandatory per feature.

## CodeScene

After changing code, run `code_health_review` on the changed files. If Code Health drops, fix the issues, run the review again, and use `code_health_score` to confirm the result. Before finishing, run `pre_commit_code_health_safeguard` and only stop when it passes.

## Authoritative Documents

| Priority | File | Purpose |
|---|---|---|
| 1 | `PRODUCT_SPEC.md` | 105 user stories, acceptance criteria, per-team review checklists (Section 7) |
| 2 | `FOUNDER_DECISIONS.md` | 34 business decisions — **overrides docs on conflict** |
| 3 | `docs/01_GYMAPP_BACKEND_COMPLETE.md` | Backend: architecture, modules, schemas, endpoints, testing |
| 4 | `docs/02_GYMAPP_FRONTEND_COMPLETE.md` | Flutter apps + Next.js apps + shared packages |
| 5 | `PHASE_2_FEATURES.md` | 20 deferred features — do NOT build |
| 6 | `docs/04_GYMAPP_FEATURES_GUIDE.md` | Feature descriptions + 20 test scenarios |
| 7 | `docs/06_GYMAPP_FEATURE_DESCRIPTIONS.md` | Per-feature plain-language descriptions |

Founder decisions that shape code live in `FOUNDER_DECISIONS.md` (Q3 single-gym trainer/trainee/staff, Q5 per-gym cancel policy, Q9 void-only payments, Q31 gym sections, Q32 seasonal hours, Q35 data import). Read before touching related code.

## Build Status

All 11 teams complete. See `BUILD_SUMMARY.md` for totals and `DISCOVERED_ISSUES.md` for deviations. Team 9 integration/functional tests are the only partial item.
