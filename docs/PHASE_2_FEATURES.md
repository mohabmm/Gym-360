# GymApp Platform — PHASE 2 FEATURES

> **Date:** 2026-04-12
> **Source:** FOUNDER_DECISIONS.md + PRODUCT_SPEC.md
> **Purpose:** Every feature explicitly deferred to Phase 2. Organized by module. Use this as the Phase 2 backlog seed.

---

## SUMMARY

| # | Feature | Source | Priority | Plan Gate | Module(s) Affected |
|---|---------|--------|----------|-----------|-------------------|
| P2-01 | Trainer payout calculation | Q1 | Medium | Pro+ | Billing |
| P2-02 | Gym ownership transfer | Q8a | Low | Admin-only | Gyms, Identity |
| P2-03 | Refund system (full + partial) | Q9 | High | Starter+ | Billing |
| P2-04 | Installment plans | Q9 | Medium | Pro+ | Billing |
| P2-05 | Multi-currency support | Q10 | Low | Business | Billing, all UI |
| P2-06 | Egyptian holiday auto-import | Q14 | Low | Pro+ | Scheduling |
| P2-07 | Family / couple memberships | Q17 | Medium | Pro+ | Billing, Identity |
| P2-08 | Dedicated guest pass system | Q18 | Low | Starter+ | Gyms, Billing |
| P2-09 | Member referral program | Q19 | Medium | Pro+ | Gamification, Billing |
| P2-10 | Notification frequency caps (configurable) | Q28 | Medium | Configurable by gym owner (Starter+) + Admin | Communication |
| P2-11 | Cash reconciliation report | Q33 | High | **Pro+** | Billing |
| P2-12 | Thermal receipt printer support | Q33 | Medium | Starter+ | Billing, Staff app |
| P2-13 | Hijri calendar toggle | Q34d | Low | All plans | All UI |
| P2-14 | RabbitMQ async messaging (MassTransit) | docs/01 | High | Infrastructure | Common.Infrastructure |
| P2-15 | Online payment gateway (Fawry/Paymob) | docs/01 | High | Starter+ | Billing |
| P2-16 | CSV bulk member import (data migration) | docs/01 | High | Starter+ | Gyms |
| P2-17 | WhatsApp Bot integration | docs/01 | Medium | Business | Communication |
| P2-18 | InBody scanner integration | docs/01 | Low | Business | Progress |
| P2-19 | Video coaching / form-check video calls | docs/01 | Medium | Business | Communication |
| P2-20 | Supplement store module | docs/01 | Low | Business | New module |

---

## DETAILED DESCRIPTIONS

### P2-01: Trainer Payout Calculation
**Source:** Q1 — Founder chose (e) display-only for MVP.
**What:** Add configuration for trainer compensation model per gym: per-session rate, monthly salary, commission %, or hybrid. Auto-calculate payable amounts. Generate payout reports.
**New entities:** `gyms.trainer_compensation_config`, `billing.trainer_payouts`
**UI:** Gym Staff Web → Trainer Settings → Compensation Model. Trainer App → Earnings → Payout Summary.

### P2-02: Gym Ownership Transfer
**Source:** Q8a
**What:** Admin tool `POST /admin/gyms/{id}/transfer-ownership {newOwnerId}`. Transfers `OwnerId`, moves staff_role=owner to new user, demotes old owner to manager.
**UI:** Admin Dashboard → Gym Detail → Transfer Ownership button.

### P2-03: Refund System
**Source:** Q9 — Founder said disallow refunds in MVP; void only.
**What:** Full refund flow: `Payment.Status=Refunded`, negative amount row linked to original, revenue dashboard adjusts. Partial refunds. Reason required. Approval workflow (manager approves, receptionist requests).
**New entities:** `billing.refund_requests`
**UI:** Staff Web → Payments → Refund button on payment detail.

### P2-04: Installment Plans
**Source:** Q9
**What:** Split annual/quarterly membership payments into N installments. Track balance per member. Auto-reminder for upcoming installments. Mark as overdue if missed.
**New entities:** `billing.installment_schedules`, `billing.installment_payments`
**UI:** Staff Web → Record Payment → "Split into installments" option.

### P2-05: Multi-currency Support
**Source:** Q10
**What:** `Money` value object supports any ISO 4217 currency. Gym owner sets gym currency in Settings. Display adapts. Reporting can aggregate in EGP equivalent.
**Impact:** Money VO refactor, all price display across all 5 apps.

### P2-06: Egyptian Holiday Auto-Import
**Source:** Q14
**What:** Optional calendar integration that pre-populates Egyptian national holidays (Eid al-Fitr, Eid al-Adha, Revolution Day, Coptic holidays, etc.) as blocked dates in trainer availability. Trainers can unblock if they choose to work.
**UI:** Trainer App → Availability → "Import Egyptian holidays" toggle.

### P2-07: Family / Couple Memberships
**Source:** Q17
**What:** A `FamilyGroup` entity linking 2-5 member accounts under a single billing plan with a discount. One primary payer, shared expiry.
**New entities:** `billing.family_groups`, `billing.family_members`
**UI:** Staff Web → Add Member → "Add to family group" option.

### P2-08: Dedicated Guest Pass System
**Source:** Q18
**What:** Lightweight `GuestPass` entity (name, phone, validity period, sponsor member). Does not require full registration. QR check-in generates a temporary pass. Limited to N per month per gym.
**New entities:** `gyms.guest_passes`
**UI:** Staff App → Quick Actions → Issue Guest Pass.

### P2-09: Member Referral Program
**Source:** Q19
**What:** Members share a referral code. When a new member joins using it, both get a reward (configurable: free days, discount on next renewal, loyalty points). Tracked in `gamification.referrals`.
**UI:** Trainee App → Settings → "Invite a friend" with shareable link/code.

### P2-10: Notification Frequency Caps (Configurable)
**Source:** Q28 — Founder wants this configurable by gym owner AND admin.
**What:**
- **Gym owner** configures per-notification-type caps from the Gym Staff app (e.g., "Max 3 announcements per week", "Max 1 workout-assigned notification per day").
- **Platform admin** configures global caps from the Admin Dashboard (e.g., "Max 20 notifications per user per day platform-wide").
- System notifications (auth, payments, security) bypass all caps.
**New entities:** `communication.gym_notification_limits`, `communication.platform_notification_limits`
**Feature code:** `notification_management`
**UI:** Gym Staff Web → Settings → Notification Limits. Admin Dashboard → Platform Settings → Notification Caps.

### P2-11: Cash Reconciliation Report
**Source:** Q33 — Founder explicitly marked Phase 2, Pro plan.
**What:** Daily cash close-out report: total collected by method (cash/card/transfer), per-staff breakdown, discrepancies, end-of-day sign-off.
**Feature code:** `cash_reconciliation`
**Plan gate:** Pro + Business
**Endpoint:** `GET /api/v1/billing/cash-reconciliation?date=YYYY-MM-DD`
**UI:** Gym Staff Web → Billing → Daily Close-Out.

### P2-12: Thermal Receipt Printer Support
**Source:** Q33
**What:** Generate ESC/POS formatted receipt output compatible with common 80mm thermal printers (Epson, Star). Connected via Bluetooth or USB to the staff tablet/phone.
**UI:** Staff App → Payment Confirmation → Print Receipt (thermal).

### P2-13: Hijri Calendar Toggle
**Source:** Q34d
**What:** Optional Hijri date display alongside Gregorian in all date pickers and date displays. Particularly useful for Ramadan scheduling and Islamic holiday awareness.
**UI:** Settings → Date Format → "Show Hijri dates".

### P2-14: RabbitMQ Async Messaging
**Source:** docs/01 architecture
**What:** Replace in-process MediatR Outbox publish with MassTransit → RabbitMQ for true async cross-module events. Enables independent scaling and failure isolation.
**Impact:** Common.Infrastructure, all module IntegrationEvent handlers.

### P2-15: Online Payment Gateway
**Source:** docs/01
**What:** Integrate Fawry and/or Paymob for online card/mobile wallet payments. Members pay membership renewals and PT packages from the Trainee app. Automatic reconciliation with billing records.
**New entities:** `billing.payment_gateway_transactions`

### P2-16: CSV Bulk Member Import
**Source:** docs/01 — data migration
**What:** Gym owner uploads a CSV (name, phone, email, memberNumber, plan, status) to bulk-import existing members. Validates against duplicates, preserves member numbers for kiosk continuity.
**UI:** Gym Staff Web → Members → Import from CSV.

### P2-17 through P2-20
These are listed in docs/01 as potential future features. No detailed spec exists yet. They will be specified when prioritised.

---

## PHASE 2 FEATURE CODES TO PRE-CREATE IN ADMIN DASHBOARD

These feature codes should be created (disabled by default) so they're ready for Phase 2 toggling:

```
cash_reconciliation       → Pro + Business
notification_management   → Starter + Pro + Business
family_memberships        → Pro + Business
guest_passes              → Starter + Pro + Business
referral_program          → Pro + Business
trainer_payouts           → Pro + Business
online_payments           → Starter + Pro + Business
csv_import                → Starter + Pro + Business
hijri_calendar            → All plans
whatsapp_integration      → Business
inbody_integration        → Business
video_coaching            → Business
```

---

## PRIORITY ORDER (Suggested)

1. **P2-15** Online payment gateway (revenue enabler)
2. **P2-03** Refund system (operational need)
3. **P2-11** Cash reconciliation (Pro upsell)
4. **P2-16** CSV bulk import (onboarding friction reducer)
5. **P2-14** RabbitMQ (scaling prerequisite)
6. **P2-10** Notification caps (user experience)
7. **P2-01** Trainer payouts (gym demand)
8. **P2-04** Installments (gym demand)
9. **P2-12** Thermal printer (gym demand)
10. **P2-07** Family memberships
11. **P2-09** Referral program
12. Everything else based on customer demand.
