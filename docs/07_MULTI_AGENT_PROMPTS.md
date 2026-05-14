# GymApp Platform — Single-Prompt Autonomous Build



You are building the GymApp Platform — a B2B SaaS gym management system for the Egyptian market. All requirements are in docs/. Execute 12 specialist teams in strict order. Complete each fully before the next.

## DOCS (read as needed per team)
- docs/01_GYMAPP_BACKEND_COMPLETE.md — Backend architecture, 9 modules, DB, APIs, plans/features, data protection, storage limits, testing
- docs/02_GYMAPP_FRONTEND_COMPLETE.md — 3 Flutter apps + 2 Next.js web apps + shared packages
- docs/04_GYMAPP_FEATURES_GUIDE.md — All features in plain language + 20 test scenarios
- docs/05_GYMAPP_BUSINESS_MODEL.md — Pricing, sales, market analysis
- docs/06_GYMAPP_FEATURE_DESCRIPTIONS.md — Each feature described simply

## RULES (all teams)
1. Pure EF Core — no Dapper. AsNoTracking() for reads.
2. SQL Server 2022 — UseSqlServer().
3. Feature gating — gated handlers check IGymPlanService.IsFeatureEnabledAsync.
4. Modular monolith — no cross-module refs except IntegrationEvents.
5. Internal sealed — all handlers and validators.
6. Bookify patterns — Entity base, Result<T>, Error, domain events, Outbox.
7. Arabic first — Arabic (primary) + English, full RTL.
8. No polling — FCM push (mobile) + SignalR (web) after admin Deploy.
9. DB-driven — plans/features/storage limits are database records. Admin CRUD.
10. Egypt PDPL compliance — consent tracking, data export, account deletion, encryption.
11. Self-validate — each team checks review criteria before proceeding.
12. Docs are authority — no contradicting them.

## EXECUTION ORDER
```
Team 1    → Product Manager (creates PRODUCT_SPEC.md)
Team 1.5  → Domain Expert (finds ALL gaps, asks founder, updates spec) ← ONLY HUMAN PAUSE
Team 2    → Architect
Team 3    → Domain (all 9 modules)
Team 4    → Database
Team 5    → API
Team 6    → Application Logic
Team 7    → Flutter (3 apps)
Team 8    → Web (2 apps)
Team 9    → Dev Testing
Team 10   → DevOps
Team 11   → QA Engineer
```

---

## TEAM 1: PRODUCT MANAGER

Read ALL docs. Create PRODUCT_SPEC.md:
- Section 1: User roles & permissions (CAN/CANNOT/data visibility per role)
- Section 2: 80+ user stories (As a [role], I want... tagged with priority, feature_code, app)
- Section 3: Acceptance criteria (Given/When/Then per story, including plan-gated + edge cases)
- Section 4: Feature gating matrix (feature_code → stories → plan → UX when disabled/enabled)
- Section 5: Data flows (registration, workout normal/remote, dual logging, admin Deploy→push, rating→scoring, payment→receipt)
- Section 6: Edge cases & decisions (plan downgrade, feature disabled, <3 ratings, override removal, remote mode default OFF, late cancel, opt-out, PDPL compliance requirements, storage limits per plan)
- Section 7: Review criteria (yes/no checklist per team for self-validation)

Then proceed to Team 1.5.

---

## TEAM 1.5: DOMAIN EXPERT (Gym Business Specialist)

⚠️ THIS IS THE ONLY TEAM THAT PAUSES FOR HUMAN INPUT ⚠️

You are a gym business domain expert with deep knowledge of how Egyptian gyms operate — scheduling, personal training, membership models, trainer compensation, class management, check-in flows, and member retention.

Your job: Read PRODUCT_SPEC.md and ALL docs. Find EVERY gap, ambiguity, or missing decision that could cause development to stall later. Predict ALL questions that would require the founder's input — questions that the docs do NOT answer.

Think about:

BUSINESS LOGIC GAPS:
- How does trainer compensation work? Per session? Monthly salary? Commission? (affects Billing module)
- Can a member have multiple trainers simultaneously?
- Can a trainer work at multiple gyms?
- What happens to a trainer's clients if the trainer leaves the gym?
- Is there a trial period for new gym subscriptions? How long? Auto-convert to paid?
- What happens to gym data if they stop paying? Grace period? Data deletion?
- Can a gym owner transfer ownership?
- Are there gym-to-gym member transfers?
- Minimum contract period for gym subscription?

SCHEDULING GAPS:
- Can members book sessions with any trainer or only their assigned trainer?
- Maximum sessions per day per trainer?
- Can trainer set different prices for different session types?
- What about public holidays — auto-block?
- No-show policy: after how many no-shows does something happen?

PAYMENT GAPS:
- Refund policy?
- Partial payments allowed?
- Installment plans?
- Currency: EGP only or multi-currency for future expansion?
- Tax/VAT on subscriptions?

MEMBER EXPERIENCE GAPS:
- Minimum age requirement?
- Family/couple membership plans?
- Guest pass system?
- Referral rewards for members (not just gym owners)?
- Can members see other members' profiles?

DATA & PRIVACY GAPS:
- Who can see workout logs? Only the member and their trainer? Or gym management too?
- Progress photos: can gym owner see them or only the trainer the member shared with?
- Chat messages: does gym management have access to trainer-member conversations?
- Data export: what format? How long to process?

TECHNICAL GAPS NOT COVERED IN DOCS:
- Offline mode: how long can data be queued? What if conflicts arise when syncing?
- Image compression: what quality? Max file size for uploads?
- Notification frequency caps: prevent spam?
- Session timeout: how long before auto-logout?
- Account lockout: after how many failed login attempts?

Compile ALL questions into DOMAIN_QUESTIONS.md organized by category. Present them to the founder ALL AT ONCE (not one by one) so they can answer in a single session.

Format each question as:
```
Q1 [CATEGORY]: [Question]
Default assumption if no answer: [your best guess based on Egyptian gym market]
Impact if wrong: [which module/team is affected]
```

AFTER receiving the founder's answers:
1. Update PRODUCT_SPEC.md with the answers
2. Create FOUNDER_DECISIONS.md logging every answer with date
3. For any question the founder says "decide for me" → make the decision, document reasoning
4. Proceed to Team 2

⚠️ DO NOT PROCEED TO TEAM 2 UNTIL THE FOUNDER HAS ANSWERED ALL QUESTIONS ⚠️

---

## TEAM 2: ARCHITECT

Read docs/01 PART 1 + PRODUCT_SPEC.md. Build: GymApp.sln, Common.Domain (Entity, IDomainEvent, Result<T>, Error, IUnitOfWork), Common.Application (ICommand/IQuery/handlers, IGymPlanService 6+ methods including storage limits, Behaviors, PlanErrors), Common.Infrastructure (Outbox pure EF Core + Quartz, Inbox, Interceptor, DateTimeProvider, RedisCacheService, JwtTokenService), Common.Presentation (IEndpoint), API host (Program.cs, Dockerfile, docker-compose, GlobalExceptionHandler), 9 module .csproj skeletons. Self-validate Section 7 "FOR TEAM 2". Proceed.

---

## TEAM 3: DOMAIN

Read docs/01 PART 2 + PRODUCT_SPEC.md + FOUNDER_DECISIONS.md. Build Domain for ALL 9 modules: Entities (private parameterless ctors, factory methods), VOs, Events (sealed *DomainEvent), Enums, Errors, Repository interfaces, Services. IntegrationEvents. Include UserConsent entity for PDPL. Self-validate Section 7 "FOR TEAM 3". Proceed.

---

## TEAM 4: DATABASE

Read docs/01 schemas + SUBSCRIPTION PLANS + DATA PROTECTION + STORAGE LIMITS + PRODUCT_SPEC.md. Build Infrastructure for ALL 9 modules: DbContexts (schema, IUnitOfWork, UseSqlServer), EF Configs, Repos (pure EF Core), Seeds. Billing: dynamic plans/features tables + plan_storage_limits + gym_storage_usage + GymPlanService (Override>Default>OFF, Redis, storage checks). Identity: user_consents table. Self-validate Section 7 "FOR TEAM 4". Proceed.

---

## TEAM 5: API

Read docs/01 endpoints + Admin API + Deploy + Data Protection endpoints + PRODUCT_SPEC.md. Build Presentation for ALL 9 modules + Admin: Plan/Feature CRUD, Deploy (FCM+SignalR), storage limit management. Identity: GET /my-data (export), DELETE /account (PDPL). SignalR hub, FCM service. Self-validate Section 7 "FOR TEAM 5". Proceed.

---

## TEAM 6: APPLICATION LOGIC

Read docs/01 Commands+Queries+Rules + PRODUCT_SPEC.md + FOUNDER_DECISIONS.md. Build Application for ALL 9 modules: Handlers (internal sealed) + Validators + DTOs + Mappers + Event handlers. Feature gating + limit checks + STORAGE LIMIT checks (photos, file storage) in every relevant handler. Module registration. Wire Program.cs. Self-validate Section 7 "FOR TEAM 6". Proceed.

---

## TEAM 7: FLUTTER

Read docs/02 PART 3+4+5+6 + FEATURE GATING + PRODUCT_SPEC.md. Build: gymapp_core (models, ApiClient, theme, widgets, l10n, FeatureGate, FCM handler, consent management UI) + gymapp_trainee (all screens, conditional workout card, PDPL consent on registration, download my data, delete account) + gymapp_trainer (all screens, remote mode toggle) + gymapp_gym (all screens, KIOSK MODE). No polling. Arabic+English RTL. Self-validate Section 7 "FOR TEAM 7". Proceed.

---

## TEAM 8: WEB

Read docs/02 PART 7+8+9+10 + PRODUCT_SPEC.md. TWO web apps were built in Lovable as SEPARATE projects. The Lovable exports are on the local machine at:

- **Admin Dashboard**: /Users/mmagdy2-eg/Downloads/gymapp-admin-dashboard
- **Gym Staff Web**: /Users/mmagdy2-eg/Downloads/gym-staff-app

Copy both into the project:
```
cp -r /Users/mmagdy2-eg/Downloads/gymapp-admin-dashboard ./lovable-export-admin
cp -r /Users/mmagdy2-eg/Downloads/gym-staff-app ./lovable-export-gym-staff
```

CRITICAL UX DIFFERENCES between the two apps — preserve these during migration:
- Admin Dashboard (lovable-export-admin): data-dense, dark sidebar, power-user. Target: platform operator (tech-savvy). Admin team RBAC (super_admin/admin/support/analyst).
- Gym Staff Web (lovable-export-gym-staff): warm, friendly, LIGHT sidebar, large buttons, friendly empty states, no jargon, First Gym branding (deep red #C41E3A). Target: gym owner (may NOT be tech-savvy, used to Excel/paper).

Migration steps (follow docs/02 PART 10):
1. Create Turborepo: gymapp-web/ with packages/@gymapp/ui/ + apps/gym-staff/ + apps/admin/
2. Extract reusable components from BOTH Lovable exports → packages/@gymapp/ui/ (data-table, stat-card, chart-container, modal, sidebar-nav, etc.)
3. Restructure lovable-export-admin → apps/admin/ (App Router)
4. Restructure lovable-export-gym-staff → apps/gym-staff/ (App Router)
5. Replace ALL mock/hardcoded data with TanStack Query hooks calling real API endpoints from docs/01
6. Add TypeScript types from @gymapp/ui/types/
7. Add next-auth + middleware.ts: Admin app checks adminRole (super_admin/admin/support/analyst). Gym Staff app checks gym_staff/gym_owner role.
8. Add SignalR connection: config_updated → invalidate plan-features query (staleTime: Infinity)
9. Add gym suspension handling: gym_suspended/gym_reactivated push events → show/hide suspension overlay

Keep Lovable's visual design exactly. Only change architecture and data layer. Self-validate Section 7 "FOR TEAM 8". Proceed.

---

## TEAM 9: DEV TESTING

Read docs/01 PART 3 + PRODUCT_SPEC.md. Build 5 test types: Domain Unit (90%+), Application Unit (80%+, test feature gate + storage limits), Integration (Testcontainers.MsSql+Redis), Functional (401/400/200 + PDPL endpoints), Architecture (NetArchTest). Self-validate Section 7 "FOR TEAM 9". Proceed.

---

## TEAM 10: DEVOPS

Docker-compose (SQL Server 2022+Redis+RabbitMQ+Seq, health checks, volumes, TDE encryption enabled), Dockerfile (multi-stage), .github/workflows/tests.yml + deploy.yml. Proceed.

---

## TEAM 11: QA ENGINEER

Read PRODUCT_SPEC.md + docs/04 + docs/06. Create QA_TEST_PLAN.md: test cases per story, E2E workflows (20 scenarios), plan-based tests, role-based access, deploy flow, edge cases, cross-app consistency, PDPL compliance tests (consent, export, deletion), storage limit tests, regression checklist. Self-validate coverage. Proceed.

---

## FINAL STEP

Create BUILD_SUMMARY.md: total files, modules, endpoints, test cases, user stories covered, decisions from FOUNDER_DECISIONS.md, PDPL compliance status, known limitations.

Create DISCOVERED_ISSUES.md: Any issues, ambiguities, or improvement opportunities discovered DURING implementation (Teams 2-11) that were NOT in the original docs or founder's answers. Format:

```
ISSUE-001: [Title]
Discovered by: Team [N]
Category: [Bug/Ambiguity/Enhancement/Missing Feature]
Description: [What was found]
Recommended solution: [Best-fit solution the PM recommends]
Impact: [Which modules/apps affected]
Priority: [P0-Critical/P1-High/P2-Medium/P3-Low]
Action needed: [Founder must decide / PM decided / Deferred to post-launch]
```

This file is for the founder to review AFTER the build is complete. These are NOT blocking — the PM made best-fit decisions during build. The founder can override any decision later.
