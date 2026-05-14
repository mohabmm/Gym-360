# `API/Endpoints/` — Cross-Module Orchestration Only

This folder is a deliberate exception to the rule that endpoints live in module `Presentation` projects.

## Why endpoints land here

The codebase follows **Rule 3** (CLAUDE.md): a module's Domain / Application / Infrastructure must never reference another module's Domain / Application / Infrastructure. Only `IntegrationEvents` cross module boundaries.

Endpoints that **synchronously orchestrate across two or more modules' Application layers** therefore cannot live inside any single module's `Presentation` without violating Rule 3. The API host project is the only legal composition root that can take direct dependencies on multiple modules at once, so those endpoints live here.

## What belongs here

- HTTP endpoint coordinates commands from **two or more modules** in one request, AND
- Synchronous response contract makes integration-event decomposition infeasible (e.g. the client needs the newly created `userId` + `memberNumber` in the same call).

## What does NOT belong here

- Single-module endpoint → put it in that module's `Presentation/Endpoints/`.
- Endpoint that only touches `Common.Application` abstractions (e.g. `IOwnerDashboardService`) → put it in the most-relevant module's `Presentation/Endpoints/`.
- Endpoint backed by `Common.Infrastructure` services (e.g. `IGymProvisioningService`) → host-level only. Module `Presentation` must not reference `Common.Infrastructure` (layering violation).

## Current contents

| Endpoint | Reason it stays in host |
|---|---|
| `StaffCreateMemberEndpoint` | Identity.Application + Gyms.Application (User + GymMembership in same call) |
| `AdminCreateGymStaffEndpoint` | Identity.Application + Gyms.Application |
| `OwnerCreateGymStaffEndpoint` | Identity.Application + Gyms.Application |
| `OwnerGymsEndpoint` | Direct `IGymProvisioningService` (Common.Infrastructure) dependency |

## Future direction

If/when modules are extracted into microservices, each cross-module endpoint here becomes a refactor candidate: decompose into an owning-module command + integration event, accept eventual consistency, and move the endpoint into the owning module's `Presentation`. Until then, keep this folder small and resist adding endpoints that *could* live in a single module's `Presentation`.
