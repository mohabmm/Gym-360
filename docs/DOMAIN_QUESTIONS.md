# GymApp Platform — DOMAIN_QUESTIONS.md

> **Team 1.5 — Domain Expert (Gym Business Specialist)**
> **Status:** AWAITING FOUNDER ANSWERS — Teams 2-11 are BLOCKED until this file is filled out into `FOUNDER_DECISIONS.md`.
> **Audience:** The founder.
> **How to use:** Read every question. Answer inline (or paste your answers in chat). For any question you don't want to answer, write "decide for me" — Team 1.5 will pick the default and document the reasoning.
> **Why all at once:** The founder's time is the constraint. Answer in one sitting; the engineering teams then run unblocked.

There are **34 questions** across 7 categories. Each lists the **default** the engineering teams will use if you don't answer, and the **impact** of being wrong.

---

## CATEGORY A — BUSINESS LOGIC (gym ↔ trainer ↔ member ↔ owner)

### Q1 [BUSINESS] — Trainer compensation model
**Question:** How are trainers paid? Pick one (or describe a hybrid):
  - (a) Per-session — gym pays trainer X EGP per completed session (the system needs to compute trainer earnings from session counts).
  - (b) Monthly salary — trainer is salaried, sessions don't drive payout (the earnings dashboard is informational only).
  - (c) Commission — trainer keeps a % of session-package revenue assigned to them.
  - (d) Hybrid — base salary + per-session bonus + commission %.
  - (e) Out of scope — gym handles trainer payouts manually outside the platform; we just show session counts.
**Default if no answer:** (e) — show counts and revenue, no automatic payout calculation in MVP.
**Impact if wrong:** Billing module — Trainer earnings dashboard would otherwise be incomplete or misleading. Affects Team 6 (handler logic) and Team 7 (Trainer app dashboard).

### Q2 [BUSINESS] — Multi-trainer per member?
**Question:** Can a single member have **multiple trainers at the same time** in the same gym (e.g., one for strength, one for nutrition)?
  - (a) Yes — multiple active assignments allowed.
  - (b) No — only one current trainer; switching trainers ends the previous assignment.
**Default if no answer:** (a) Yes — allowed. Each session is owned by exactly one trainer; the "primary trainer" is whoever holds the most recent active assignment.
**Impact if wrong:** Scheduling and Training modules. Affects how `WorkoutAssignment`, `Session`, and `RemoteWorkoutConfig` resolve when there is more than one active trainer. Affects chat (1-on-1 vs which trainer the member chats with).

### Q3 [BUSINESS] — Trainer at multiple gyms?
**Question:** Can a trainer work at **multiple gyms** simultaneously through the platform?
  - (a) Yes — one trainer login, multiple gym memberships, switches gym context like trainees do.
  - (b) No — one trainer = one gym; if they leave, they re-register at the new gym.
**Default if no answer:** (b) No — MVP restricts trainers to one gym at a time. Phase 2 can lift this.
**Impact if wrong:** Identity (`GymMembership` model), Trainer app gym switcher UX, Gamification (whose Coach-of-the-Month?), Billing (where do their session counts roll up?).

### Q4 [BUSINESS] — Trainer leaves the gym — what happens to their clients?
**Question:** When a trainer leaves a gym, what happens to:
  - (a) Their clients — auto-unassigned and the gym owner reassigns manually? Or auto-distributed to the remaining trainers?
  - (b) Their workout templates and programs — owned by the trainer (deleted with them) or owned by the gym (kept)?
  - (c) Their session history and chat history — deleted or preserved?
**Default if no answer:**
  - (a) Owner reassigns manually via a "reassign clients" UI; clients keep their progress, workouts, and chat.
  - (b) Templates/programs are owned by the trainer and become invisible when the trainer leaves; gym owner can clone them before the trainer is removed.
  - (c) Session and chat history preserved (immutable audit trail).
**Impact if wrong:** Domain model (ownership), Training and Communication modules.

### Q5 [BUSINESS] — Late cancel + no-show consequences
**Question:** When a member cancels <24h before a session ("late cancel") or no-shows:
  - (a) Does it consume a session from their session-package balance?
  - (b) Does the trainer/system charge a fee?
  - (c) After how many late cancels or no-shows does the member get flagged or suspended? (e.g., 3 in 30 days?)
**Default if no answer:**
  - (a) No — late cancel/no-show does NOT decrement the package.
  - (b) No fee — informational flag only.
  - (c) 3 no-shows in 30 days → trainer is alerted, member profile gets a "high-risk" tag. No automatic suspension.
**Impact if wrong:** Billing (package decrement logic), Scheduling (no-show counters), Communication (notifications).

### Q6 [BUSINESS] — Gym subscription trial period => 
**Question:** When a new gym signs up for the platform, do they get a free trial?
  - (a) Yes — how many days? Auto-converts to paid after expiry, or stays Free until they pay?
  - (b) No trial — they choose Free or paid plan from day one.
**Default if no answer:** (b) No trial. New gyms start on the Free plan and self-upgrade. (Free is functionally generous enough to be the trial.)
**Impact if wrong:** Billing (`gym_subscriptions.is_trial`, `trial_expires_at` columns), Admin gym onboarding flow.

### Q7 [BUSINESS] — Gym stops paying — grace period and data fate
**Question:** Docs/01 says: 7d warning → 14d grace → 21d suspended → eventual termination. Confirm or override:
  - (a) Confirm 7/14/21-day timeline?
  - (b) After how many days of suspension does data get hard-purged? (PDPL requires 30d retention default.)
  - (c) Can a suspended gym pay and instantly reactivate, or is there a manual admin step?
**Default if no answer:**
  - (a) Confirmed 7/14/21.
  - (b) Hard-purge 30 days after Termination is clicked (NOT after suspension — suspension preserves data indefinitely).
  - (c) Suspended → pay → admin manually clicks "Reactivate". (Auto-reactivation is risky; humans approve.)
**Impact if wrong:** Quartz purge jobs (Team 6), Admin reactivation flow (Team 5).

### Q8 [BUSINESS] — Gym ownership transfer & gym-to-gym member transfer
**Question:**
  - (a) Can a gym's `OwnerId` be transferred to another user? (e.g., the owner sells the business)
  - (b) Can a member move their data (workouts, progress) from Gym A to Gym B if both use GymApp?
**Default if no answer:**
  - (a) Yes — admin tool only (`POST /admin/gyms/{id}/transfer-ownership`). Phase 2.
  - (b) No — members keep their personal data tied to their User account, not the gym; rejoining a different gym does NOT migrate gym-side data (sessions, ratings, packages).
**Impact if wrong:** Admin UX, Identity model.

### Q9 [PAYMENT] — Refund policy + partial payments + installments
**Question:**
  - (a) Are refunds allowed? If yes, full only or partial?
  - (b) Can a member pay a membership in installments (e.g., 3 monthly payments for an annual plan)?
  - (c) Tax/VAT — does Egypt require us to compute and display VAT on receipts? Currently 14%.
**Default if no answer:**
  - (a) Refund = a new `Payment` row with negative amount + reason. The receipt links the original.
  - (b) Yes — `Payment` rows are independent; the system tracks balance vs plan price; no auto-debit reminders in MVP.
  - (c) No VAT in MVP — receipts show gross amount only. Phase 2 if a customer asks.
**Impact if wrong:** Billing module, receipt template, revenue dashboard.

### Q10 [PAYMENT] — Currency
**Question:** EGP only in MVP, or do we need multi-currency from day one (USD for tourist gyms in Sharm/Hurghada)?
**Default if no answer:** EGP only. Multi-currency is Phase 2.
**Impact if wrong:** Money value object, Billing module, all price-display UI.

---

## CATEGORY B — SCHEDULING

### Q11 [SCHEDULING] — Booking trainers
**Question:** Can a member book a session with **any** trainer at the gym, or only with their **assigned** trainer?
  - (a) Any trainer — open marketplace inside the gym.
  - (b) Assigned trainer only — the gym owner (or trainer) must accept the relationship first.
**Default if no answer:** (b) Assigned trainer only — protects existing relationships. Any-trainer browsing is a discovery view but booking requires assignment.
**Impact if wrong:** Trainer marketplace UX, Scheduling permission rules.

### Q12 [SCHEDULING] — Maximum sessions per day per trainer
**Question:** Should the system enforce a hard cap (e.g., 10 sessions/day) to protect trainer health and prevent over-booking errors?
**Default if no answer:** No hard cap, but show a warning at 8 and a confirmation dialog at 10. Trainer can override.
**Impact if wrong:** Scheduling validators.

### Q13 [SCHEDULING] — Different prices for different session types
**Question:** Can a trainer set different prices for "Personal Training" vs "Assessment" vs "Follow-up"?
**Default if no answer:** No — pricing is per session-package template, not per session type. Trainers do not set their own prices; the gym owner does.
**Impact if wrong:** Billing schema, package template UI.

### Q14 [SCHEDULING] — Public holidays
**Question:** Should the system auto-block public Egyptian holidays from trainer availability (Eid al-Fitr, Eid al-Adha, Coptic Christmas, Ramadan night-shift schedules)?
**Default if no answer:** No auto-block. Trainers manually block via "Blocked dates". Phase 2 = optional national-holiday calendar import.
**Impact if wrong:** Scheduling availability calculation, possible integration with Egyptian calendar.

### Q15 [SCHEDULING] — Account-deletion-during-30d-grace re-registration
**Question:** If a user deletes their account and tries to re-register with the same email within the 30-day soft-delete window, what happens?
  - (a) Restore the soft-deleted account (data intact).
  - (b) Block the re-registration with "Your account is being deleted, please contact support".
  - (c) Create a fresh account with the same email (delete the soft-deleted one immediately).
**Default if no answer:** (a) Restore — the user changed their mind, give the data back. They confirm via OTP.
**Impact if wrong:** PDPL flow, Identity Delete + Register handlers.

---

## CATEGORY C — MEMBER EXPERIENCE

### Q16 [MEMBER] — Minimum age requirement
**Question:** What is the minimum age to register?
  - (a) 18+ only.
  - (b) 16+ (with parental consent).
  - (c) 13+ (with parental consent).
  - (d) No minimum.
**Default if no answer:** 16+. Under 16 requires parental consent (the parent registers the account on the child's behalf and signs the consent form). PDPL recommends parental consent for minors handling personal data.
**Impact if wrong:** Identity validation, consent flow, possible legal exposure.

### Q17 [MEMBER] — Family / couple membership plans
**Question:** Should a single membership cover multiple linked accounts (e.g., husband + wife)?
**Default if no answer:** No — each member has their own account and pays their own membership. Phase 2 if a customer asks.
**Impact if wrong:** Membership plan model, billing logic.

### Q18 [MEMBER] — Guest passes
**Question:** Can the gym issue temporary guest passes (e.g., 1-week trial) without creating a full member?
**Default if no answer:** No in MVP. A guest is a regular member with a 7-day membership plan. Phase 2 = lightweight guest entity if a customer asks.
**Impact if wrong:** Member management UX.

### Q19 [MEMBER] — Referral rewards
**Question:** Should there be a member-to-member referral program where the referrer gets a reward (free week, discount)?
**Default if no answer:** No in MVP. Phase 2.
**Impact if wrong:** Marketing surface in Trainee app, billing.

### Q20 [MEMBER] — Member-to-member visibility
**Question:** Can members see each other's profiles (within the same gym), or is everyone private?
**Default if no answer:** Private. Members only see each other on **opt-in** leaderboards. No member-to-member chat. No member directory.
**Impact if wrong:** Trainee app UI, privacy model.

---

## CATEGORY D — DATA & PRIVACY

### Q21 [PRIVACY] — Workout log visibility
**Question:** Who can see a member's workout logs?
  - (a) Member + their trainer only.
  - (b) Member + their trainer + gym owner / managers.
**Default if no answer:** (a) Member + assigned trainer(s) only. Gym managers see **aggregate** counts (sessions completed, attendance) but NOT exercise/set/rep details. PDPL data minimisation.
**Impact if wrong:** Read-side authorisation in Training module.

### Q22 [PRIVACY] — Progress photo visibility
**Question:** Can the gym owner / manager see progress photos, or only the trainer the member chose to share with?
**Default if no answer:** Only the trainer the member shared with. Owner/managers cannot see photos. Photos are encrypted at rest.
**Impact if wrong:** Photo authorisation logic, PDPL compliance.

### Q23 [PRIVACY] — Chat message visibility
**Question:** Does gym management have access to trainer-member chat content?
**Default if no answer:** No. Chats are end-to-end visible only to the two participants. Gym management can see **whether** a chat exists and **how many messages**, not contents. Required for trust + PDPL.
**Impact if wrong:** Communication module read-side, possible legal exposure.

### Q24 [PRIVACY] — Data export format & SLA
**Question:** When a user clicks "Download my data":
  - (a) Format: JSON only, JSON + PDF, or ZIP of CSVs?
  - (b) SLA: instant (synchronous), within 24h, or within 7 days (PDPL allows up to 30d)?
**Default if no answer:**
  - JSON archive (machine-readable) + a summary PDF. Bundled in one ZIP. Photos as JPEG inside the ZIP.
  - SLA: within 24h (Quartz job). Email with signed download URL (24h TTL).
**Impact if wrong:** PDPL compliance UX, Identity service, blob storage.

### Q25 [PRIVACY] — Sensitive data extra consent
**Question:** PDPL classifies health conditions, body measurements, and progress photos as **sensitive**. Should the user re-consent at the moment they enter the data, or is the registration consent enough?
**Default if no answer:** Registration consent is enough for body metrics and basic health flags. Progress photos require an **explicit** in-context confirmation the first time the user uploads ("These photos will be encrypted and only shared if you choose"). One-time, dismissible.
**Impact if wrong:** PDPL compliance, UX flow.

---

## CATEGORY E — TECHNICAL GAPS NOT IN DOCS

### Q26 [TECH] — Offline mode for mobile
**Question:** What is the offline behaviour expectation for the Flutter apps (especially Trainer Live Session and Trainee workout logging)?
  - (a) Read-only offline (cached data, no writes).
  - (b) Write-queue offline — data persists locally and syncs when online. How long is the queue allowed to retain data? How are conflicts resolved?
**Default if no answer:** (b) Write-queue with 7-day local retention. Conflict resolution = last-writer-wins by id. The Trainee/Trainer apps queue all writes (workout logs, session logs, body metrics) and replay on reconnection. The kiosk does NOT support offline mode (must always be online).
**Impact if wrong:** Flutter local DB schema, sync engine, large impact on Team 7.

### Q27 [TECH] — Image compression
**Question:**
  - (a) Max input file size for member uploads (profile, progress, form-check)?
  - (b) Server compression target (long-edge px, JPEG quality)?
**Default if no answer:** Client-side: ≤10 MB hard limit. Server-side: long-edge ≤2048 px, JPEG q80, strip EXIF (privacy). Profile photos: long-edge ≤512 px, q80.
**Impact if wrong:** Upload handlers, blob storage budget, Free plan storage limits feasibility.

### Q28 [TECH] — Notification frequency caps
**Question:** Should the platform cap how many notifications a single user can receive in a window (anti-spam) — e.g., max 10/day?
**Default if no answer:** Max 20 notifications/day per user, plus user-defined quiet hours, plus per-type opt-outs. System notifications (auth, payment receipts, account security) bypass the cap.
**Impact if wrong:** Communication module rate limiter, user retention.

### Q29 [TECH] — Session timeout
**Question:** How long before an inactive session auto-logs-out the user?
  - (a) Mobile (Trainee/Trainer/Staff) — JWT lifetime + refresh.
  - (b) Web (Staff/Admin) — idle timeout in the browser.
**Default if no answer:**
  - Mobile: JWT 1h, refresh token 30d, sliding (matches docs/01).
  - Staff Web: idle timeout 30 min.
  - Admin Web: idle timeout 15 min (sensitive — shorter).
  - Kiosk: never times out (it IS the kiosk).
**Impact if wrong:** Identity service, web middleware, UX (users hate being logged out).

### Q30 [TECH] — Account lockout
**Question:** After how many failed login attempts is an account locked, and for how long?
**Default if no answer:** 5 failed attempts in 15 min → account locked for 30 min. After 3 lockout cycles → account requires manual reset via email OTP. Brute-force protection.
**Impact if wrong:** Identity security, support overhead.

---

## CATEGORY F — DOMAIN-SPECIFIC GAPS UNIQUE TO GYM-BUSINESS

### Q31 [DOMAIN] — Mixed-section gyms (men's / ladies / mixed floors)
**Question:** Egyptian gyms commonly have separate "Men's" and "Ladies" sections (sometimes shared cardio + separate weight rooms). The `GymMembership.section` field exists in docs/01. How should it work?
  - (a) Section is a free-text field set when the member is added.
  - (b) Section is an enum (`Men`, `Women`, `Mixed`); the gym owner enables which sections their gym has.
  - (c) Out of scope — section is informational only, no business logic.
**Default if no answer:** (b) Enum with the gym owner enabling sections in Settings. Member's `section` is set on add. Trainer assignment validated (a male trainer cannot be assigned to the women's section unless the gym explicitly allows). Class booking respects section. Notification copy is gendered if section is Women's.
**Impact if wrong:** Critical for Egyptian market — getting this wrong loses gyms. Affects Identity, Gyms, Scheduling, Communication.

### Q32 [DOMAIN] — Ramadan operating hours
**Question:** Egyptian gyms shift dramatically during Ramadan (e.g., open 10 PM-3 AM during the holy month). Should the system support seasonal operating-hour overrides?
  - (a) Yes — the gym owner adds a "Ramadan schedule" that overrides normal hours for the date range.
  - (b) No — the owner manually edits operating hours twice a year.
**Default if no answer:** (a) Yes. `gyms.operating_hours` gets a `season` column or a separate `seasonal_operating_hours` table with `from`/`until` dates.
**Impact if wrong:** Gyms module, member notifications, class scheduling. Egyptian market expectation.

### Q33 [DOMAIN] — Cash-vs-card culture
**Question:** Most Egyptian gym revenue is cash. Confirm:
  - (a) Online card payment is **out of scope** for MVP — staff records cash payments at the desk. Phase 2 may integrate Fawry or Paymob.
  - (b) Receipts must be printable (we expect a thermal receipt printer connected to the staff device).
  - (c) Daily cash-reconciliation report is needed (total cash collected today, who collected each payment, end-of-day close-out).
**Default if no answer:** (a) Confirmed. (b) Receipts printable as A4 PDF; thermal printer support is Phase 2. (c) Yes — `GET /api/v1/billing/cash-reconciliation?date=` for the manager close-out.
**Impact if wrong:** Billing module scope, staff app receipt printing, revenue close-out flow.

### Q34 [DOMAIN] — Arabic-first UX choices
**Question:** Confirm Arabic-first defaults:
  - (a) Default language for new accounts is Arabic (not English).
  - (b) Default direction is RTL when language is Arabic.
  - (c) Default currency display is `EGP` shown as `ج.م` in Arabic, `EGP` in English.
  - (d) Hijri calendar option in date pickers — needed or skip?
**Default if no answer:**
  - (a) Yes — Arabic default; user can switch to English.
  - (b) Yes — RTL when Arabic.
  - (c) `ج.م` in Arabic UI, `EGP` in English UI.
  - (d) Skip Hijri in MVP. Gregorian only. Phase 2.
**Impact if wrong:** Flutter and Next.js i18n setup, l10n bundles, date pickers, Team 7 + 8 scope.

---

## HOW TO ANSWER

**Reply in chat with anything in this format:**

```
Q1: e
Q2: a
Q3: decide for me
Q4: (a) auto-reassign by owner (b) keep owned by trainer hidden (c) preserve
Q5: (a) no (b) no fee (c) 5 no-shows in 30d → suspend
…
Q34: confirm all defaults except (d) yes — add Hijri toggle
```

Anything you don't answer will use the **default**, recorded in `FOUNDER_DECISIONS.md` with the reason. Once you've answered (or said "use defaults for everything"), Team 1.5 will:
1. Update `PRODUCT_SPEC.md` Section 6 with your decisions.
2. Write `FOUNDER_DECISIONS.md` with every decision + date.
3. Unblock Team 2 (Architect).

⚠️ **Until then, no downstream team can start.** ⚠️
