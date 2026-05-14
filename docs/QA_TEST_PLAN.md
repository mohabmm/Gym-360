# GymApp Platform -- QA Test Plan

> **Owner:** Team 11 (QA Engineer)
> **Date:** 2026-04-12
> **Status:** v1.0
> **Sources:** PRODUCT_SPEC.md, docs/04_GYMAPP_FEATURES_GUIDE.md, docs/06_GYMAPP_FEATURE_DESCRIPTIONS.md, FOUNDER_DECISIONS.md, PHASE_2_FEATURES.md

---

## 1. Test Strategy Overview

### 1.1 Test Levels

| Level | Scope | Tools | Owner |
|-------|-------|-------|-------|
| **Unit (Domain)** | Entity creation, factory methods, domain events, state transitions, value objects | xUnit, FluentAssertions | Team 9 |
| **Unit (Application)** | Handlers, validators, feature gating, storage limits | xUnit, FluentAssertions, NSubstitute | Team 9 |
| **Integration** | Full MediatR pipeline with real DB and cache | xUnit, WebApplicationFactory, Testcontainers.MsSql, Testcontainers.Redis | Team 9 |
| **Functional (API)** | HTTP request/response, auth, validation, status codes | xUnit, WebApplicationFactory, Testcontainers | Team 9 |
| **Architecture** | Layer deps, module boundaries, naming conventions | NetArchTest.Rules | Team 9 |
| **E2E (Manual/Automated)** | Cross-app workflows, UI, real-time sync | Manual + Appium (mobile) + Playwright (web) | Team 11 |
| **PDPL Compliance** | Consent, export, deletion, retention, audit | Manual + automated functional tests | Team 11 |

### 1.2 Test Environments

| Environment | Purpose | Data |
|-------------|---------|------|
| **Local** | Developer testing | docker-compose (SQL Server 2022, Redis, Seq) |
| **CI** | Automated tests on every PR | Testcontainers (ephemeral) |
| **Staging** | Full E2E, QA, UAT | Seeded data (4 plans, 200+ exercises, 50+ foods, sample gyms) |
| **Production** | Smoke tests post-deploy | Synthetic monitoring account only |

### 1.3 Priority Definitions

| Priority | Definition | SLA |
|----------|-----------|-----|
| **P0** | Blocks launch. Core auth, check-in, payments, PDPL. | Must pass before any release |
| **P1** | Major feature. Training, scheduling, progress, analytics. | Must pass before GA |
| **P2** | Nice-to-have. Gamification, advanced analytics, nutrition. | Can ship with known issues |

### 1.4 Exclusions (Phase 2 -- DO NOT TEST)

Per PHASE_2_FEATURES.md, the following are explicitly out of scope:

- P2-01: Trainer payout calculation
- P2-02: Gym ownership transfer
- P2-03: Refund system (full + partial)
- P2-04: Installment plans
- P2-05: Multi-currency support
- P2-06: Egyptian holiday auto-import
- P2-07: Family/couple memberships
- P2-08: Dedicated guest pass system
- P2-09: Member referral program
- P2-10: Notification frequency caps (configurable)
- P2-11: Cash reconciliation report
- P2-12: Thermal receipt printer support
- P2-13: Hijri calendar toggle
- P2-14: RabbitMQ async messaging
- P2-15: Online payment gateway
- P2-16: CSV bulk member import
- P2-17: WhatsApp Bot integration
- P2-18: InBody scanner integration
- P2-19: Video coaching
- P2-20: Supplement store module

---

## 2. User Story Test Cases

### 2.1 Identity and Onboarding (Stories 1-12)

| TC ID | Story | Test Case | Steps | Expected Result | Priority |
|-------|-------|-----------|-------|-----------------|----------|
| TC-001 | S1 | Register with email + OTP | POST `/api/v1/identity/register` with valid email, password, name, phone, role=trainee | 201, accessToken + refreshToken returned, UserRegisteredDomainEvent emitted | P0 |
| TC-002 | S1 | Register with duplicate email | POST register with existing email | 400 `IDENTITY.EMAIL_EXISTS` | P0 |
| TC-003 | S1 | Register with weak password | POST register with password "pa1" | 400 validation error (min 8 chars + uppercase + number) | P0 |
| TC-004 | S1 | OTP rate limit | Request >3 OTPs in 1 hour | 429 `IDENTITY.OTP_RATE_LIMIT` | P0 |
| TC-005 | S1 | OTP expired | Verify OTP after 5 minutes | 400 `IDENTITY.OTP_EXPIRED` | P0 |
| TC-006 | S2 | Google sign-in | Authenticate with Google OAuth | 200/201, valid tokens returned | P0 |
| TC-007 | S2 | Apple sign-in | Authenticate with Apple Sign In | 200/201, valid tokens returned | P0 |
| TC-008 | S3 | Set fitness profile | PUT `/api/v1/identity/profile` with height, weight, goal, level, health flags | 200, profile saved | P0 |
| TC-009 | S4 | Link gym via 6-char code | POST `/api/v1/identity/link-gym` with valid gymCode | GymMembership created, UserLinkedToGymIntegrationEvent emitted | P0 |
| TC-010 | S4 | Link gym via invalid code | POST link-gym with nonexistent code | 404 `GYMS.GYM_NOT_FOUND` | P0 |
| TC-011 | S5 | Switch between gyms | Member in 2 gyms, switch context | Branding, classes, trainers reflect the selected gym | P0 |
| TC-012 | S6 | Trainer registration (pending approval) | Trainer registers, account status = Pending | Account unusable until gym owner approves | P0 |
| TC-013 | S6 | Approve trainer | PUT `/gyms/{id}/trainers/{trainerId}/approve` | Trainer status = Active, can use app | P0 |
| TC-014 | S7 | Invite staff with role | Owner invites staff as manager/receptionist | Staff account created with correct role; multi_staff feature gated | P1 |
| TC-015 | S8 | Add platform admin | super_admin creates another admin with sub-role | New admin can log in with correct permissions | P0 |
| TC-016 | S9 | Password reset via OTP | Request reset, enter OTP, set new password | Password changed, can log in with new password | P0 |
| TC-017 | S10 | Record consent during signup | POST `/api/v1/identity/consents` with type=data_processing | UserConsent row with IpAddress, GrantedAt, exact text | P0 |
| TC-018 | S10 | Refuse marketing consent | POST consent with type=marketing, granted=false | Consent recorded as refused; marketing disabled | P0 |
| TC-019 | S11 | Download personal data (PDPL) | GET `/api/v1/identity/my-data` | Export job queued, ZIP with JSON + PDF delivered via signed URL within SLA | P0 |
| TC-020 | S12 | Delete account (PDPL) | DELETE `/api/v1/identity/account` | Soft-delete with DeletedAt=now, Quartz hard-purge after 30d | P0 |
| TC-021 | S12 | Re-register within 30 days | Register again with same email within 30d of deletion | Option to restore account offered (Q15) | P0 |
| TC-022 | S10 | Withdraw consent | Withdraw data_processing consent after initial grant | Account flagged for deletion, 30d window applies | P0 |

### 2.2 Gyms / Onboarding (Stories 13-20)

| TC ID | Story | Test Case | Steps | Expected Result | Priority |
|-------|-------|-----------|-------|-----------------|----------|
| TC-023 | S13 | Register gym | Gym owner submits registration (name, address, contact, hours) | Appears in admin pending queue | P0 |
| TC-024 | S14 | View pending gym queue | Admin opens onboarding queue | All pending gyms listed with submitted info | P0 |
| TC-025 | S15 | Approve gym | Admin approves gym | 6-char gym code auto-generated, default plan assigned, welcome email sent | P0 |
| TC-026 | S15 | Reject gym | Admin rejects gym with reason | Gym notified, not activated | P0 |
| TC-027 | S16 | Upload gym branding | Owner uploads logo + picks colors (gym_branding gated) | Apps reflect gym branding | P1 |
| TC-028 | S17 | White-label branding | Owner on Business sets splash + app name (white_label_branding gated) | Full white-label experience, no GymApp branding | P2 |
| TC-029 | S18 | Set operating hours | Owner sets weekly hours + amenities | Members see accurate info | P1 |
| TC-030 | S18 | Set seasonal hours (Ramadan) | Owner adds "Ramadan Hours" with date range (Q32) | Seasonal hours override default during active range | P1 |
| TC-031 | S19 | Suspend gym immediately | Admin clicks Suspend with reason | Status=Suspended, all apps read-only, kiosk blocked, FCM `gym_suspended` sent | P0 |
| TC-032 | S19 | Verify suspended gym API | Any write request to suspended gym | 403 `GYM_SUSPENDED` | P0 |
| TC-033 | S20 | Terminate gym | Admin clicks Terminate (name confirmation) | Data purged after 30d PDPL window | P0 |

### 2.3 Members and Check-In (Stories 21-28)

| TC ID | Story | Test Case | Steps | Expected Result | Priority |
|-------|-------|-----------|-------|-----------------|----------|
| TC-034 | S21 | Add member | Staff adds a new member | Member created with auto-suggested member number | P0 |
| TC-035 | S22 | Member number auto-suggest | Open "Add member" form | System suggests MAX(memberNumber)+1, field is editable | P0 |
| TC-036 | S22 | Duplicate member number | Staff enters existing number | 409 `GYMS.MEMBER_NUMBER_EXISTS` | P0 |
| TC-037 | S23 | Edit member number | Staff corrects a member's number | Number updated, no conflicts | P0 |
| TC-038 | S21 | Search members | Staff searches by name, phone, number | Matching results returned | P0 |
| TC-039 | S21 | Freeze member | Staff freezes membership | Status = Frozen, member cannot check in | P0 |
| TC-040 | S21 | Reactivate member | Staff reactivates frozen member | Status = Active | P0 |
| TC-041 | S24 | Kiosk QR check-in | Scan active member's QR on kiosk | Photo + name + "Welcome!" for 5s, auto-reset, check-in row with method=QR | P0 |
| TC-042 | S24 | Kiosk member-number check-in | Type "7824" on kiosk numpad | Same visual as QR, check-in row with method=MEMBER_NUMBER | P0 |
| TC-043 | S25 | QR check-in welcome screen | Member scans QR | Photo + name displayed with "Welcome!" | P0 |
| TC-044 | S26 | Manual check-in | Staff searches by name or number | Check-in recorded with method=MANUAL | P0 |
| TC-045 | S27 | Expired membership check-in | Any check-in method for expired member | Blocked: "Membership expired -- please renew at the desk", 403 `GYMS.MEMBERSHIP_EXPIRED` | P0 |
| TC-046 | S27 | Check-in throttling | Same member checks in again within 1h | 409 `GYMS.CHECKIN_THROTTLED` | P1 |
| TC-047 | S27 | Check-in on suspended gym | Check-in attempt on suspended gym | 403 `GYM_SUSPENDED` | P0 |
| TC-048 | S28 | Today's check-in count | Owner views dashboard | Correct check-in count and current attendance shown | P1 |

### 2.4 Trainer-Client Management (Stories 29-32)

| TC ID | Story | Test Case | Steps | Expected Result | Priority |
|-------|-------|-----------|-------|-----------------|----------|
| TC-049 | S29 | Client list | Trainer opens client list | All assigned clients shown with last/next session, goal tags | P0 |
| TC-050 | S30 | Client 5-tab profile | Trainer taps a client | Overview/Progress/Sessions/Workouts/Notes tabs all load | P0 |
| TC-051 | S31 | Send PAR-Q form | Trainer sends intake form to new client | Client receives form, can fill it | P0 |
| TC-052 | S32 | Assign member to trainer | Manager assigns member to trainer | Trainer sees member in their client list | P1 |
| TC-053 | -- | Multi-trainer per member (Q2) | Member assigned to two trainers in same gym | Both trainers see the member; sessions scoped per trainer | P1 |
| TC-054 | -- | Trainer at multiple gyms (Q3) | Trainer works at Gym A and Gym B | Gym switcher works; all data scoped per gym via X-Gym-Id | P1 |

### 2.5 Workouts and Programming (Stories 33-39)

| TC ID | Story | Test Case | Steps | Expected Result | Priority |
|-------|-------|-----------|-------|-----------------|----------|
| TC-055 | S33 | Browse exercise library | Trainer searches exercises by muscle group / equipment | 200+ exercises returned, filterable | P0 |
| TC-056 | S34 | Build workout with sets/reps | Trainer creates workout, sets per exercise | Workout saved, workout_builder gated | P1 |
| TC-057 | S34 | Superset / circuit support | Trainer groups exercises as superset | Correctly ordered and grouped | P1 |
| TC-058 | S34 | Save as template | Trainer saves workout as template | Template reusable across clients | P1 |
| TC-059 | S35 | Assign workout (remote OFF) | Trainer assigns workout; RemoteWorkoutConfig.IsActive=false | Client's GET /training/today returns null; no "Today's Workout" card | P0 |
| TC-060 | S36 | Multi-week program | Trainer maps workouts across weeks (multi_week_programs gated) | Program created with weekly themes | P2 |
| TC-061 | S37 | Custom exercise (Business) | Trainer on Business adds custom exercise with video (custom_exercise_library gated) | Exercise visible only to that gym | P2 |
| TC-062 | S38 | Ad-hoc workout log | Member logs workout from exercise library (always available) | WorkoutLog created regardless of plan or remote mode | P0 |
| TC-063 | S39 | Workout history | Member views calendar / list of completed workouts | All workouts shown with date, duration, muscle groups | P0 |

### 2.6 Remote Workout Mode (Stories 40-44)

| TC ID | Story | Test Case | Steps | Expected Result | Priority |
|-------|-------|-----------|-------|-----------------|----------|
| TC-064 | S40 | Activate remote mode | Trainer POST `/training/remote-workout/activate` with clientId + reason | RemoteWorkoutConfig.IsActive=true, FCM push sent, remote_workout_mode gated | P0 |
| TC-065 | S40 | Activate on ungated plan | Trainer on Free/Starter attempts activation | 403 `PLAN.FEATURE_NOT_AVAILABLE` with requiredPlan:pro | P0 |
| TC-066 | S41 | Guided workout experience | Client with active remote mode opens app | "Today's Workout" card visible; step-by-step with demo media, sets/reps, rest timer | P0 |
| TC-067 | S42 | Upload exercise demo media | Trainer uploads images/videos for exercises | Client sees media during guided workout | P1 |
| TC-068 | S43 | Activation notification | Remote mode activated for client | FCM: "Your trainer has enabled remote workouts for you" | P0 |
| TC-069 | S43 | Deactivation notification | Remote mode deactivated | FCM: "Remote workout mode has been turned off"; card disappears | P0 |
| TC-070 | S44 | Independent per-client toggle | Activate for C1 and C2, NOT C3; deactivate C1 only | C1 loses guided mode, C2 keeps it, C3 never had it | P0 |

### 2.7 Live Session and Dual Logging (Stories 45-49)

| TC ID | Story | Test Case | Steps | Expected Result | Priority |
|-------|-------|-----------|-------|-----------------|----------|
| TC-071 | S45 | Live Session mode | Trainer starts live session, logs reps/weight per set | Session tracked with timer, exercises walk-through | P0 |
| TC-072 | S45 | Swap exercise mid-session | Trainer replaces exercise during live session | Alternative substituted, session continues | P1 |
| TC-073 | S46 | Trainer post-session log | POST `/scheduling/sessions/{id}/trainer-log` | TrainerLog set, Status=Completed, SessionCompletedIntegrationEvent | P0 |
| TC-074 | S47 | Member post-session log | Member receives notification, POST `/sessions/{id}/member-log` | MemberLog set independently | P0 |
| TC-075 | S48 | Dual log side-by-side | GET `/scheduling/sessions/{id}` | Both TrainerLog and MemberLog returned | P0 |
| TC-076 | S48 | Trainer log missing | Member submits log but trainer hasn't | "Trainer log: pending" placeholder shown | P0 |
| TC-077 | S49 | Missing trainer logs report | Owner views weekly stats | Count of sessions with missing trainer logs shown | P1 |

### 2.8 Scheduling (Stories 50-55)

| TC ID | Story | Test Case | Steps | Expected Result | Priority |
|-------|-------|-----------|-------|-----------------|----------|
| TC-078 | S50 | Set availability | Trainer sets Sun-Thu 8AM-6PM, 60min default, 15min buffer | Availability saved, slots computed | P0 |
| TC-079 | S51 | Block dates | Trainer blocks vacation dates | Affected clients auto-notified, sessions flagged | P0 |
| TC-080 | S52 | Book session | Member books available slot | Session Status=Pending, integration event emitted | P0 |
| TC-081 | S52 | Book unavailable slot | Member books overlapping or blocked slot | 409 `SCHEDULING.SLOT_UNAVAILABLE` | P0 |
| TC-082 | S52 | Book with unassigned trainer | Member books trainer they're not assigned to (Q11) | 403 or validation error | P0 |
| TC-083 | S53 | Normal cancellation (>window) | Member cancels session outside cancellation window | Normal cancellation, no deduction | P0 |
| TC-084 | S53 | Late cancellation (<window) | Member cancels within gym's cancellation_window_hours (Q5) | IsLateCancel=true, session deducted from package, trainer notified | P0 |
| TC-085 | S53 | Configurable cancel window | Gym sets cancellation_window_hours=6 (Q5) | Cancel at hour 5 = late; cancel at hour 7 = normal | P0 |
| TC-086 | S54 | Mark no-show | Trainer marks client no-show | No-show count tracked per client | P0 |
| TC-087 | S54 | No-show threshold (Q5) | Client hits configurable no-show threshold | Action triggered per gym policy (flag_only / notify_trainer / suspend_booking) | P1 |
| TC-088 | S55 | Recurring weekly session | Trainer sets recurring slot for 12 weeks | Sessions auto-created for the duration | P1 |

### 2.9 Progress (Stories 56-61)

| TC ID | Story | Test Case | Steps | Expected Result | Priority |
|-------|-------|-----------|-------|-----------------|----------|
| TC-089 | S56 | Log weight | Member logs weight | Trend chart shows 30/90/365 day view; kg/lbs toggle | P1 |
| TC-090 | S57 | Log body measurements | Member logs all 9 measurements | Per-metric chart, cm/inches toggle | P1 |
| TC-091 | S58 | Body fat Navy method | Member enters waist and neck measurements | Body fat % calculated and displayed with healthy range | P1 |
| TC-092 | S59 | Upload progress photos | Member uploads front/side/back photos | Photos stored, comparison slider works (progress_photos gated) | P1 |
| TC-093 | S60 | Photo privacy default | Member uploads first photo | Photos private by default; explicit "share with trainer" toggle | P1 |
| TC-094 | S60 | Share photos with trainer | Member enables sharing | Trainer can view; gym owner/manager CANNOT view (Q22) | P1 |
| TC-095 | S61 | Trainer logs measurements | Trainer enters measurements for client | Tagged "trainer-measured" (progress_tracking gated) | P1 |

### 2.10 Nutrition (Stories 62-65)

| TC ID | Story | Test Case | Steps | Expected Result | Priority |
|-------|-------|-----------|-------|-----------------|----------|
| TC-096 | S62 | Log meals | Member logs food from database | Calories/macros calculated, daily totals shown (nutrition_tracking gated) | P2 |
| TC-097 | S63 | Log water | Member uses quick-add buttons (250/500/custom) | Ring shows progress toward 2L default target | P2 |
| TC-098 | S64 | Create meal plan | Trainer builds 7-day meal plan with alternatives (meal_plans gated) | Plan saved and assignable | P2 |
| TC-099 | S65 | View meal plan | Member views assigned meal plan | Meals with portions, calories, macros; mark completed | P2 |

### 2.11 Communication (Stories 66-68)

| TC ID | Story | Test Case | Steps | Expected Result | Priority |
|-------|-------|-----------|-------|-----------------|----------|
| TC-100 | S66 | Push notification types | Verify all automated triggers: session reminder 24h, 1h; feedback; expiry 7d; announcements; streak | Each fires at correct time (push_notifications gated) | P1 |
| TC-101 | S66 | Quiet hours | Member sets quiet hours 11PM-7AM | No notifications during quiet hours | P1 |
| TC-102 | S66 | Per-type opt-out | Member disables specific notification type | That type no longer sent | P1 |
| TC-103 | S67 | Trainer-client chat | Trainer sends text, photo, voice | Real-time delivery to client | P1 |
| TC-104 | S67 | Broadcast to tagged group | Trainer broadcasts to all clients | All clients in group receive message | P1 |
| TC-105 | S68 | In-app notification center | Member opens notification center | Read/unread badges, all notifications listed | P1 |

### 2.12 Classes (Stories 69-71)

| TC ID | Story | Test Case | Steps | Expected Result | Priority |
|-------|-------|-----------|-------|-----------------|----------|
| TC-106 | S69 | Create class | Staff creates class (name, trainer, day/time, capacity, recurring) | Class created (class_management gated) | P1 |
| TC-107 | S70 | Book class spot | Member books available spot | Status=Booked, spot count decrements | P1 |
| TC-108 | S70 | Waitlist auto-promotion | 21st member books full class (20 capacity) | Waitlisted; when one cancels, auto-promoted + notified | P1 |
| TC-109 | S71 | Cancel class | Staff cancels class | All Booked + Waitlisted members notified | P1 |

### 2.13 Billing / Payments / Packages (Stories 72-78)

| TC ID | Story | Test Case | Steps | Expected Result | Priority |
|-------|-------|-----------|-------|-----------------|----------|
| TC-110 | S72 | Record payment | Staff records cash/card/transfer payment | Payment created, receipt number `REC-{GymCode}-{YYYYMMDD}-{SEQ}` generated (payment_recording gated) | P1 |
| TC-111 | S72 | Void payment (Q9) | Staff voids incorrect payment with reason | Status=Voided, excluded from revenue; no auto session-package restore | P1 |
| TC-112 | S73 | Revenue dashboard | Owner views dashboard | Today/week/month totals split by memberships vs PT packages | P1 |
| TC-113 | S74 | Create package template | Owner creates "10 sessions for EGP 5,000" | SessionPackageTemplate created (session_packages gated) | P1 |
| TC-114 | S74 | Trainer creates package | Trainer attempts to create package | 403 `BILLING.TRAINER_CANNOT_MANAGE_PACKAGES` | P1 |
| TC-115 | S75 | Assign package to member | Receptionist assigns template to member + links trainer | SessionPackage created with RemainingSessions = template count | P1 |
| TC-116 | S76 | Trainer views packages (read-only) | Trainer queries `/billing/packages/my-clients` | Read-only list, no edit, no price exposed | P1 |
| TC-117 | S76 | Session package decrement | Session completed | RemainingSessions--, alerts at 3 and 1 remaining | P1 |
| TC-118 | S76 | Package exhausted | RemainingSessions reaches 0 | SessionPackageExhausted event, all parties notified | P1 |
| TC-119 | S78 | Renewal reminder | Membership 7 days from expiry | Push notification sent to member | P1 |

### 2.14 Gamification (Stories 79-86)

| TC ID | Story | Test Case | Steps | Expected Result | Priority |
|-------|-------|-----------|-------|-----------------|----------|
| TC-120 | S79 | Attendance leaderboard | 3 members attend different session counts | Ranked correctly by sessions; ties broken by streak (member_leaderboards gated) | P2 |
| TC-121 | S80 | Leaderboard opt-out | Member sets ShowOnLeaderboard=false | Hidden from public board; can still see own rank privately | P2 |
| TC-122 | S80 | Leaderboard opt-in again | Member re-enables | Name reappears on public board | P2 |
| TC-123 | S81 | Rate trainer | Member submits 1-5 stars + comment after session | Rating row written (trainer_ratings gated) | P2 |
| TC-124 | S82 | Manager sees individual ratings | Manager queries `/{trainerId}/ratings` | Full list: member name, stars, comment, sessionId, date | P2 |
| TC-125 | S83 | Trainer cannot see individual ratings | Trainer queries own `/ratings` | 403 -- trainers never see individual ratings | P2 |
| TC-126 | S83 | Trainer sees aggregate only | Trainer queries `/public-profile` | Aggregate stars + Coach badges + approved testimonials only | P2 |
| TC-127 | S84 | Coach of the Month | Month-end job runs with qualified trainers | Highest normalized score wins; notification to all gym members (coach_of_the_month gated) | P2 |
| TC-128 | S84 | Coach exclusion (<3 ratings) | Trainer with 2 unique-client ratings | Excluded from Coach-of-the-Month ranking | P2 |
| TC-129 | S85 | Testimonial moderation | Manager approves/rejects comments | Approved: anonymized on public profile; Rejected: never shown (testimonial_moderation gated) | P2 |
| TC-130 | S86 | Trainer public profile | Member browses trainers | Aggregate rating + anonymized testimonials visible | P2 |

### 2.15 Analytics (Stories 87-89)

| TC ID | Story | Test Case | Steps | Expected Result | Priority |
|-------|-------|-----------|-------|-----------------|----------|
| TC-131 | S87 | Basic analytics dashboard | Owner views dashboard | Active members, new this month, revenue, check-ins (basic_analytics gated) | P1 |
| TC-132 | S88 | Advanced analytics | Owner on Pro/Business views analytics | Peak hours heatmap, retention, trainer utilization, churn alerts (advanced_analytics gated) | P2 |
| TC-133 | S89 | Churn prediction | Owner on Business views churn | Members tagged Low/Medium/High risk with actions (churn_prediction gated) | P2 |

### 2.16 Admin Platform (Stories 90-101)

| TC ID | Story | Test Case | Steps | Expected Result | Priority |
|-------|-------|-----------|-------|-----------------|----------|
| TC-134 | S90 | Master gym list | Admin opens gym list | Filterable by plan/status/city, drill-down to detail | P0 |
| TC-135 | S91 | CRUD plans | Admin creates plan with code, name, price, isPublic, isTimeLimited, dates | Plan row created | P0 |
| TC-136 | S91 | CRUD features | Admin creates/edits feature codes | Feature row managed | P0 |
| TC-137 | S91 | Toggle plan-feature matrix | Admin enables nutrition_tracking for Pro | plan_features row updated, plan_change_log appended | P0 |
| TC-138 | S92 | Per-gym feature override | Admin force-ON feature for specific gym with reason | gym_feature_overrides row created | P0 |
| TC-139 | S93 | Override storage limits | Admin overrides photo quota for VIP gym | Custom limit applied for that gym | P0 |
| TC-140 | S94 | Deploy changes | Admin clicks Deploy (scope: all/plan/gym) | Redis invalidated, FCM silent push sent, SignalR broadcast, audit log written | P0 |
| TC-141 | S94 | Deploy pending-change badge | Admin makes changes without deploying | Badge shows on Deploy button | P0 |
| TC-142 | S95 | Audit log | Admin views plan_change_log | Every change filterable by gym | P0 |
| TC-143 | S96 | Platform analytics | Admin views dashboard | Total gyms, members, MRR, growth, by-city, feature usage | P0 |
| TC-144 | S97 | Churn list (gym-level) | Gyms with no login >= 14d | At-risk gyms listed | P1 |
| TC-145 | S98 | Impersonate user | Admin impersonates a user | Sees what user sees; action logged | P1 |
| TC-146 | S99 | Support ticket inbox | Admin views tickets | Priority, assignee, conversation history | P1 |
| TC-147 | S100 | PDPL breach log | super_admin views breach log | Access restricted to super_admin only; 72h notification flag | P0 |
| TC-148 | S101 | Bulk suspend overdue gyms | Gyms past 21-day grace period | Bulk suspension applied | P1 |

### 2.17 Cross-Cutting (Stories 102-105)

| TC ID | Story | Test Case | Steps | Expected Result | Priority |
|-------|-------|-----------|-------|-----------------|----------|
| TC-149 | S102 | Toggle Arabic/English | Switch language | Full RTL when Arabic; LTR when English | P0 |
| TC-150 | S103 | Session timeout | User idle beyond threshold (Q29) | Mobile: JWT 1h; Staff Web: 30min; Admin Web: 15min; Kiosk: never | P0 |
| TC-151 | S104 | Account lockout | 5 failed logins in 15 min (Q30) | Locked 30 min; after 3 cycles: must reset via OTP | P0 |
| TC-152 | S105 | TDE + TLS | Verify encryption | Data at rest encrypted (TDE); all traffic TLS 1.3 | P0 |

---

## 3. E2E Workflow Tests

These cover all 20 scenarios from docs/04_GYMAPP_FEATURES_GUIDE.md.

### Scenario 1: Member Onboarding Flow

**Priority:** P0

| Step | Action | Expected |
|------|--------|----------|
| 1 | Register with email + password | 201 with tokens |
| 2 | Request OTP | OTP delivered to email |
| 3 | Verify OTP | User.IsVerified=true |
| 4 | Complete fitness profile | Profile saved |
| 5 | Enter 6-char gym code | GymMembership created |
| 6 | App fetches plan-features | Features cached |
| 7 | App fetches gym branding | Theme applied, gym logo displayed |

### Scenario 2: Normal Training Flow (No Remote Mode)

**Priority:** P0

| Step | Action | Expected |
|------|--------|----------|
| 1 | Trainer assigns workout to client (mode=planning) | Assignment saved, IsRemoteMode=false |
| 2 | Client opens trainee app | GET /training/today returns null |
| 3 | Client home screen | NO "Today's Workout" card |
| 4 | In-person session occurs | Trainer uses Live Session Mode |
| 5 | Trainer logs post-session | SessionCompletedIntegrationEvent emitted |
| 6 | Member receives notification | "Leave your post-session feedback" |
| 7 | Member submits dual-log | Both logs visible on session detail |

### Scenario 3: Remote Workout Mode -- Activation

**Priority:** P0

| Step | Action | Expected |
|------|--------|----------|
| 1 | Trainer goes to client profile | Remote mode toggle visible |
| 2 | Trainer toggles ON with reason "On leave until April 20" | RemoteWorkoutConfig.IsActive=true |
| 3 | Client receives FCM push | "Your trainer has enabled remote workouts for you" |
| 4 | Client opens home screen | "Today's Workout" card appears |
| 5 | Client taps "Start Workout" | Guided experience: exercises with demo media, sets/reps, rest timer |
| 6 | Client logs performance | WorkoutLog created, appears in history |

### Scenario 4: Remote Workout Mode -- Deactivation

**Priority:** P0

| Step | Action | Expected |
|------|--------|----------|
| 1 | Trainer toggles Remote Mode OFF for client | IsActive=false, DeactivatedAt=now |
| 2 | Client receives notification | "Remote workout mode has been turned off" |
| 3 | Client opens home screen | "Today's Workout" card disappears |
| 4 | Client opens exercise library | Can still log ad-hoc workouts |

### Scenario 5: Remote Workout Mode -- Multiple Clients

**Priority:** P0

| Step | Action | Expected |
|------|--------|----------|
| 1 | Trainer activates for Client A (reason: "traveling") | A gets notification, sees guided workouts |
| 2 | Trainer activates for Client B (reason: "home supplement") | B gets notification, sees guided workouts |
| 3 | Client C (not activated) opens app | NO guided workout card |
| 4 | Trainer deactivates for Client A only | A loses guided mode |
| 5 | Verify Client B | Still has guided workouts |
| 6 | Verify Client C | Still no guided workouts |

### Scenario 6: Session Dual-Logging

**Priority:** P0

| Step | Action | Expected |
|------|--------|----------|
| 1 | Trainer books session with member | Session created, Status=Pending |
| 2 | Member confirms | Status=Confirmed |
| 3 | Session time arrives | Trainer opens Live Session |
| 4 | Session completes, trainer logs feedback | TrainerLog set, Status=Completed |
| 5 | Member logs their perspective | MemberLog set |
| 6 | View session detail | Both logs side-by-side |

### Scenario 7: Payment and Package Flow

**Priority:** P1

| Step | Action | Expected |
|------|--------|----------|
| 1 | Staff creates membership plan | Plan created |
| 2 | Staff records payment | Payment with receipt number |
| 3 | Member status becomes active | Check-in allowed |
| 4 | Membership approaches expiry | 7-day reminder notification sent |
| 5 | Member renews at desk | New payment recorded, expiry extended |

### Scenario 8: Session Package Decrement

**Priority:** P1

| Step | Action | Expected |
|------|--------|----------|
| 1 | Owner creates 10-session package template | Template saved |
| 2 | Staff assigns to client + links trainer | SessionPackage with RemainingSessions=10 |
| 3 | Session completed (7 times) | RemainingSessions=3 |
| 4 | SessionPackageLow event at 3 | Trainer + member + manager notified |
| 5 | Session completed 2 more times | RemainingSessions=1 |
| 6 | SessionPackageLow event at 1 | Second alert sent |
| 7 | Final session completed | RemainingSessions=0, SessionPackageExhausted |

### Scenario 9: Class Booking and Waitlist

**Priority:** P1

| Step | Action | Expected |
|------|--------|----------|
| 1 | Staff creates class with capacity 20 | Class created |
| 2 | 20 members book | All Status=Booked, spots=0 |
| 3 | 21st member books | Status=Waitlisted |
| 4 | One booked member cancels | Spot opens |
| 5 | Waitlisted member auto-promoted | Status flips to Booked, notification sent |

### Scenario 10: Progress Photo Comparison

**Priority:** P1

| Step | Action | Expected |
|------|--------|----------|
| 1 | Member uploads front photo in January | Photo stored, private by default |
| 2 | Member uploads front photo in March | Second photo stored |
| 3 | Member opens comparison slider | Side-by-side of Jan vs Mar |

### Scenario 11: Check-In Kiosk

**Priority:** P0

| Step | Action | Expected |
|------|--------|----------|
| 1 | Tablet runs in kiosk mode | Full-screen with gym logo, QR + numpad side-by-side |
| 2 | Member scans QR | Photo + name + "Welcome!" shown |
| 3 | Auto-reset after 5 seconds | Kiosk returns to default |
| 4 | Member types number on numpad | Same result as QR |
| 5 | Expired member attempts check-in | Blocked: "Membership expired" |

### Scenario 12: Gym Onboarding (Admin)

**Priority:** P0

| Step | Action | Expected |
|------|--------|----------|
| 1 | Gym owner registers | Appears in admin pending queue |
| 2 | Admin reviews and approves | 6-char gym code generated |
| 3 | Default plan assigned | Free plan by default (Q6) |
| 4 | Welcome email sent | Gym owner receives email |
| 5 | Gym appears as active | Listed in master gym list |

### Scenario 13: Churn Detection

**Priority:** P1

| Step | Action | Expected |
|------|--------|----------|
| 1 | Gym stops logging in | Activity tracked |
| 2 | 14 days pass with no login | Gym appears in churn analytics |
| 3 | Admin sends re-engagement email | Email delivered to gym owner |

### Scenario 14: Member Leaderboard -- Attendance

**Priority:** P2

| Step | Action | Expected |
|------|--------|----------|
| 1 | Member A attends 20 sessions | Score = 20 |
| 2 | Member B attends 15 sessions | Score = 15 |
| 3 | Member C attends 10 sessions | Score = 10 |
| 4 | View leaderboard | A > B > C ranking |

### Scenario 15: Member Leaderboard -- Opt Out

**Priority:** P2

| Step | Action | Expected |
|------|--------|----------|
| 1 | Member toggles ShowOnLeaderboard=false | Privacy setting saved |
| 2 | Other members view leaderboard | Opted-out member NOT listed |
| 3 | Opted-out member views own position | Can see own rank privately |
| 4 | Member toggles back ON | Name reappears on public board |

### Scenario 16: Weight Loss Leaderboard -- Fair Ranking

**Priority:** P2

| Step | Action | Expected |
|------|--------|----------|
| 1 | Member A: 80kg start, 78kg end (2.5% lost) | Percentage calculated |
| 2 | Member B: 100kg start, 98kg end (2.0% lost) | Percentage calculated |
| 3 | View leaderboard | A ranks above B (percentage-based) |

### Scenario 17: Trainer Rating -- Privacy

**Priority:** P2

| Step | Action | Expected |
|------|--------|----------|
| 1 | Member rates trainer 3 stars with comment | Rating row written |
| 2 | Trainer opens their app | Aggregate rating updated; individual review NOT visible |
| 3 | Gym manager opens trainer ratings page | Full review: client name, stars, comment, date |

### Scenario 18: Coach of the Month

**Priority:** P2

| Step | Action | Expected |
|------|--------|----------|
| 1 | Month ends | Quartz job calculates normalized scores |
| 2 | Highest score trainer identified | Coach of the Month badge assigned |
| 3 | All gym members notified | FCM: "Coach of the Month announced!" |
| 4 | Badge appears on trainer's public profile | Visible to all members |

### Scenario 19: Testimonial Moderation

**Priority:** P2

| Step | Action | Expected |
|------|--------|----------|
| 1 | Client submits "Amazing trainer!" | Appears in manager's moderation queue |
| 2 | Manager approves | Anonymized comment on trainer's public profile |
| 3 | Another client submits inappropriate comment | Appears in queue |
| 4 | Manager rejects | Never appears publicly |

### Scenario 20: Trainer Rating Normalization

**Priority:** P2

| Step | Action | Expected |
|------|--------|----------|
| 1 | Trainer A: 30 clients, avg 4.2 stars | Score calculated with formula |
| 2 | Trainer B: 5 clients, avg 4.8 stars | Score calculated with formula |
| 3 | Compare scores | Normalization prevents Trainer B from dominating unfairly with few clients |
| 4 | Trainer with <3 unique ratings | Excluded from Coach-of-the-Month |

---

## 4. Plan-Based Feature Gating Tests

### 4.1 Feature Availability Matrix

Test every feature code against every plan. For each cell marked "disabled", verify 403 `PLAN.FEATURE_NOT_AVAILABLE`. For each cell marked "enabled", verify the feature works.

| Feature Code | Free | Starter | Pro | Business |
|---|---|---|---|---|
| `member_management` | enabled | enabled | enabled | enabled |
| `qr_checkin` | enabled | enabled | enabled | enabled |
| `exercise_library_global` | enabled | enabled | enabled | enabled |
| `basic_reporting` | enabled | enabled | enabled | enabled |
| `unlimited_trainers` | disabled | enabled | enabled | enabled |
| `workout_builder` | disabled | enabled | enabled | enabled |
| `session_logging` | disabled | enabled | enabled | enabled |
| `progress_tracking` | disabled | enabled | enabled | enabled |
| `progress_photos` | disabled | enabled | enabled | enabled |
| `class_management` | disabled | enabled | enabled | enabled |
| `push_notifications` | disabled | enabled | enabled | enabled |
| `payment_recording` | disabled | enabled | enabled | enabled |
| `session_packages` | disabled | enabled | enabled | enabled |
| `basic_analytics` | disabled | enabled | enabled | enabled |
| `multi_staff` | disabled | enabled | enabled | enabled |
| `gym_branding` | disabled | disabled | enabled | enabled |
| `nutrition_tracking` | disabled | disabled | enabled | enabled |
| `meal_plans` | disabled | disabled | enabled | enabled |
| `member_leaderboards` | disabled | disabled | enabled | enabled |
| `trainer_ratings` | disabled | disabled | enabled | enabled |
| `coach_of_the_month` | disabled | disabled | enabled | enabled |
| `testimonial_moderation` | disabled | disabled | enabled | enabled |
| `advanced_analytics` | disabled | disabled | enabled | enabled |
| `remote_workout_mode` | disabled | disabled | enabled | enabled |
| `multi_week_programs` | disabled | disabled | enabled | enabled |
| `white_label_branding` | disabled | disabled | disabled | enabled |
| `api_access` | disabled | disabled | disabled | enabled |
| `custom_exercise_library` | disabled | disabled | disabled | enabled |
| `custom_permissions` | disabled | disabled | disabled | enabled |
| `pdf_reports` | disabled | disabled | disabled | enabled |
| `churn_prediction` | disabled | disabled | disabled | enabled |

### 4.2 Plan-Gating Test Cases

| TC ID | Test Case | Steps | Expected Result | Priority |
|-------|-----------|-------|-----------------|----------|
| TC-200 | Feature OFF returns 403 | Gym on Free calls gated endpoint | 403 with `{feature, currentPlan, requiredPlan, upgradeUrl}` | P0 |
| TC-201 | Feature ON returns success | Gym on Pro calls Pro-gated endpoint | 200/201 with data | P0 |
| TC-202 | Override force-ON | Admin force-ON feature for Free gym, Deploy | Gym accesses feature despite plan | P0 |
| TC-203 | Override force-OFF | Admin force-OFF feature for Pro gym, Deploy | Gym blocked from normally-available feature | P0 |
| TC-204 | Override removed, fallback | Admin removes override, Deploy | Resolution falls back to plan default | P0 |
| TC-205 | Mobile lock icon | Disabled feature in trainee app | Lock icon + upgrade prompt with plan name + price | P1 |
| TC-206 | Web lock display | Disabled feature in staff web | Route renders upgrade card; sidebar shows lock | P1 |
| TC-207 | Deploy unlocks instantly | Admin enables feature + Deploy | Mobile: FCM -> re-fetch -> unlock without re-login; Web: SignalR -> invalidate -> re-render | P0 |
| TC-208 | Deploy locks instantly | Admin disables feature + Deploy | Previously unlocked widgets re-lock; in-flight writes return 403 | P0 |
| TC-209 | Time-limited plan | Plan with availableFrom/availableUntil window | Visible on signup only within window; gyms already on it keep access | P1 |
| TC-210 | Hidden plan | Plan with isPublic=false | Not visible on signup; existing gyms keep access | P1 |

### 4.3 Limit-Based Gating Tests

| TC ID | Resource | Plan | Limit | Test | Expected | Priority |
|-------|----------|------|-------|------|----------|----------|
| TC-220 | Members | Free | 25 | Add 26th member | 400 `PLAN.LIMIT_EXCEEDED {currentUsage:25, newLimit:25}` | P0 |
| TC-221 | Members | Starter | 75 | Add 76th member | 400 `PLAN.LIMIT_EXCEEDED` | P0 |
| TC-222 | Trainers | Free | 1 | Add 2nd trainer | 400 `PLAN.LIMIT_EXCEEDED` | P0 |
| TC-223 | Trainers | Starter | unlimited | Add 50th trainer | Success | P0 |
| TC-224 | Staff | Starter | 3 | Add 4th staff | 400 `PLAN.LIMIT_EXCEEDED` | P1 |
| TC-225 | Photos/member | Free | 5 | Upload 6th photo | 400 `PLAN.STORAGE_LIMIT_REACHED` | P1 |
| TC-226 | Photos/member | Starter | 20 | Upload 21st photo | 400 `PLAN.STORAGE_LIMIT_REACHED` | P1 |
| TC-227 | Total storage | Free | 100MB | Upload when at 99MB, file is 2MB | 400 `PLAN.STORAGE_LIMIT_REACHED` | P1 |
| TC-228 | Total storage | Business | 10GB | Upload at 9.9GB | Success | P1 |
| TC-229 | Custom exercises | Business | 50 | Add 51st custom exercise | 400 `PLAN.LIMIT_EXCEEDED` | P2 |
| TC-230 | API rate limit | Business | 60/min | Send 61st request in 1 min | 429 rate limited | P1 |

### 4.4 Plan Upgrade/Downgrade Tests

| TC ID | Test Case | Expected | Priority |
|-------|-----------|----------|----------|
| TC-240 | Upgrade Free -> Starter | All Starter features unlock after Deploy | P0 |
| TC-241 | Upgrade Starter -> Pro | All Pro features unlock after Deploy | P0 |
| TC-242 | Downgrade Pro -> Starter (under limit) | Pro-only features lock; data preserved but unreadable | P0 |
| TC-243 | Downgrade Pro -> Starter (over limit) | Downgrade succeeds; new writes blocked; "over limit" warning in admin | P0 |
| TC-244 | Downgrade with 180 members (limit 75) | No members deleted; new adds blocked until under 75 | P0 |

---

## 5. Role-Based Access Tests

### 5.1 Role Permission Matrix

For each endpoint group, verify that unauthorized roles receive 401 (unauthenticated) or 403 (wrong role).

| Endpoint Group | trainee | trainer | receptionist | manager | owner | support | analyst | admin | super_admin |
|---|---|---|---|---|---|---|---|---|---|
| Register / login | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes |
| Own profile CRUD | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes |
| Link gym | Yes | Yes | -- | -- | -- | -- | -- | -- | -- |
| Browse exercises | Yes | Yes | -- | -- | -- | -- | -- | -- | -- |
| Log own workout | Yes | -- | -- | -- | -- | -- | -- | -- | -- |
| Book session | Yes | -- | -- | -- | -- | -- | -- | -- | -- |
| Submit member-log | Yes | -- | -- | -- | -- | -- | -- | -- | -- |
| View leaderboards | Yes | -- | -- | -- | -- | -- | -- | -- | -- |
| PDPL my-data/delete | Yes | Yes | Yes | Yes | Yes | -- | -- | -- | -- |
| Client management | -- | Yes | -- | -- | -- | -- | -- | -- | -- |
| Workout builder | -- | Yes | -- | -- | -- | -- | -- | -- | -- |
| Remote mode toggle | -- | Yes | -- | -- | -- | -- | -- | -- | -- |
| Live session | -- | Yes | -- | -- | -- | -- | -- | -- | -- |
| Trainer-log | -- | Yes | -- | -- | -- | -- | -- | -- | -- |
| Set availability | -- | Yes | -- | -- | -- | -- | -- | -- | -- |
| Check-in members | -- | -- | Yes | Yes | Yes | -- | -- | -- | -- |
| View member profiles | -- | -- | Yes | Yes | Yes | -- | -- | -- | -- |
| Record payments | -- | -- | Yes | Yes | Yes | -- | -- | -- | -- |
| Add/edit members | -- | -- | Yes | Yes | Yes | -- | -- | -- | -- |
| Approve trainers | -- | -- | -- | Yes | Yes | -- | -- | -- | -- |
| Manage classes | -- | -- | -- | Yes | Yes | -- | -- | -- | -- |
| Send announcements | -- | -- | -- | Yes | Yes | -- | -- | -- | -- |
| View analytics/revenue | -- | -- | -- | Yes | Yes | -- | -- | -- | -- |
| Manage staff | -- | -- | -- | Yes | Yes | -- | -- | -- | -- |
| Edit gym settings | -- | -- | -- | Yes | Yes | -- | -- | -- | -- |
| Manage session packages | -- | -- | -- | Yes | Yes | -- | -- | -- | -- |
| Change subscription | -- | -- | -- | -- | Yes | -- | -- | -- | -- |
| View individual trainer ratings | -- | -- | -- | Yes | Yes | -- | -- | -- | -- |
| Admin gym list/detail | -- | -- | -- | -- | -- | Yes | Yes | Yes | Yes |
| CRUD plans/features/matrix | -- | -- | -- | -- | -- | -- | -- | Yes | Yes |
| Per-gym override | -- | -- | -- | -- | -- | -- | -- | Yes | Yes |
| Deploy changes | -- | -- | -- | -- | -- | -- | -- | Yes | Yes |
| Suspend/reactivate gym | -- | -- | -- | -- | -- | -- | -- | Yes | Yes |
| Manage admin users | -- | -- | -- | -- | -- | -- | -- | -- | Yes |
| PDPL breach log | -- | -- | -- | -- | -- | -- | -- | -- | Yes |
| Platform settings | -- | -- | -- | -- | -- | -- | -- | -- | Yes |
| Impersonate user | -- | -- | -- | -- | -- | -- | -- | Yes | Yes |

### 5.2 Role-Based Test Cases

| TC ID | Test Case | Steps | Expected | Priority |
|-------|-----------|-------|----------|----------|
| TC-300 | Unauthenticated request | Call any protected endpoint without token | 401 Unauthorized | P0 |
| TC-301 | Expired JWT | Call endpoint with expired token | 401 Unauthorized | P0 |
| TC-302 | Trainee accesses admin endpoint | Trainee calls `/api/v1/admin/*` | 403 Forbidden | P0 |
| TC-303 | Trainer creates package | Trainer calls POST package template | 403 `BILLING.TRAINER_CANNOT_MANAGE_PACKAGES` | P0 |
| TC-304 | Receptionist views analytics | Receptionist calls analytics endpoint | 403 Forbidden | P1 |
| TC-305 | Support role manages plans | support calls CRUD plans | 403 Forbidden | P0 |
| TC-306 | Analyst manages plans | analyst calls CRUD plans | 403 Forbidden | P0 |
| TC-307 | Trainee views other member | Trainee calls another member's profile | 403 Forbidden | P0 |
| TC-308 | Trainer views other trainer's clients | Trainer calls clients of another trainer | 403 Forbidden | P0 |
| TC-309 | Manager views trainer individual ratings | Manager queries trainer ratings | 200 with full list | P1 |
| TC-310 | Trainer views own individual ratings | Trainer queries own individual ratings | 403 Forbidden | P1 |
| TC-311 | Admin accesses breach log (not super_admin) | admin role queries PDPL breach log | 403 Forbidden | P0 |
| TC-312 | Custom permissions (Business) | Owner on Business customizes receptionist role | Custom matrix applied | P2 |
| TC-313 | Gym data isolation | Staff of Gym A calls data of Gym B | 403 Forbidden or empty result | P0 |
| TC-314 | X-Gym-Id header validation | Trainer at Gym A sends X-Gym-Id for Gym B (not member) | 403 Forbidden | P0 |

---

## 6. Admin Deploy Flow Tests

### 6.1 End-to-End Deploy Flow

| TC ID | Test Case | Steps | Expected | Priority |
|-------|-----------|-------|----------|----------|
| TC-400 | Deploy global scope | Admin changes plan-feature matrix, clicks Deploy (scope=all) | (1) Redis invalidated for all gyms, (2) FCM silent push to all apps, (3) SignalR broadcast, (4) audit log written, (5) toast: "Deployed to N gyms" | P0 |
| TC-401 | Deploy plan scope | Admin changes features for Pro plan, Deploy scope=plan/pro | Only Pro gyms affected; Free/Starter/Business unchanged | P0 |
| TC-402 | Deploy gym scope | Admin overrides feature for single gym, Deploy scope=gym/{id} | Only that gym affected | P0 |
| TC-403 | Mobile receives config_refresh | FCM data-only message {type:config_refresh} | App calls planFeaturesRepository.refresh(), Riverpod state updates, UI rebuilds | P0 |
| TC-404 | Web receives config_updated | SignalR config_updated event | TanStack Query invalidates ['plan-features'], component re-renders | P0 |
| TC-405 | Feature unlock without re-login | Admin enables nutrition_tracking for Starter, Deploy | Trainee app's locked nutrition tab unlocks within seconds | P0 |
| TC-406 | Feature lock without re-login | Admin disables feature, Deploy | Previously unlocked tab re-locks; in-progress API call returns 403 | P0 |
| TC-407 | Deploy audit trail | Admin deploys multiple times | Each deployment has audit row with: who, when, scope, affected gyms, changes | P0 |
| TC-408 | Pending changes badge | Admin makes 3 changes without deploying | Deploy button shows badge (3 pending) | P1 |

### 6.2 Suspension Deploy

| TC ID | Test Case | Steps | Expected | Priority |
|-------|-----------|-------|----------|----------|
| TC-410 | Suspend gym | Admin suspends gym | FCM `gym_suspended` to all users, full-screen overlay on all apps, kiosk blocked | P0 |
| TC-411 | Suspended gym API | Any write to suspended gym | 403 `GYM_SUSPENDED` | P0 |
| TC-412 | Reactivate gym | Admin reactivates after payment | FCM `gym_reactivated`, overlay removed, API restored | P0 |
| TC-413 | Non-payment suspension flow | Overdue -> 7d warning -> 14d grace -> 21d suspended | Correct timeline, banners, and eventual suspension | P0 |

---

## 7. Edge Cases

From PRODUCT_SPEC.md Section 6 and FOUNDER_DECISIONS.md.

### 7.1 Plan and Feature Edge Cases

| TC ID | Edge Case | Test | Expected | Priority |
|-------|-----------|------|----------|----------|
| TC-500 | Plan downgrade past member limit (6.1) | Gym on Pro (180 members) downgraded to Starter (75 limit) | Downgrade succeeds; no members deleted; new adds blocked until under 75; admin sees "over limit" warning | P0 |
| TC-501 | Feature disabled while in use (6.2) | Trainer mid-workout-build; admin disables workout_builder + Deploy | In-flight save succeeds (idempotent); subsequent calls return 403 | P0 |
| TC-502 | Feature override removed (6.3) | Gym has override ON; admin removes override + Deploy | Falls back to plan default (OFF); nutrition tab re-locks; data preserved but unreadable; PDPL export still works | P0 |
| TC-503 | Storage limit reached (6.7) | Free plan member at 5/5 photos, attempts upload | 400 `PLAN.STORAGE_LIMIT_REACHED` with upgrade prompt; no silent overwrite | P0 |

### 7.2 Scheduling Edge Cases

| TC ID | Edge Case | Test | Expected | Priority |
|-------|-----------|------|----------|----------|
| TC-510 | Late cancel with configurable window (6.5/Q5) | Gym sets cancellation_window_hours=6; member cancels at hour 5 | IsLateCancel=true, session deducted from package | P0 |
| TC-511 | Late cancel package deduction | Late cancel on member with 5 remaining sessions | RemainingSessions decremented to 4 | P0 |
| TC-512 | No-show configurable threshold (6.14/Q5) | Gym sets 3 no-shows in 30 days -> suspend_booking | After 3rd no-show, member booking suspended | P1 |
| TC-513 | Trainer max sessions/day warning (Q12) | Trainer has 8 sessions scheduled | UI shows warning; at 10, confirmation dialog; trainer can override | P1 |
| TC-514 | Booking unassigned trainer (Q11) | Member attempts to book unassigned trainer | Blocked -- booking requires active assignment | P0 |

### 7.3 Remote Workout Mode Edge Cases

| TC ID | Edge Case | Test | Expected | Priority |
|-------|-----------|------|----------|----------|
| TC-520 | Remote mode OFF by default (6.6) | New trainer-client pair | RemoteWorkoutConfig.IsActive=false; GET /training/today returns null | P0 |
| TC-521 | Remote mode on ungated plan | Free/Starter trainer activates remote mode | 403 `PLAN.FEATURE_NOT_AVAILABLE` | P0 |

### 7.4 Gamification Edge Cases

| TC ID | Edge Case | Test | Expected | Priority |
|-------|-----------|------|----------|----------|
| TC-530 | <3 ratings, no Coach badge (6.4) | Trainer with 2 unique-client ratings at month-end | Excluded from Coach-of-the-Month ranking; aggregate still shown; "More ratings needed" in UI | P2 |
| TC-531 | Weight-loss % ranking | A (80->78, 2.5%) vs B (100->98, 2.0%) | A ranks above B | P2 |
| TC-532 | Leaderboard opt-out (6.8) | Member opts out | Hidden from public; own rank visible privately | P2 |

### 7.5 Identity / PDPL Edge Cases

| TC ID | Edge Case | Test | Expected | Priority |
|-------|-----------|------|----------|----------|
| TC-540 | Re-register after deletion (Q15) | User re-registers same email within 30d | Option to restore account; if restored, data recovered intact | P0 |
| TC-541 | Minor registration (Q16) | 16-year-old registers | Allowed | P0 |
| TC-542 | Minor 13-15 registration | 14-year-old registers | Requires parental consent | P0 |
| TC-543 | Under-13 registration | 12-year-old registers | Blocked entirely | P0 |
| TC-544 | Consent withdrawal (6.9) | Member withdraws data_processing consent | Account flagged for deletion; 30d window; anonymous aggregates survive | P0 |
| TC-545 | Session timeout (6.15/Q29) | Idle on Staff Web for 31 minutes | Session expired, forced to re-login | P0 |
| TC-546 | Account lockout (6.15/Q30) | 5 failed logins in 15 min | Locked 30 min; after 3 cycles -> must OTP reset | P0 |

### 7.6 File / Upload Edge Cases

| TC ID | Edge Case | Test | Expected | Priority |
|-------|-----------|------|----------|----------|
| TC-550 | File over 10MB (Q27) | Upload 15MB image | Client-side rejection before upload | P1 |
| TC-551 | Image compression | Upload valid large image | Server resizes to long-edge <=2048px, JPEG q80, EXIF stripped | P1 |
| TC-552 | Profile photo compression | Upload profile photo | Resized to <=512px, q80 | P1 |

### 7.7 Multi-Entity Edge Cases

| TC ID | Edge Case | Test | Expected | Priority |
|-------|-----------|------|----------|----------|
| TC-560 | Member at multiple gyms (6.12) | Trainee in Gym A and B; switch context | Branding, classes, trainers swap; plan-features per gym | P0 |
| TC-561 | Trainer at multiple gyms (6.19/Q3) | Trainer at Gym A and B; switch context | All data scoped per selected gym via X-Gym-Id | P0 |
| TC-562 | Multi-trainer per member (6.18/Q2) | Member with 2 trainers | Each session/assignment/remote-config scoped per trainer; no "primary trainer" | P1 |
| TC-563 | Trainer leaves gym (6.13/Q4) | Trainer removed from gym | Clients auto-unassigned; templates invisible (clonable before removal); history preserved | P1 |

### 7.8 Gym Section Edge Cases

| TC ID | Edge Case | Test | Expected | Priority |
|-------|-----------|------|----------|----------|
| TC-570 | Gym sections (Q31) | Gym enables Mens + Womens sections | Member section validated against enabled sections; trainer assignment checks compatibility | P1 |
| TC-571 | Seasonal hours override (Q32) | Active Ramadan schedule | Operating hours resolved: seasonal > default | P1 |

### 7.9 Billing Edge Cases

| TC ID | Edge Case | Test | Expected | Priority |
|-------|-----------|------|----------|----------|
| TC-580 | Void payment (6.11/Q9) | Staff voids payment | Status=Voided; excluded from revenue; NO auto session-package restore | P1 |
| TC-581 | No refunds in MVP | Any attempt to refund | Not supported; only void | P1 |
| TC-582 | Non-payment grace period (Q7) | Gym overdue -> 7d warning -> 14d grace -> 21d suspension | Timeline correct; admin manual reactivation only | P0 |

### 7.10 Offline Mode Edge Cases

| TC ID | Edge Case | Test | Expected | Priority |
|-------|-----------|------|----------|----------|
| TC-590 | Offline write queue (Q26) | Trainer logs session in airplane mode | Queued locally; replayed on reconnection | P1 |
| TC-591 | Offline conflict resolution | Two writes to same record offline | Last-writer-wins by record ID | P1 |
| TC-592 | Offline queue retention | Data queued >7 days | Expired entries purged | P2 |
| TC-593 | Kiosk offline | Kiosk loses network | Kiosk does NOT support offline; shows error | P0 |

---

## 8. PDPL Compliance Tests

### 8.1 Consent Recording and Tracking

| TC ID | Test Case | Steps | Expected | Priority |
|-------|-----------|-------|----------|----------|
| TC-600 | Record data_processing consent | POST consent with type, granted, consentText | Row with IpAddress, GrantedAt, exact text | P0 |
| TC-601 | Record marketing consent (refused) | POST consent type=marketing, granted=false | Consent recorded; marketing disabled | P0 |
| TC-602 | Record photo_sharing consent | First photo upload triggers confirmation dialog (Q25) | One-time consent prompt; consent recorded | P0 |
| TC-603 | View active consents | User views Settings -> Privacy | All consent types shown with status | P0 |
| TC-604 | Withdraw consent | User withdraws data_processing consent | Account flagged for deletion; 30d soft-delete | P0 |
| TC-605 | Consent required for writes | User without data_processing consent attempts write | Blocked until consent recorded | P0 |
| TC-606 | Parental consent (13-15) | Minor 14 years old registers | Parental consent flow required (Q16) | P0 |

### 8.2 Data Export

| TC ID | Test Case | Steps | Expected | Priority |
|-------|-----------|-------|----------|----------|
| TC-610 | Export all data | GET `/api/v1/identity/my-data` | Quartz job queued; ZIP with JSON archive + summary PDF + photos as JPEG | P0 |
| TC-611 | Export includes all modules | Verify export contents | Identity, Gyms, Training, Progress, Scheduling, Nutrition, Communication, Billing, Gamification data | P0 |
| TC-612 | Export ignores storage limits (6.22) | Member on Free with limited photos | Export returns ALL photos regardless of plan limit | P0 |
| TC-613 | Signed download URL | Export job completes | Email with signed URL; URL has 24h TTL | P0 |
| TC-614 | Export SLA | Request export | Delivered within SLA (24h per Q24) | P0 |
| TC-615 | Export audit logging | Export requested | platform_audit_log row written | P0 |

### 8.3 Account Deletion

| TC ID | Test Case | Steps | Expected | Priority |
|-------|-----------|-------|----------|----------|
| TC-620 | Soft delete account | DELETE `/api/v1/identity/account` | User soft-deleted with DeletedAt=now | P0 |
| TC-621 | 30-day retention | Deleted account after 30 days | Quartz job hard-purges all sensitive data | P0 |
| TC-622 | Restore within 30 days | Re-register same email within window | Restore option offered; data recovered via OTP (Q15) | P0 |
| TC-623 | Hard purge completeness | After 30d, check all module data | All PII removed; anonymous aggregates (member count, revenue) survive | P0 |
| TC-624 | Gym termination purge | Admin terminates gym | All data purged 30d after Terminate (not after suspension) -- Q7 | P0 |
| TC-625 | Suspended gym data preserved | Gym suspended for 60 days | Data intact; preserved indefinitely during suspension | P0 |

### 8.4 Audit Logging

| TC ID | Test Case | Steps | Expected | Priority |
|-------|-----------|-------|----------|----------|
| TC-630 | Sensitive data access logged | Admin accesses progress photos or health data | Row in platform_audit_log with who, when, what | P0 |
| TC-631 | PDPL breach log (super_admin only) | Non-super_admin tries to access | 403 Forbidden | P0 |
| TC-632 | 72h notification flag | PDPL breach logged | Mandatory 72h notification flag present | P0 |
| TC-633 | Export request logged | User requests data export | Audit row written | P0 |
| TC-634 | Deletion request logged | User requests account deletion | Audit row written | P0 |

### 8.5 Data Visibility Restrictions

| TC ID | Test Case | Steps | Expected | Priority |
|-------|-----------|-------|----------|----------|
| TC-640 | Workout log visibility (Q21) | Manager queries member workout detail | Only aggregate counts visible; NOT exercise/set/rep detail | P0 |
| TC-641 | Progress photo visibility (Q22) | Manager queries member photos | 403 -- only shared trainer can see | P0 |
| TC-642 | Chat content visibility (Q23) | Manager queries chat content | Only metadata (exists, count, last active); NOT content | P0 |
| TC-643 | Member-to-member privacy (Q20) | Member queries another member | Invisible; only see each other on opt-in leaderboards | P0 |

---

## 9. Storage Limit Tests

### 9.1 Per-Plan Storage Limits

| TC ID | Resource | Plan | Limit | Test | Expected | Priority |
|-------|----------|------|-------|------|----------|----------|
| TC-700 | Progress photos / member | Free | 5 | Upload photo at limit | 400 `PLAN.STORAGE_LIMIT_REACHED {resource:"progress_photos", limit:5}` | P0 |
| TC-701 | Progress photos / member | Starter | 20 | Upload photo at limit | 400 `PLAN.STORAGE_LIMIT_REACHED` | P0 |
| TC-702 | Progress photos / member | Pro | 50 | Upload at 49 | Success (under limit) | P1 |
| TC-703 | Progress photos / member | Business | 200 | Upload at 199 | Success | P1 |
| TC-704 | Total file storage / gym | Free | 100MB | Upload at 99MB, file=2MB | 400 `PLAN.STORAGE_LIMIT_REACHED` | P0 |
| TC-705 | Total file storage / gym | Starter | 500MB | Upload at 499MB | Success | P1 |
| TC-706 | Total file storage / gym | Pro | 2GB | Upload at limit | 400 | P1 |
| TC-707 | Total file storage / gym | Business | 10GB | Upload at 9.9GB | Success | P1 |
| TC-708 | Custom exercise videos | Business | 50 (500MB) | Upload 51st | 400 `PLAN.LIMIT_EXCEEDED` | P2 |

### 9.2 Storage Override Tests

| TC ID | Test Case | Steps | Expected | Priority |
|-------|-----------|-------|----------|----------|
| TC-710 | Admin overrides photo limit | Admin sets custom limit for VIP gym | Gym can upload beyond plan limit up to override | P0 |
| TC-711 | Admin overrides storage limit | Admin gives extra storage to gym | New total limit applied | P0 |
| TC-712 | Override removed | Admin removes override | Falls back to plan default; existing files preserved | P0 |

### 9.3 Retention Limits

| TC ID | Resource | Free | Starter | Pro | Business | Priority |
|-------|----------|------|---------|-----|----------|----------|
| TC-720 | Check-in retention | 30d | 6mo | 1y | 2y | P1 |
| TC-721 | Workout-log retention | 30d | 1y | 2y | 5y | P1 |
| TC-722 | Analytics retention | 7d | 3mo | 1y | 2y | P1 |

Verify Quartz purge jobs respect per-plan retention periods and do not delete data within the window.

---

## 10. Cross-App Consistency

### 10.1 Data Consistency Tests

| TC ID | Test Case | Apps Involved | Steps | Expected | Priority |
|-------|-----------|---------------|-------|----------|----------|
| TC-800 | Session data matches | Trainee + Trainer | Trainer logs session; member views | Same session data displayed in both apps | P0 |
| TC-801 | Dual-log visible in both | Trainee + Trainer | Both logs submitted | Each app shows both logs on session detail | P0 |
| TC-802 | Check-in visible | Trainee + Staff | Member checks in via kiosk | Staff app shows check-in; member sees attendance history | P0 |
| TC-803 | Payment reflects everywhere | Staff Mobile + Staff Web | Payment recorded on mobile | Visible on web revenue dashboard | P1 |
| TC-804 | Class booking syncs | Trainee + Staff | Member books class | Staff sees booking count; member sees confirmation | P1 |
| TC-805 | Remote mode reflects | Trainer + Trainee | Trainer activates remote mode | Trainee app shows guided workout card | P0 |
| TC-806 | Announcement reaches all | Staff + Trainee | Staff publishes announcement | All members see it; push notification delivered | P1 |
| TC-807 | Plan features consistent | All apps | Admin enables feature + Deploy | Mobile, Staff Web, Trainee App all reflect change | P0 |
| TC-808 | Gym branding consistent | Trainee + Trainer | Owner sets branding | Both apps show gym logo + colors | P1 |
| TC-809 | Suspension overlay | All apps | Admin suspends gym | Trainee, Trainer, Staff all show suspension overlay | P0 |

### 10.2 Real-Time Sync Tests

| TC ID | Test Case | Mechanism | Expected | Priority |
|-------|-----------|-----------|----------|----------|
| TC-810 | Config refresh (mobile) | FCM silent push | Plan features refreshed without re-login | P0 |
| TC-811 | Config refresh (web) | SignalR config_updated | TanStack Query invalidated, UI re-renders | P0 |
| TC-812 | Chat real-time | SignalR / FCM | Messages appear in real-time on both sides | P1 |
| TC-813 | Notification delivery | FCM push | Notification appears on device within seconds | P1 |

---

## 11. Arabic/RTL Tests

### 11.1 Language and Layout Tests

| TC ID | Test Case | Steps | Expected | Priority |
|-------|-----------|-------|----------|----------|
| TC-900 | Arabic default | New user registers | Arabic is default language (Q34) | P0 |
| TC-901 | RTL layout (Arabic) | Switch to Arabic | Full RTL: text alignment, navigation, icons, lists | P0 |
| TC-902 | LTR layout (English) | Switch to English | Standard LTR layout | P0 |
| TC-903 | Language toggle | Switch Arabic -> English and back | Layout flips correctly; all text translated | P0 |
| TC-904 | Arabic text rendering | View Arabic content | Correct rendering: no truncation, no overflow, proper line breaks | P0 |
| TC-905 | Mixed Arabic/English text | Names in English, UI in Arabic | Bidirectional text renders correctly | P1 |

### 11.2 Currency Formatting

| TC ID | Test Case | Steps | Expected | Priority |
|-------|-----------|-------|----------|----------|
| TC-910 | EGP in Arabic | View price in Arabic UI | Displayed as `EGP` amount format or `ج.م` (Q34c) | P0 |
| TC-911 | EGP in English | View price in English UI | Displayed as `EGP` prefix | P0 |
| TC-912 | Receipt in Arabic | Generate receipt in Arabic locale | Correct formatting, RTL layout | P1 |

### 11.3 Unit Preferences

| TC ID | Test Case | Steps | Expected | Priority |
|-------|-----------|-------|----------|----------|
| TC-920 | kg/lbs toggle | Switch weight units | All weights displayed in selected unit | P1 |
| TC-921 | cm/inches toggle | Switch measurement units | All measurements in selected unit | P1 |

### 11.4 Egyptian Food Database

| TC ID | Test Case | Steps | Expected | Priority |
|-------|-----------|-------|----------|----------|
| TC-930 | Search Egyptian foods | Search for "koshari", "ful", "grilled chicken" | Found in food database with Arabic names and macros | P2 |
| TC-931 | Arabic food names | View food database in Arabic | All 50+ Egyptian food items shown with Arabic names | P2 |

### 11.5 RTL-Specific UI Tests

| TC ID | Test Case | Expected | Priority |
|-------|-----------|----------|----------|
| TC-940 | Navigation drawer | Opens from right side in Arabic | P0 |
| TC-941 | Back button / arrows | Point in correct RTL direction | P0 |
| TC-942 | Charts (line, bar) | Render correctly in RTL (x-axis direction) | P1 |
| TC-943 | Forms and inputs | Text fields aligned right in Arabic | P0 |
| TC-944 | Kiosk numpad | Numbers display correctly (numerals are LTR even in Arabic) | P0 |
| TC-945 | Date display | Gregorian only in MVP (no Hijri -- Q34d) | P0 |

---

## 12. Regression Checklist

### 12.1 Critical Path Smoke Tests (P0 -- run before every release)

- [ ] **AUTH-01**: Register new user with email + OTP
- [ ] **AUTH-02**: Login with existing credentials
- [ ] **AUTH-03**: Google OAuth sign-in
- [ ] **AUTH-04**: Password reset via OTP
- [ ] **AUTH-05**: Token refresh (JWT 1h expiry)
- [ ] **GYM-01**: Link to gym via 6-char code
- [ ] **GYM-02**: QR check-in on kiosk (active member)
- [ ] **GYM-03**: Member-number check-in on kiosk
- [ ] **GYM-04**: Expired membership check-in blocked
- [ ] **GYM-05**: Gym switcher works (multi-gym member)
- [ ] **TRAIN-01**: Browse exercise library (200+ exercises)
- [ ] **TRAIN-02**: Log ad-hoc workout
- [ ] **TRAIN-03**: Remote mode activation + guided workout card appears
- [ ] **TRAIN-04**: Remote mode deactivation + card disappears
- [ ] **SESS-01**: Book session with assigned trainer
- [ ] **SESS-02**: Cancel session (normal + late cancel)
- [ ] **SESS-03**: Live Session mode + trainer log
- [ ] **SESS-04**: Member dual-log + side-by-side view
- [ ] **BILL-01**: Record payment + receipt generated
- [ ] **BILL-02**: Session package decrement on completed session
- [ ] **PLAN-01**: Feature gating (disabled -> 403)
- [ ] **PLAN-02**: Feature gating (enabled -> success)
- [ ] **PLAN-03**: Admin Deploy pushes config to apps
- [ ] **PDPL-01**: Record consent
- [ ] **PDPL-02**: Download my data (export job)
- [ ] **PDPL-03**: Delete account (soft-delete)
- [ ] **LANG-01**: Arabic/English toggle + RTL
- [ ] **SUSP-01**: Suspend gym -> all apps blocked
- [ ] **SUSP-02**: Reactivate gym -> all apps restored

### 12.2 Post-Deployment Verification Steps

After each deployment to staging or production:

1. **Health check**: GET `/health` returns 200
2. **Database connectivity**: API connects to SQL Server 2022
3. **Redis connectivity**: Cache operations succeed
4. **Seq logging**: Logs flowing to Seq dashboard
5. **Swagger**: All routes render at `/swagger`
6. **Auth flow**: Complete registration + login cycle
7. **Check-in**: QR + numpad check-in on kiosk
8. **Feature gating**: Verify plan-features endpoint returns correct matrix
9. **FCM**: Send test notification, verify delivery
10. **SignalR**: Connect to config-hub, verify heartbeat
11. **Seed data**: 4 plans seeded, 200+ exercises, 50+ foods, feature matrix populated
12. **Admin dashboard**: Login as super_admin, verify gym list loads
13. **Arabic**: Switch to Arabic, verify RTL rendering

### 12.3 Module-by-Module Regression

| Module | Key Regression Tests |
|--------|---------------------|
| **Identity** | Register, login, OTP, consent, profile, link-gym, delete account, restore |
| **Gyms** | Check-in (QR + numpad + manual), member CRUD, branding, sections, seasonal hours, suspension |
| **Training** | Exercise library, workout builder, remote mode toggle (on/off/per-client), workout log |
| **Progress** | Weight log, measurements, body fat calc, photo upload/privacy/comparison |
| **Scheduling** | Availability, book/cancel/late-cancel, no-show, live session, recurring |
| **Nutrition** | Food log, water tracking, meal plan CRUD |
| **Communication** | Chat (text/photo/voice), notifications (all types), quiet hours, opt-outs |
| **Billing** | Payment recording, void, receipt, package template, package assign/decrement, revenue dashboard |
| **Gamification** | Leaderboards (3 types), opt-out, trainer ratings (privacy), Coach-of-the-Month, testimonial moderation |

### 12.4 Non-Functional Regression

- [ ] **PERF-01**: API response time <200ms (p95) for standard CRUD
- [ ] **PERF-02**: Check-in response <500ms including photo display
- [ ] **PERF-03**: Dashboard load <2s
- [ ] **SEC-01**: No sensitive data in API error messages
- [ ] **SEC-02**: CORS configured correctly
- [ ] **SEC-03**: Rate limiting active (5 failed logins -> lockout)
- [ ] **SEC-04**: TLS 1.3 enforced
- [ ] **COMPAT-01**: iOS 15+ support
- [ ] **COMPAT-02**: Android 10+ support
- [ ] **COMPAT-03**: Chrome, Firefox, Safari latest 2 versions (web)

---

## Appendix A: Test Case Summary

| Section | Test Cases | P0 | P1 | P2 |
|---------|-----------|-----|-----|-----|
| 2. User Story Tests | TC-001 to TC-152 | 78 | 54 | 20 |
| 4. Plan-Based Feature Gating | TC-200 to TC-244 | 17 | 8 | 2 |
| 5. Role-Based Access | TC-300 to TC-314 | 10 | 4 | 1 |
| 6. Admin Deploy Flow | TC-400 to TC-413 | 11 | 1 | 0 |
| 7. Edge Cases | TC-500 to TC-593 | 18 | 14 | 3 |
| 8. PDPL Compliance | TC-600 to TC-643 | 25 | 0 | 0 |
| 9. Storage Limits | TC-700 to TC-722 | 5 | 10 | 1 |
| 10. Cross-App Consistency | TC-800 to TC-813 | 5 | 7 | 0 |
| 11. Arabic/RTL Tests | TC-900 to TC-945 | 11 | 6 | 2 |
| 12. Regression Checklist | 28 smoke + 13 deploy + module checklists | -- | -- | -- |
| **Total** | **~200+ test cases** | **~180** | **~104** | **~29** |

---

## Appendix B: Verification Against Team 11 Checklist (PRODUCT_SPEC.md Section 7)

- [x] `QA_TEST_PLAN.md` covers every story in Section 2 (Stories 1-105 mapped to TC-001 through TC-152)
- [x] All 20 docs/04 scenarios are mapped to test cases (Section 3, Scenarios 1-20)
- [x] Plan-based access matrix is exercised (Section 4 -- full 31-feature x 4-plan matrix)
- [x] Role-based access matrix is exercised (Section 5 -- 5 user roles + 4 admin sub-roles)
- [x] Deploy flow end-to-end test exists (Section 6 -- TC-400 through TC-413)
- [x] PDPL compliance test pack exists (Section 8 -- consent, export, deletion, retention, audit)
- [x] Storage limit boundary tests exist (Section 9 -- per-plan limits, overrides, retention)
- [x] Cross-app consistency checks exist (Section 10 -- data consistency + real-time sync)
- [x] Regression checklist ready for every release (Section 12 -- smoke tests + post-deploy + module regression)

## Trainee /me Endpoints (PRD: PRD_TRAINEE_ME_ENDPOINTS.md)

Trainee-facing read-only endpoints that surface the authenticated user's own nutrition and billing data, scoped to `X-Gym-Id`. None of these are plan-gated (PDPL exemption). Cross-references: **Q3** (multi-gym), **Q9** (voided payments), **Rule #10** (PDPL right to own data).

### Cross-cutting guarantees (apply to all three endpoints)

| ID | Test case | Expected result | PRD ref |
|----|-----------|-----------------|---------|
| TC-ME-001 | Call without `Authorization` header | `401 Unauthorized` | Auth |
| TC-ME-002 | Call with JWT but without `X-Gym-Id` header | `400` body contains `GYM_CONTEXT_REQUIRED` | Gym scope |
| TC-ME-003 | Call with JWT + `X-Gym-Id` but user has no membership at that gym | `403` body contains `NOT_A_MEMBER_OF_GYM` | Gym scope |
| TC-ME-004 | Call while target gym `Status == Suspended` | `403` body contains `GYM_SUSPENDED` | PRODUCT_SPEC §5 |
| TC-ME-005 | Plan feature `meal_plans` / `session_packages` / `payment_recording` toggled OFF at gym | Still `200` (regression guard — never 403) | PDPL Rule #10 |
| TC-ME-006 | Trainee has data at Gym A + Gym B, calls with `X-Gym-Id=gymB` | Only Gym B data returned; no Gym A leakage | Q3 multi-gym |

### `GET /api/v1/nutrition/me/meal-plan`

| ID | Test case | Expected result | PRD ref |
|----|-----------|-----------------|---------|
| TC-ME-MP-001 | Trainee has no assigned meal plan | `200` with `data: null` | §1 Acceptance |
| TC-ME-MP-002 | Trainee has exactly one assigned (non-template) meal plan | `200` with full plan payload | §1 Acceptance |
| TC-ME-MP-003 | Trainee has multiple assigned plans over time | Returns most-recently-assigned (by `AssignedAt DESC`) | §1 Behaviour |
| TC-ME-MP-004 | Trainer's only plan for this trainee is a template (`IsTemplate == true`) | Returns `data: null` | §1 Acceptance |
| TC-ME-MP-005 | Plan was assigned at Gym A; trainee switches to Gym B context | `data: null` under `X-Gym-Id=gymB` | §1 Edge case |
| TC-ME-MP-006 | Gym downgraded to a plan without `meal_plans` feature | Still returns the existing plan | §1 Edge case + PDPL |
| TC-ME-MP-007 | Trainer who assigned the plan has since left the gym | Plan still returned with historical trainer display name | §1 Edge case |

### `GET /api/v1/billing/me/packages`

| ID | Test case | Expected result | PRD ref |
|----|-----------|-----------------|---------|
| TC-ME-PK-001 | Trainee has no packages | `200` with `data: []` and `meta.total: 0` | §2 Edge case |
| TC-ME-PK-002 | Trainee has 1 active + 1 exhausted + 1 expired, default filter | Only the active one returned (default `status=Active`) | §2 Behaviour |
| TC-ME-PK-003 | `status=All` with the same dataset | All three returned | §2 Acceptance |
| TC-ME-PK-004 | `status=Expired` with the same dataset | Only the expired package | §2 Acceptance |
| TC-ME-PK-005 | Another member at the same gym has packages | Caller does not see other members' packages | §2 Behaviour |
| TC-ME-PK-006 | Multi-gym: packages at Gym A and Gym B, caller uses `X-Gym-Id=gymB` | Only Gym B packages returned | §2 Acceptance + Q3 |
| TC-ME-PK-007 | Package trainer has left the gym (Q4) | Package still returned; `trainerName` is the historical snapshot | §2 Edge case |
| TC-ME-PK-008 | Late-cancelled session already deducted (Q5) | `remainingSessions` reflects the decrement | §2 Edge case + Q5 |
| TC-ME-PK-009 | `pageSize=500` (exceeds max 50) | Clamped to 50 or `400 VALIDATION_ERROR` | §2 Acceptance |
| TC-ME-PK-010 | Plan feature `session_packages` toggled OFF | Still `200` | §2 Acceptance + PDPL |

### `GET /api/v1/billing/me/payments`

| ID | Test case | Expected result | PRD ref |
|----|-----------|-----------------|---------|
| TC-ME-PAY-001 | Trainee has no payments | `200` with `data: []` and `meta.total: 0` | §3 Edge case |
| TC-ME-PAY-002 | Trainee has a voided payment (Q9) | Voided row is **included**; `status="Voided"`; `amount` is positive | §3 Acceptance + Q9 |
| TC-ME-PAY-003 | Voided payment fields | `voidReason` + `voidedAt` populated; `amount > 0` (never negative) | §3 Field ref + Q9 |
| TC-ME-PAY-004 | Only voided payments exist (rare data-entry case) | All are returned | §3 Edge case |
| TC-ME-PAY-005 | `page=1&pageSize=2` with 5 records | 2 items + `meta.{page:1, pageSize:2, total:5, totalPages:3}` | §3 Acceptance |
| TC-ME-PAY-006 | `pageSize=500` (exceeds max 50) | Clamped to 50 or `400 VALIDATION_ERROR` | §3 Acceptance |
| TC-ME-PAY-007 | Date range `from=X&to=Y` | Filter is inclusive on both ends | §3 Acceptance |
| TC-ME-PAY-008 | Invalid date format | `400 VALIDATION_ERROR` | §3 Acceptance |
| TC-ME-PAY-009 | `purpose=Membership` filter | Only rows with that purpose returned | §3 Acceptance |
| TC-ME-PAY-010 | Default sort | Rows ordered by `PaidAt DESC` | §3 Acceptance |
| TC-ME-PAY-011 | Multi-gym: payments at Gym A and Gym B, caller uses `X-Gym-Id=gymB` | Only Gym B payments returned | §3 Acceptance + Q3 |
| TC-ME-PAY-012 | Recording staff user was deleted | `recordedByStaffName` is the snapshot value or `null` (never throws) | §3 Edge case |
| TC-ME-PAY-013 | `referenceId` points to deleted package/plan | `referenceLabel` is last-known snapshot or `null` | §3 Edge case |
| TC-ME-PAY-014 | Plan feature `payment_recording` toggled OFF | Still `200` | §3 Acceptance + PDPL |

### Traceability

- Automated coverage lives in:
  - `tests/GymApp.Tests.Application/Nutrition/GetMyMealPlanQueryHandlerTests.cs`
  - `tests/GymApp.Tests.Application/Billing/GetMyPackagesQueryHandlerTests.cs`
  - `tests/GymApp.Tests.Application/Billing/GetMyPaymentsQueryHandlerTests.cs`
  - `tests/GymApp.Tests.Integration/Nutrition/MyMealPlanIntegrationTests.cs`
  - `tests/GymApp.Tests.Integration/Billing/MyPackagesIntegrationTests.cs`
  - `tests/GymApp.Tests.Integration/Billing/MyPaymentsIntegrationTests.cs`
  - `tests/GymApp.Tests.Functional/Nutrition/MyMealPlanFunctionalTests.cs`
  - `tests/GymApp.Tests.Functional/Billing/MyPackagesFunctionalTests.cs`
  - `tests/GymApp.Tests.Functional/Billing/MyPaymentsFunctionalTests.cs`
- Every test method is named `Should_<behaviour>_When_<precondition>` and maps 1:1 to an acceptance bullet in `PRD_TRAINEE_ME_ENDPOINTS.md`.
- Regression triggers: any change to `IGymMembershipChecker`, `GymAccessErrors`, or the handlers MUST rerun this pack before merge.
