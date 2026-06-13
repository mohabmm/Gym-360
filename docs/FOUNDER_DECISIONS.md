# GymApp Platform — FOUNDER_DECISIONS.md

> **Date:** 2026-04-12
> **Decided by:** Founder + Team 1.5 (Domain Expert) defaults where founder said "use default" or did not answer.
> **Purpose:** This is the single source of truth for every business decision that the docs did not cover. Teams 2-11 MUST follow this file. If a decision here conflicts with docs/, this file wins (because the founder spoke after the docs were written).

---

## FORMAT

```
Q[N]: [Question summary]
ANSWER: [Decision]
DECIDED BY: Founder | Default (Team 1.5)
PHASE: MVP | Phase 2
IMPACT: [Which modules/teams are affected]
NOTES: [Any extra context]
```

---

## CATEGORY A — BUSINESS LOGIC

### Q1 — Trainer compensation model
**ANSWER:** (e) Out of scope for MVP. The platform shows session counts and calculated revenue per trainer, but does NOT compute payouts. Gyms handle trainer compensation manually outside the platform.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** MVP (display only) — actual payout calculation is Phase 2.
**IMPACT:** Billing module (earnings dashboard is informational), Trainer app dashboard (shows counts, not payroll).
**NOTES:** The Trainer app shows: total sessions this month, estimated revenue (sessions x package price per session), month-over-month comparison. No "pay trainer" button. No salary/commission config.

---

### Q2 — Multi-trainer per member
**ANSWER:** (a) Yes — a member CAN have multiple trainers simultaneously in the same gym. Each `Session` and `WorkoutAssignment` is owned by exactly one trainer. Each `RemoteWorkoutConfig` is per trainer-client pair. Chat is 1-on-1 per trainer.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** MVP
**IMPACT:** Scheduling, Training, Communication modules. UI shows a trainer picker when the member has multiple trainers.
**NOTES:** The "primary trainer" concept does not exist. The member simply has N active trainer relationships. The trainer list is shown in the member's profile.

---

### Q3 — Trainer at multiple gyms
**ANSWER:** (b) No — a trainer belongs to exactly ONE gym. A coach who works at multiple gyms creates separate accounts (one per gym). Same rule applies to trainees and staff. Owners are the only principals with multi-gym scope.
**DECIDED BY:** Founder (revised 2026-05-01)
**PHASE:** MVP
**IMPACT:** Identity module — `GymMembership` enforces a filtered unique constraint on `(UserId)` for `Status='Active'`. Trainee and Trainer apps remove the gym-context switcher UI; both apps assume one active membership resolved from the JWT `gym_id` claim. Gym Staff app already one-gym scoped. Admin web (Owner persona) keeps a multi-gym chooser driven by `Contract.DashboardMode`.
**NOTES:** Required by the move to **schema-per-Gym hard isolation** (one SQL Server, dynamically-prefixed per-module schemas `g{N}_*`). A user identity living in two tenants would break the isolation invariant. See `docs/07_MULTITENANCY.md`. The `X-Gym-Id` header is retained as defense-in-depth but the JWT claim is authoritative.

---

### Q4 — Trainer leaves the gym
**ANSWER:**
- (a) Clients are auto-unassigned; gym owner reassigns manually via a "reassign clients" UI. Clients keep all their data.
- (b) Workout templates and programs are owned by the trainer. When the trainer leaves, they become invisible at that gym. Gym owner can clone/duplicate them before the trainer is removed.
- (c) Session history and chat history are preserved (immutable audit trail). The trainer's name still shows on historical records.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** MVP
**IMPACT:** Training (template ownership), Gyms (reassignment UI), Communication (chat preservation).

---

### Q5 — Late cancel + no-show consequences
**ANSWER:**
- **(a) Cancellation time policy is CONFIGURABLE by the gym** — NOT hardcoded to 24 hours. The gym owner/manager sets the cancellation window (e.g., 2h, 6h, 12h, 24h, 48h) from the Gym Staff app → Settings → Session Policies. Default: 24 hours.
- **(a) If the member cancels WITHIN the policy window (late cancel) → the session IS deducted from their session package.** The trainer is considered to have earned that session (they blocked their schedule). Trainer should be paid for late-cancelled sessions.
- **(b) No additional fee** beyond the deducted session.
- **(c) No-show/late-cancel suspension threshold is CONFIGURABLE by the gym** from the Staff app → Settings → Session Policies. The gym sets: "After X no-shows/late-cancels in Y days → [action]". Actions: `flag_only` (default), `notify_trainer`, `suspend_booking`. Default: 3 in 30 days → notify trainer + flag on profile.
**DECIDED BY:** Founder
**PHASE:** MVP
**IMPACT:** **Major.** Scheduling module needs a `GymSessionPolicy` entity/config (cancellation_window_hours, noshow_threshold, noshow_window_days, noshow_action). Billing module: late-cancel now triggers package decrement. Gyms module: Settings UI for policy config. Staff app: Settings screen for these values. Trainer app: late-cancel sessions show as "deducted (late cancel)" in package history.
**NOTES:** This changes the PRODUCT_SPEC Section 3 acceptance criteria for AC-S52/53 — the 24h is now dynamic. The `CancelSession` handler must read `GymSessionPolicy.CancellationWindowHours` instead of hardcoding 24.

New entities needed:
```
gyms.gym_session_policies (
    gym_id UUID PK,
    cancellation_window_hours INT NOT NULL DEFAULT 24,
    noshow_threshold INT NOT NULL DEFAULT 3,
    noshow_window_days INT NOT NULL DEFAULT 30,
    noshow_action VARCHAR(20) NOT NULL DEFAULT 'flag_only',  -- 'flag_only', 'notify_trainer', 'suspend_booking'
    updated_at DATETIMEOFFSET,
    updated_by UUID
)
```

---

### Q6 — Gym subscription trial period
**ANSWER:** (b) No trial. New gyms start on the Free plan. Free is functionally generous enough to serve as the trial. Self-upgrade when ready.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** MVP
**IMPACT:** Minimal — `is_trial` and `trial_expires_at` columns exist in schema but remain NULL/false for now.

---

### Q7 — Gym stops paying — grace period and data fate
**ANSWER:**
- (a) Confirmed: 7-day warning → 14-day grace → 21-day suspension.
- (b) Hard purge 30 days after admin clicks "Terminate" (NOT after suspension). Suspension preserves data indefinitely.
- (c) Suspended → gym pays → admin manually clicks "Reactivate". No auto-reactivation.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** MVP
**IMPACT:** Quartz purge job triggers on `TerminatedAt + 30 days`. Admin reactivation is a manual button.

---

### Q8 — Gym ownership transfer & member transfer
**ANSWER:**
- (a) Yes — admin-only tool `POST /admin/gyms/{id}/transfer-ownership`. Phase 2.
- (b) No — members keep personal data on their User; gym-side data (sessions, packages) stays at the originating gym.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** (a) Phase 2. (b) MVP (no transfer needed).
**IMPACT:** Minimal for MVP.

---

### Q9 — Refund policy + partial payments + installments
**ANSWER:** Refunds are **Phase 2**. In MVP, refunds are disallowed. If a payment was recorded incorrectly, staff can void it (mark as `Status=Voided` with a reason), but no money flows back through the system. Partial payments and installments are also Phase 2.
**DECIDED BY:** Founder
**PHASE:** Phase 2
**IMPACT:** Billing module — `Payment.Status` enum gets a `Voided` value. No negative-amount rows in MVP. Revenue dashboard excludes voided payments. Phase 2 will add `Refunded` status, partial payment tracking, and installment plans.
**NOTES:** Voiding a payment does NOT restore a session-package count automatically — the staff must manually adjust if needed.

---

### Q10 — Currency
**ANSWER:** EGP only in MVP. Multi-currency is Phase 2.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** MVP (EGP only)
**IMPACT:** Money VO hardcodes `Currency = "EGP"`. Display: `ج.م` in Arabic, `EGP` in English.

---

## CATEGORY B — SCHEDULING

### Q11 — Booking trainers (any vs assigned)
**ANSWER:** (b) Assigned trainer only. Members can browse all trainers (public profiles + ratings) for discovery, but booking requires an active assignment. The gym owner or trainer must accept the relationship first.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** MVP
**IMPACT:** Scheduling `BookSession` validator checks active assignment exists.

---

### Q12 — Maximum sessions per day per trainer
**ANSWER:** No hard cap. UI shows a warning at 8 sessions/day and a confirmation dialog at 10. Trainer can override both.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** MVP
**IMPACT:** Scheduling validators — soft warning, not a blocking rule.

---

### Q13 — Different prices per session type
**ANSWER:** No. Pricing is per session-package template, not per session type. Trainers do not set their own prices; the gym owner does.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** MVP
**IMPACT:** No `price` field on `Session`. All pricing goes through `SessionPackageTemplate`.

---

### Q14 — Public holidays
**ANSWER:** No auto-block. Trainers manually block dates. Phase 2 = optional Egyptian national-holiday calendar import.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** Phase 2 (auto-import)
**IMPACT:** Scheduling — no holiday integration in MVP.

---

### Q15 — Re-registration during 30-day deletion window
**ANSWER:** (a) Restore — the user changed their mind. Confirm via OTP. Data is recovered intact.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** MVP
**IMPACT:** Identity `RegisterUserCommand` must check for soft-deleted accounts with the same email and offer restoration.

---

## CATEGORY C — MEMBER EXPERIENCE

### Q16 — Minimum age
**ANSWER:** 16+. Under 16 requires parental consent (parent registers on the child's behalf). Under 13 blocked entirely.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** MVP
**IMPACT:** Identity validation (date of birth check), consent flow (parental consent type for 13-15).

---

### Q17 — Family / couple memberships
**ANSWER:** No. Each member has their own account and membership. Phase 2 if a customer asks.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** Phase 2
**IMPACT:** None for MVP.

---

### Q18 — Guest passes
**ANSWER:** No lightweight guest entity. A guest is a regular member with a short-duration plan (e.g., "1-Week Trial" membership plan created by the gym). Phase 2 = dedicated guest pass system.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** Phase 2 (dedicated system)
**IMPACT:** None for MVP — gym creates short plans.

---

### Q19 — Referral rewards
**ANSWER:** No referral program in MVP. Phase 2.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** Phase 2
**IMPACT:** None for MVP.

---

### Q20 — Member-to-member visibility
**ANSWER:** Private. Members see each other ONLY on opt-in leaderboards. No member directory. No member-to-member chat.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** MVP
**IMPACT:** Trainee app — no "members" tab. Leaderboard shows names only for opted-in members.

---

## CATEGORY D — DATA & PRIVACY

### Q21 — Workout log visibility
**ANSWER:** (a) Member + their assigned trainer(s) only. Gym managers see aggregate counts (sessions completed, attendance) but NOT exercise/set/rep detail. PDPL data minimisation.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** MVP
**IMPACT:** Training read-side authorization: `GetWorkoutLogs` requires `userId == caller || trainerAssignment.trainerId == caller`.

---

### Q22 — Progress photo visibility
**ANSWER:** Only the trainer the member explicitly shared with. Gym owner/managers cannot see photos. Photos encrypted at rest.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** MVP
**IMPACT:** Progress photo authorization. Encrypted blob storage.

---

### Q23 — Chat message visibility
**ANSWER:** No management access to chat content. Chats are visible only to the two participants. Management can see metadata (chat exists, message count, last active) but not content.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** MVP
**IMPACT:** Communication authorization. PDPL compliance.

---

### Q24 — Data export format & SLA
**ANSWER:** JSON archive + summary PDF, bundled in ZIP with photos as JPEG. Delivered within 24 hours via Quartz job. Email with signed download URL (24h TTL).
**DECIDED BY:** Default (Team 1.5)
**PHASE:** MVP
**IMPACT:** Identity service (export job), blob storage (temp ZIP).

---

### Q25 — Sensitive data extra consent
**ANSWER:** Registration consent covers body metrics and health flags. Progress photos require an explicit in-context confirmation on first upload ("These photos will be encrypted and only shared if you choose"). One-time, dismissible.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** MVP
**IMPACT:** Trainee app UX (photo upload dialog), Identity (consent type `photo_sharing`).

---

## CATEGORY E — TECHNICAL GAPS

### Q26 — Offline mode
**ANSWER:** (b) Write-queue offline with 7-day local retention. Conflict resolution = last-writer-wins by record ID. Trainee/Trainer apps queue writes (workout logs, session logs, body metrics) and replay on reconnection. Kiosk does NOT support offline (must be online).
**DECIDED BY:** Default (Team 1.5)
**PHASE:** MVP
**IMPACT:** Flutter local DB (Hive/Isar), sync service, conflict resolver — significant scope for Team 7.

---

### Q27 — Image compression
**ANSWER:** Client-side: ≤10 MB hard limit. Server-side: long-edge ≤2048 px, JPEG q80, strip EXIF. Profile photos: ≤512 px, q80.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** MVP
**IMPACT:** Upload handlers, image processing service.

---

### Q28 — Notification frequency caps
**ANSWER:** Notification frequency caps are **Phase 2**. In MVP, users control notifications via per-type opt-outs and quiet hours only. Phase 2 will add:
- Gym owner can configure notification limits per type from the Gym Staff app.
- Platform admin can configure platform-wide notification caps from the Admin Dashboard.
**DECIDED BY:** Founder
**PHASE:** Phase 2
**IMPACT:** Communication module — MVP has per-type toggles + quiet hours only. Phase 2 adds `gym_notification_limits` table and admin-configurable global caps. Feature code TBD (likely `notification_management`).

---

### Q29 — Session timeout
**ANSWER:**
- Mobile: JWT 1h, refresh token 30d (sliding).
- Staff Web: idle timeout 30 min.
- Admin Web: idle timeout 15 min.
- Kiosk: never times out.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** MVP
**IMPACT:** Identity JWT config, web middleware.

---

### Q30 — Account lockout
**ANSWER:** 5 failed attempts in 15 min → locked for 30 min. After 3 lockout cycles → must reset via email OTP.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** MVP
**IMPACT:** Identity login handler, rate-limiting middleware.

---

## CATEGORY F — DOMAIN-SPECIFIC (EGYPTIAN GYM MARKET)

### Q31 — Mixed-section gyms (men's / ladies / mixed)
**ANSWER:** (b) Section is an enum. Gym owner enables which sections their gym has from Settings. Options: `Mens`, `Womens`, `Mixed`, or `None` (single-section gym). Member's section is set when they're added. Trainer assignment validated against section compatibility. Class booking respects section. Notifications respect gendered copy where appropriate.
**DECIDED BY:** Founder
**PHASE:** MVP
**IMPACT:** **Significant for Egyptian market.** Gyms module: `GymSection` enum, gym settings for enabled sections. Identity: `GymMembership.section` validated against enabled sections. Scheduling: trainer-member section compatibility check. Gyms: classes tagged with section. Communication: notification copy variants.

New schema:
```
gyms.gym_sections (
    gym_id UUID NOT NULL,
    section VARCHAR(10) NOT NULL,  -- 'mens', 'womens', 'mixed'
    is_enabled BIT NOT NULL DEFAULT 1,
    PRIMARY KEY (gym_id, section)
)
-- GymMembership.section must be one of the gym's enabled sections
-- Trainer.section must match or be 'mixed' to serve across sections
```

---

### Q32 — Ramadan operating hours
**ANSWER:** (a) Yes — seasonal operating-hour overrides. Gym owner adds a named schedule (e.g., "Ramadan Hours") with a date range that auto-activates and overrides normal hours.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** MVP
**IMPACT:** Gyms module: `gym_seasonal_hours` table with `name`, `from_date`, `until_date`, and `DaySchedule` override. The GET operating-hours endpoint resolves: seasonal (if active) > default.

---

### Q33 — Cash culture (reconciliation report, thermal printing)
**ANSWER:**
- (a) Confirmed — no online payment gateway in MVP. Staff records cash payments at the desk.
- (b) Receipts printable as A4 PDF. Thermal receipt printer support is Phase 2.
- (c) Cash reconciliation report (`GET /api/v1/billing/cash-reconciliation?date=`) is **Phase 2**, gated behind `Pro` plan feature.
**DECIDED BY:** Founder
**PHASE:** (a) MVP. (b) Thermal printer Phase 2. (c) Cash reconciliation Phase 2, Pro plan.
**IMPACT:** Billing module — MVP: record payment + generate PDF receipt. Phase 2 adds: thermal printer format, `cash_reconciliation` feature code (Pro+), daily close-out report.

New Phase 2 feature code:
```
Feature code: cash_reconciliation
Display name: Cash Reconciliation
Category: Billing
Plan: Pro + Business
Description: Daily cash close-out report showing who collected what, totals by payment method, and end-of-day summary.
```

---

### Q34 — Arabic-first UX
**ANSWER:**
- (a) Yes — Arabic is the default language for new accounts.
- (b) Yes — RTL when language is Arabic.
- (c) `ج.م` in Arabic UI, `EGP` in English UI.
- (d) No Hijri calendar in MVP. Gregorian only. Phase 2.
**DECIDED BY:** Default (Team 1.5)
**PHASE:** MVP (a/b/c). Phase 2 (d Hijri).
**IMPACT:** Flutter/Next.js i18n. Date pickers Gregorian only in MVP.

---

### Q35 — Data import from Odoo / Daftra / Excel
**ANSWER:**
- (a) The Gym Staff web app exposes a **Data Import** page (owner-only) on the **Pro** plan and above. Feature code: `data_import`.
- (b) Direction is **one-way import** only: Odoo/Daftra/Excel → GymApp. No outbound sync, no ongoing scheduled sync — each run is a one-off job.
- (c) Accepted format: a single `.xlsx` workbook with four tabs (`Members`, `Payments`, `Sessions`, `InBody`). Odoo and Daftra customers export their data to Excel and upload that. A downloadable template is served from `GET /api/v1/gyms/imports/template.xlsx`.
- (d) Correlation key across tabs is the gym-scoped **MemberNumber** (already modelled on `GymMembership.MemberNumber`).
- (d.1) **REVISED 2026-05-31 (Founder):** an optional **NationalId** column is added to the `Members` tab to ease migration of legacy gym rosters that key on the Egyptian National ID. Stored on the `User` aggregate (fixed `identity` schema — person-wide, not gym-scoped), so the same ID follows a member across gyms. Validation: optional; if present must be exactly 14 digits, else the row imports with `NationalId = null` and a `NationalId` error is written to the errors report (never blocks the member). Also captured by the manual **staff create-member** flow. PDPL: exported in the data-export payload and purged with the rest of the User row on account deletion. MemberNumber remains the cross-tab correlation key.
- (e) Processing is asynchronous (Quartz job). The uploaded file is persisted under `DataImport:StorageRoot` for audit. An error CSV is produced for any rejected rows and is downloadable from `GET /api/v1/gyms/{gymId}/imports/{jobId}/errors.csv`.
- (f) Cross-module fan-out uses integration events from `Gyms.IntegrationEvents`: members land in Identity, payments + session packages in Billing, InBody records in Progress. Modular-monolith rule #4 is preserved.
- (g) Session-package rows whose trainer cannot be resolved are **skipped** (logged to the error report); no placeholder trainer is created.
- (h) Imported session packages carry a reserved `TemplateId = FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFE` so they can be filtered in analytics.
**DECIDED BY:** **Founder** (2026-04-17)
**PHASE:** MVP
**IMPACT:** Gyms (new `DataImportJob` aggregate + table `gyms.data_import_jobs`, Excel parser, Quartz job), Billing (new feature `data_import`, new payment + package handlers), Identity (new cross-module `IGymMemberLookup` contract + handler that creates User + GymMembership), Progress (new InBody handler), Gym Staff web app (new `/data-import` page), tests, docs.
**NOTES:**
- Plans enabled: Pro, Business. Free + Starter stay OFF.
- Max file size: 10 MB.
- Imported users are created with an empty `PasswordHash`. Staff must send them the onboarding OTP flow after the import to set a password.
- Idempotency: re-uploading the same workbook will create **duplicate payments and packages** today. A dedup table keyed on `(ImportJobId, Sheet, RowNumber)` is deferred — staff should only run a file once.

---

## SUMMARY TABLE

| Q | Topic | Decision | Phase | Decided By |
|---|-------|----------|-------|------------|
| Q1 | Trainer compensation | Display only, no payout calc | MVP (display) / Phase 2 (calc) | Default |
| Q2 | Multi-trainer per member | Yes, allowed | MVP | Default |
| Q3 | Trainer at multiple gyms | **No — single-gym only (revised 2026-05-01)** | MVP | **Founder** |
| Q4 | Trainer leaves gym | Owner reassigns; templates hidden; history preserved | MVP | Default |
| Q5 | Late cancel / no-show | **Configurable policy; late cancel deducts session; configurable suspension** | MVP | **Founder** |
| Q6 | Trial period | No trial; start on Free | MVP | Default |
| Q7 | Non-payment grace | 7/14/21 days; purge 30d after Terminate | MVP | Default |
| Q8 | Ownership transfer | Phase 2; no member data transfer | Phase 2 | Default |
| Q9 | Refunds | **Disallowed in MVP; void only** | Phase 2 | **Founder** |
| Q10 | Currency | EGP only | MVP | Default |
| Q11 | Booking trainers | Assigned only | MVP | Default |
| Q12 | Max sessions/day | Soft warning at 8/10, no hard cap | MVP | Default |
| Q13 | Price per session type | No; pricing via package templates | MVP | Default |
| Q14 | Public holidays | Manual block; auto-import Phase 2 | Phase 2 (auto) | Default |
| Q15 | Re-register after delete | Restore account via OTP | MVP | Default |
| Q16 | Minimum age | 16+; parental consent 13-15 | MVP | Default |
| Q17 | Family memberships | No | Phase 2 | Default |
| Q18 | Guest passes | No lightweight guest; use short plans | Phase 2 | Default |
| Q19 | Referral rewards | No | Phase 2 | Default |
| Q20 | Member visibility | Private; leaderboards only | MVP | Default |
| Q21 | Workout log visibility | Member + trainer only | MVP | Default |
| Q22 | Photo visibility | Shared trainer only | MVP | Default |
| Q23 | Chat visibility | Participants only | MVP | Default |
| Q24 | Data export format | JSON + PDF ZIP, 24h SLA | MVP | Default |
| Q35 | Data import from Odoo/Daftra/Excel | **Pro+ only, one-way Excel import, 4-tab workbook** | MVP | **Founder** |
| Q25 | Sensitive data consent | Registration + photo-specific prompt | MVP | Default |
| Q26 | Offline mode | Write-queue, 7d retention, last-writer-wins | MVP | Default |
| Q27 | Image compression | 10MB in, 2048px/q80 out | MVP | Default |
| Q28 | Notification caps | **Phase 2; configurable by gym owner + admin** | Phase 2 | **Founder** |
| Q29 | Session timeout | JWT 1h/30d; web 30/15 min | MVP | Default |
| Q30 | Account lockout | 5 attempts / 15 min → 30 min lock | MVP | Default |
| Q31 | Gym sections (men/women) | **(b) Enum, gym-configured** | MVP | **Founder** |
| Q32 | Ramadan hours | Seasonal override schedules | MVP | Default |
| Q33 | Cash reconciliation | **Phase 2, Pro plan feature** | Phase 2 | **Founder** |
| Q34 | Arabic-first / Hijri | Arabic default; Hijri Phase 2 | MVP / Phase 2 | Default |

---

## FOUNDER-ANSWERED vs DEFAULT BREAKDOWN

**Founder explicitly answered (6):** Q3, Q5, Q9, Q28, Q31, Q33
**Team 1.5 defaults used (28):** Q1, Q2, Q4, Q6, Q7, Q8, Q10, Q11, Q12, Q13, Q14, Q15, Q16, Q17, Q18, Q19, Q20, Q21, Q22, Q23, Q24, Q25, Q26, Q27, Q29, Q30, Q32, Q34

All 34 decisions are now resolved. Teams 2-11 are **UNBLOCKED**.
