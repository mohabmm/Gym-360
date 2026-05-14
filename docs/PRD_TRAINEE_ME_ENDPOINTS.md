# PRD — Trainee `/me` Endpoints (Meal Plan, Session Packages, Payments)

**Owner:** Product Engineering
**Date:** 2026-04-15
**Scope:** MVP
**Target apps:** `gymapp_trainee` (Flutter)
**Target modules:** `Nutrition`, `Billing`
**Status:** Ready for implementation

---

## Purpose

Three read-only, trainee-facing endpoints that surface **the trainee's own** nutrition and billing data inside the mobile app. These endpoints back the following screens:

- Trainee → Nutrition tab → "My Meal Plan" card
- Trainee → Profile/Settings → "My Packages" list
- Trainee → Profile/Settings → "Payment History" list

All three follow a consistent `/me/...` convention: the **authenticated user is the subject**, the current gym is implicit via header, and the caller cannot target another user.

---

## Cross-cutting decisions (apply to all three endpoints)

### Authentication

- **JWT required** on every request. Anonymous calls return `401 UNAUTHORIZED`.
- `userId` is **always** read from the `sub` claim of the JWT.
- There is **no `userId` path/query parameter** on any of these endpoints — by design, a trainee cannot view another user's data through these routes. Trainer-facing equivalents (e.g. `GET /api/v1/nutrition/clients/{id}/...`) already exist for cross-user reads and are out of scope here.

### Gym scope (`X-Gym-Id`)

- Per **Founder Q3** (`FOUNDER_DECISIONS.md`), a user (trainee or trainer) can belong to multiple gyms. The current gym is selected in the mobile app's gym switcher and sent on every request via the `X-Gym-Id` header.
- The header is **required** for all three endpoints. Missing header → `400 GYM_CONTEXT_REQUIRED`.
- Middleware validates that the authenticated `userId` has an **active `GymMembership`** at `X-Gym-Id`. If not → `403 NOT_A_MEMBER_OF_GYM` (does not leak whether the gym exists).
- All data returned **MUST be scoped to that gym only**. A trainee with packages at Gym A and Gym B calling with `X-Gym-Id = gymB` sees Gym B packages only.

### Gym suspension

- If the target gym's `Status == Suspended`, all three endpoints return `403 GYM_SUSPENDED` (consistent with `PRODUCT_SPEC.md §5` and the global suspension rule).
- Suspension check runs **before** data loading in the same pipeline middleware/behaviour used by every other gym-scoped endpoint.
- Note: suspension blocks **writes** globally; these endpoints are reads but still return `403 GYM_SUSPENDED` because the mobile app shows a full suspension overlay and must not render stale data underneath it.

### Plan / feature gating — NONE

- **None of these endpoints are plan-gated.** This is intentional and non-negotiable.
- PDPL (Egypt Personal Data Protection Law) principle: a member MUST always be able to access their own personal data regardless of the gym's subscription state. Hiding a member's own payment history or their own assigned meal plan because the gym downgraded plan would block lawful access to personal data.
- The upstream creation endpoints (e.g. `POST /api/v1/nutrition/meal-plans` for trainers, `POST /api/v1/billing/packages/assign` for staff) remain plan-gated on `meal_plans` and `session_packages`. This PRD gates nothing on the read side.
- Handlers MUST NOT call `IGymPlanService.IsFeatureEnabledAsync` for these three routes.

### Response envelope

All responses use the standard envelope defined in `docs/01_GYMAPP_BACKEND_COMPLETE.md §API conventions`:

```json
{ "data": <payload>, "errors": [], "meta": {} }
```

For paginated endpoints, `meta` contains `{ "page": 1, "pageSize": 20, "total": 42, "totalPages": 3 }`.

### Empty results

Absence of data is a **200 OK with empty payload**, never a 404. Rationale: the resource (the trainee) exists; they simply have nothing of that kind yet. A 404 would force the mobile app to branch on error paths for the normal empty state.

### Currency

Per **Founder Q10**, MVP is EGP only. All monetary fields return `{ "amount": <decimal>, "currency": "EGP" }`. Clients render `ج.م` (Arabic) or `EGP` (English).

### Voided payments (Q9)

Per **Founder Q9**, refunds do not exist in MVP. When staff cancel an incorrectly recorded payment, they **void** it → `Payment.Status = Voided`. Voided payments:

- **ARE included** in the trainee's payment history response (they remain part of the audit trail the member is entitled to see).
- Are tagged `"status": "Voided"` so the client can render them struck-through with a "Voided" badge.
- Are **excluded from any total/summary** the mobile app computes from the list (UI rule, not a server filter).
- Are **never hidden**, **never deleted**, and **never returned as negative amounts**.

### Caching

- `Cache-Control: private, max-age=0, must-revalidate` — the data is user-specific, never shared.
- ETags are optional and not required for MVP.

### Rate limiting

Falls under the standard authenticated-trainee rate-limit bucket (no endpoint-specific override needed).

### Observability

- Log `userId`, `gymId`, route, duration, result count. Never log meal plan contents, package pricing, or payment amounts in plain logs.
- Emit standard `api_request` metric with `route` tag.

### Out of scope for this PRD (all three endpoints)

- Write operations (creation, mutation, void, cancel) — existing admin/staff/trainer endpoints cover these.
- Cross-gym aggregation (e.g. "all my packages across all gyms"). The mobile app calls this endpoint once per gym if it ever needs aggregation, but in practice it only ever renders the current gym.
- Admin or staff access to a specific member's data — that uses different, role-gated routes already spec'd.
- Offline cache semantics — handled by the Flutter client per `Q26` (write-queue offline). These are read endpoints; the client caches the last successful response locally.

---

## 1. `GET /api/v1/nutrition/me/meal-plan`

### User story

> As a trainee, I want to view the meal plan my trainer assigned to me in the current gym so that I can follow my daily meals, macros, and alternatives.

### Request

| Attribute | Value |
|---|---|
| Method | `GET` |
| Path | `/api/v1/nutrition/me/meal-plan` |
| Auth | JWT required; `userId` from `sub` claim |
| Headers | `Authorization: Bearer <jwt>` (required), `X-Gym-Id: <guid>` (required), `Accept-Language: ar|en` (optional, defaults to user preference) |
| Path params | none |
| Query params | none |
| Body | none |

### Behaviour

The handler loads the **single currently-assigned, non-template** `MealPlan` where `ClientId == userId` and `GymId == X-Gym-Id`. If the trainee has had multiple meal plans assigned over time, return the **most recently assigned** one (by `AssignedAt DESC`). Older plans are not returned here (add `GET .../me/meal-plan/history` in Phase 2 if demand emerges).

If the trainee has no meal plan assigned in this gym, return `200 OK` with `"data": null`.

### Response (`200 OK`)

```json
{
  "data": {
    "id": "guid",
    "name": "string",
    "assignedByTrainerId": "guid",
    "assignedByTrainerName": "string",
    "assignedAt": "2026-04-10T09:30:00+03:00",
    "dailyCalorieTarget": 2200,
    "macroTargets": {
      "proteinGrams": 165,
      "carbsGrams": 220,
      "fatGrams": 75
    },
    "days": [
      {
        "dayNumber": 1,
        "label": "Monday",
        "plannedMeals": [
          {
            "mealType": "Breakfast",
            "name": "Ful + eggs + bread",
            "items": [
              { "foodName": "Ful medames", "quantity": 150, "unit": "g", "calories": 250, "proteinGrams": 18, "carbsGrams": 28, "fatGrams": 6 }
            ],
            "alternatives": [
              { "name": "Greek yogurt + oats", "items": [ /* same shape */ ] }
            ]
          }
        ],
        "dailyTotals": { "calories": 2180, "proteinGrams": 162, "carbsGrams": 218, "fatGrams": 74 }
      }
    ],
    "notes": "Hydrate 3L/day. Skip alternatives on training days."
  },
  "errors": [],
  "meta": {}
}
```

### Field reference

| Field | Type | Required? | Notes |
|---|---|---|---|
| `data` | object \| null | nullable | `null` when trainee has no assigned plan |
| `data.id` | GUID | required | MealPlan aggregate id |
| `data.name` | string | required | Trainer-given name |
| `data.assignedByTrainerId` | GUID | required | Trainer who assigned this plan |
| `data.assignedByTrainerName` | string | required | Display name, localised where available |
| `data.assignedAt` | ISO 8601 | required | Always with timezone offset |
| `data.dailyCalorieTarget` | int | required | kcal |
| `data.macroTargets.{protein,carbs,fat}Grams` | int | required | |
| `data.days[]` | array | required | Length matches plan (typically 7) |
| `data.days[].dayNumber` | int (1-based) | required | |
| `data.days[].label` | string | nullable | Optional human label |
| `data.days[].plannedMeals[]` | array | required | May be empty for rest days |
| `data.days[].plannedMeals[].mealType` | enum | required | `Breakfast|Lunch|Dinner|Snack|PreWorkout|PostWorkout` |
| `data.days[].plannedMeals[].items[]` | array | required | |
| `data.days[].plannedMeals[].alternatives[]` | array | nullable | May be empty |
| `data.days[].dailyTotals` | object | required | Sum over plannedMeals (server-computed) |
| `data.notes` | string | nullable | Free-text from trainer |

### Acceptance criteria

- [ ] Returns `200` + plan object when an active `MealPlan` exists for `(ClientId=sub, GymId=header)`.
- [ ] Returns `200` + `"data": null` when no plan exists.
- [ ] Returns the single most-recently-assigned plan when multiple exist.
- [ ] Never returns meal plans from other gyms, even if the trainee has them elsewhere.
- [ ] Never returns template plans (`IsTemplate == true`).
- [ ] Returns `401` with no JWT.
- [ ] Returns `400 GYM_CONTEXT_REQUIRED` with no `X-Gym-Id`.
- [ ] Returns `403 NOT_A_MEMBER_OF_GYM` when JWT user has no active membership in `X-Gym-Id`.
- [ ] Returns `403 GYM_SUSPENDED` when the gym is suspended.
- [ ] Does NOT call `IGymPlanService.IsFeatureEnabledAsync` — the `meal_plans` feature flag is irrelevant to the trainee's read of their own plan.
- [ ] Uses `AsNoTracking()` (Ironclad Rule #1).
- [ ] Latency budget: p95 < 200 ms warm cache.

### Edge cases

- **Trainer who assigned the plan has since left the gym.** Plan is still returned with the trainer's historical display name (Q4: history preserved).
- **Plan was assigned at Gym A, trainee switches to Gym B context.** Endpoint returns `null` under `X-Gym-Id = gymB`.
- **Gym downgraded to a plan without `meal_plans`.** Endpoint still returns the existing plan. New plans cannot be created by trainers, but existing data remains readable by its owner (PDPL).
- **Plan with zero days** (malformed): return `200` with `days: []`. Log a warning.

---

## 2. `GET /api/v1/billing/me/packages`

### User story

> As a trainee, I want to see my session packages at the current gym so that I know how many PT sessions I have left and when each package expires.

### Request

| Attribute | Value |
|---|---|
| Method | `GET` |
| Path | `/api/v1/billing/me/packages` |
| Auth | JWT required; `userId` from `sub` claim |
| Headers | `Authorization: Bearer <jwt>` (required), `X-Gym-Id: <guid>` (required) |
| Path params | none |
| Query params | `status` (optional, one of `Active|Exhausted|Expired|All`, default `Active`), `page` (optional, default 1), `pageSize` (optional, default 20, max 50) |
| Body | none |

### Behaviour

Loads `SessionPackage` rows where `MemberId == userId` and `GymId == X-Gym-Id`, filtered by `status` (default `Active`), ordered by `Status ASC (Active first), ExpiresAt ASC NULLS LAST, CreatedAt DESC`. Paginated.

"Active" means `Status == Active` (`RemainingSessions > 0` and not yet expired by time). "Exhausted" = `RemainingSessions == 0`. "Expired" = past `ExpiresAt`. "All" returns every status.

### Response (`200 OK`)

```json
{
  "data": [
    {
      "id": "guid",
      "name": "10 Sessions — Personal Training",
      "trainerId": "guid",
      "trainerName": "Ahmed Saleh",
      "totalSessions": 10,
      "remainingSessions": 6,
      "status": "Active",
      "pricePerSession": { "amount": 500.00, "currency": "EGP" },
      "totalPrice": { "amount": 5000.00, "currency": "EGP" },
      "assignedAt": "2026-03-01T11:00:00+02:00",
      "expiresAt": "2026-06-01T00:00:00+02:00",
      "lastUsedAt": "2026-04-10T18:00:00+02:00"
    }
  ],
  "errors": [],
  "meta": { "page": 1, "pageSize": 20, "total": 3, "totalPages": 1 }
}
```

### Field reference

| Field | Type | Required? | Notes |
|---|---|---|---|
| `data` | array | required | Empty `[]` when none |
| `data[].id` | GUID | required | |
| `data[].name` | string | required | Copied from template at assignment time |
| `data[].trainerId` | GUID | required | The trainer the package is bound to |
| `data[].trainerName` | string | required | Display name at time of query |
| `data[].totalSessions` | int | required | |
| `data[].remainingSessions` | int | required | 0 when exhausted |
| `data[].status` | enum | required | `Active|Exhausted|Expired` |
| `data[].pricePerSession` | Money | required | `{amount, currency}` |
| `data[].totalPrice` | Money | required | |
| `data[].assignedAt` | ISO 8601 | required | |
| `data[].expiresAt` | ISO 8601 | nullable | Null if package has no expiry |
| `data[].lastUsedAt` | ISO 8601 | nullable | Null if never used |

### Acceptance criteria

- [ ] Returns `200` with an array (possibly empty).
- [ ] Default filter `Active` returns only active packages.
- [ ] `status=All` returns every status.
- [ ] `status=Expired` returns only expired packages.
- [ ] Results scoped strictly to `X-Gym-Id`.
- [ ] Pagination metadata is correct and `pageSize` is clamped to `[1, 50]`.
- [ ] Returns `401` with no JWT.
- [ ] Returns `400 GYM_CONTEXT_REQUIRED` with no `X-Gym-Id`.
- [ ] Returns `403 NOT_A_MEMBER_OF_GYM` when not a member of `X-Gym-Id`.
- [ ] Returns `403 GYM_SUSPENDED` when the gym is suspended.
- [ ] Does NOT gate on `session_packages` feature (PDPL — trainee's own data).
- [ ] Uses `AsNoTracking()`.
- [ ] Latency budget: p95 < 200 ms.

### Edge cases

- **Trainee has no packages.** `200` with `data: []` and `meta.total: 0`.
- **Package's trainer has left the gym** (Q4). Package still returned; `trainerName` reflects historical name. `trainerId` still present (frontend can disable "book with this trainer" if trainer is inactive).
- **Package expired yesterday.** Appears only under `status=Expired` or `status=All`.
- **Late-cancel deducted a session** (Q5). That decrement is already reflected in `remainingSessions`. A usage history sub-resource is Phase 2.
- **Trainee was assigned the same template twice.** Both are separate `SessionPackage` rows and both appear.

---

## 3. `GET /api/v1/billing/me/payments`

### User story

> As a trainee, I want to see the payments recorded against me at the current gym so that I have a personal record of what I've paid and for what.

### Request

| Attribute | Value |
|---|---|
| Method | `GET` |
| Path | `/api/v1/billing/me/payments` |
| Auth | JWT required; `userId` from `sub` claim |
| Headers | `Authorization: Bearer <jwt>` (required), `X-Gym-Id: <guid>` (required) |
| Path params | none |
| Query params | `from` (optional, ISO date, inclusive), `to` (optional, ISO date, inclusive), `purpose` (optional, `Membership|SessionPackage|Product|All`, default `All`), `page` (optional, default 1), `pageSize` (optional, default 20, max 50) |
| Body | none |

### Behaviour

Loads `Payment` rows where `MemberId == userId` and `GymId == X-Gym-Id`, filtered by optional date range and purpose, ordered by `PaidAt DESC`. Paginated.

**All payment statuses are returned by default, including `Voided`.** There is no filter to hide them — per Q9 the trainee is entitled to see their complete payment record for their own gym.

### Response (`200 OK`)

```json
{
  "data": [
    {
      "id": "guid",
      "receiptNumber": "REC-MAA01-20260410-0042",
      "amount": { "amount": 5000.00, "currency": "EGP" },
      "paymentMethod": "Cash",
      "purpose": "SessionPackage",
      "referenceId": "guid",
      "referenceLabel": "10 Sessions — Personal Training",
      "status": "Completed",
      "paidAt": "2026-04-10T17:32:00+03:00",
      "recordedByStaffName": "Mona Hassan",
      "voidReason": null,
      "voidedAt": null
    },
    {
      "id": "guid",
      "receiptNumber": "REC-MAA01-20260315-0031",
      "amount": { "amount": 1200.00, "currency": "EGP" },
      "paymentMethod": "Card",
      "purpose": "Membership",
      "referenceId": "guid",
      "referenceLabel": "Monthly Gold",
      "status": "Voided",
      "paidAt": "2026-03-15T11:00:00+02:00",
      "recordedByStaffName": "Mona Hassan",
      "voidReason": "Recorded against wrong member",
      "voidedAt": "2026-03-16T09:10:00+02:00"
    }
  ],
  "errors": [],
  "meta": { "page": 1, "pageSize": 20, "total": 14, "totalPages": 1 }
}
```

### Field reference

| Field | Type | Required? | Notes |
|---|---|---|---|
| `data` | array | required | Empty `[]` when none |
| `data[].id` | GUID | required | |
| `data[].receiptNumber` | string | required | Human-readable `REC-{GymCode}-{YYYYMMDD}-{SEQ}` |
| `data[].amount` | Money | required | Always positive — **no negative amounts** per Q9 |
| `data[].paymentMethod` | enum | required | `Cash|Card|Online|BankTransfer` |
| `data[].purpose` | enum | required | `Membership|SessionPackage|Product` |
| `data[].referenceId` | GUID | nullable | FK to plan/package/product |
| `data[].referenceLabel` | string | nullable | Resolved display label (server-side join for UX) |
| `data[].status` | enum | required | `Completed|Pending|Failed|Voided` |
| `data[].paidAt` | ISO 8601 | required | |
| `data[].recordedByStaffName` | string | nullable | Historical staff display name; null if staff deleted |
| `data[].voidReason` | string | nullable | Required when `status=Voided`, null otherwise |
| `data[].voidedAt` | ISO 8601 | nullable | Required when `status=Voided`, null otherwise |

### Acceptance criteria

- [ ] Returns `200` with an array (possibly empty).
- [ ] Voided payments **are included** in the default response.
- [ ] Voided payments carry `status="Voided"`, populated `voidReason`, `voidedAt`, and a **positive** `amount` — never a negative.
- [ ] Date range filtering works on `PaidAt`. `from`/`to` are inclusive. Invalid dates → `400 VALIDATION_ERROR`.
- [ ] Purpose filter works; `All` returns everything.
- [ ] Ordered by `PaidAt DESC`.
- [ ] Results scoped strictly to `X-Gym-Id`.
- [ ] Pagination metadata is correct; `pageSize` clamped to `[1, 50]`.
- [ ] Returns `401` with no JWT.
- [ ] Returns `400 GYM_CONTEXT_REQUIRED` with no `X-Gym-Id`.
- [ ] Returns `403 NOT_A_MEMBER_OF_GYM` when not a member of `X-Gym-Id`.
- [ ] Returns `403 GYM_SUSPENDED` when the gym is suspended.
- [ ] Does NOT gate on `payment_recording` feature (PDPL — trainee's own audit trail).
- [ ] Uses `AsNoTracking()`.
- [ ] Latency budget: p95 < 250 ms at 20-row page.

### Edge cases

- **Trainee has no payments.** `200` with `data: []`.
- **Only voided payments exist** (rare — data-entry mistakes). All are returned; the UI shows them struck-through with a "Voided" badge.
- **Receipts spanning a gym-code change.** The stored `receiptNumber` is immutable; returned as-is.
- **Staff user who recorded the payment has been removed.** `recordedByStaffName` is the snapshot name stored on the Payment; if unavailable, return `null`.
- **`referenceId` points to a deleted package/plan.** `referenceLabel` returns the last-known label (stored on the Payment at record time) or `null` if never captured.
- **Phase 2 / refunds.** When refunds ship in Phase 2, a new `Refunded` status will appear here with a negative-signed `refundedAmount` companion field. This PRD does **not** include it — MVP has `Voided` only.

---

## Error catalogue (all three endpoints)

| HTTP | Error code | When |
|---|---|---|
| 400 | `GYM_CONTEXT_REQUIRED` | `X-Gym-Id` header missing or not a GUID |
| 400 | `VALIDATION_ERROR` | Invalid query parameters (bad `status`, bad date, out-of-range `pageSize`) |
| 401 | `UNAUTHORIZED` | Missing or invalid JWT |
| 403 | `NOT_A_MEMBER_OF_GYM` | JWT user has no active `GymMembership` at `X-Gym-Id` |
| 403 | `GYM_SUSPENDED` | Target gym's `Status == Suspended` |
| 500 | `INTERNAL_ERROR` | Unhandled server error |

Response shape for errors follows the standard envelope:

```json
{
  "data": null,
  "errors": [ { "code": "GYM_SUSPENDED", "message": "This gym is currently suspended." } ],
  "meta": {}
}
```

---

## Test plan (mandatory integration tests — Team 9)

Per CLAUDE.md workflow rule #4 ("Run tests after every implementation — integration tests mandatory per feature"), the following must be covered with Testcontainers (SQL Server + Redis):

### Per endpoint

- [ ] 200 happy path with data.
- [ ] 200 empty result (trainee with no plan / no packages / no payments).
- [ ] 401 without JWT.
- [ ] 400 without `X-Gym-Id`.
- [ ] 403 when JWT user is not a member of `X-Gym-Id`.
- [ ] 403 when target gym is suspended.
- [ ] Gym A data never leaks into Gym B response (multi-gym isolation, Q3).
- [ ] Feature flag for `meal_plans` / `session_packages` / `payment_recording` OFF → endpoint STILL returns 200 (regression guard for the PDPL exemption).

### Endpoint-specific

- **Meal plan:** most-recent plan returned when multiple exist; templates never returned.
- **Packages:** default `Active` filter; `status=All`; `status=Expired` boundary; pagination correctness.
- **Payments:** voided payment IS included with `status="Voided"`; amount stays positive; date-range filter inclusive on both ends; purpose filter; pagination correctness.

---

## Open questions / flagged conflicts

None. Docs (`PRODUCT_SPEC.md`, `FOUNDER_DECISIONS.md`, `docs/01_GYMAPP_BACKEND_COMPLETE.md`) are consistent on the rules invoked above:

- Q3 multi-gym with `X-Gym-Id` — covered in PRODUCT_SPEC §5 and FOUNDER_DECISIONS Q3.
- Q9 Voided status — covered in FOUNDER_DECISIONS Q9 and docs/01 MODULE 8.
- Gym suspension 403 — covered in PRODUCT_SPEC §3 / §5.
- PDPL right to one's own data — covered in CLAUDE.md Rule #10 and PRODUCT_SPEC §D (Data & Privacy).

The non-gating decision for these three `/me` reads is an **explicit extension** of the PDPL principle and is introduced by this PRD; it does not contradict any existing doc (the plan-feature matrix in PRODUCT_SPEC lists `meal_plans` / `session_packages` as plan-gated for *creation/assignment*, which remains true).
