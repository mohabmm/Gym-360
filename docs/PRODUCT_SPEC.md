# GymApp Platform — PRODUCT_SPEC.md

> **Owner:** Team 1 (Product Manager)
> **Status:** Draft v1 — pending Team 1.5 (Domain Expert) gap fill.
> **Sources:** docs/01_GYMAPP_BACKEND_COMPLETE.md, docs/02_GYMAPP_FRONTEND_COMPLETE.md, docs/04_GYMAPP_FEATURES_GUIDE.md, docs/05_GYMAPP_BUSINESS_MODEL.md, docs/06_GYMAPP_FEATURE_DESCRIPTIONS.md.
> **Authority rule:** Where this document and the docs disagree, the docs win and this file is the bug.

This spec is the contract every downstream team (Architect → QA) reads. Each section ends with the team(s) it primarily serves.

---

## SECTION 1 — USER ROLES & PERMISSIONS

### 1.1 Identity-level roles (`Identity.Role` enum)

| Code | Display | Primary surface | Scope |
|------|---------|-----------------|-------|
| `trainee` | Gym Member | Trainee mobile (Flutter) | One or more gyms |
| `trainer` | Personal Trainer | Trainer mobile (Flutter) | One gym at a time (MVP) — see Section 6 open question Q2 |
| `gym_staff` | Receptionist / Manager | Gym Staff mobile + Gym Staff web | One gym; sub-role `staff_role ∈ {owner, manager, receptionist}` |
| `gym_owner` | Gym Owner | Gym Staff mobile + Gym Staff web | One gym (owner) |
| `platform_admin` | GymApp Internal | Admin Dashboard web | Whole platform; sub-role `adminRole ∈ {super_admin, admin, support, analyst}` |

> `gym_owner` is technically a `gym_staff` row with `staff_role='owner'`. Treat them as a single user in code; only the `staffRole` claim differs.

### 1.2 Trainee — CAN / CANNOT / Visibility

**CAN**
- Register, verify (OTP / Google / Apple), set fitness profile, link to one or more gyms via 6-char gym code.
- Log own ad-hoc workouts at any time from the global exercise library.
- Follow guided "Today's Workout" **only when their trainer has activated Remote Workout Mode for them**.
- Log body metrics (weight, body fat, 9 measurements), upload progress photos (private by default), choose to share photos with their trainer.
- Book / cancel sessions with their assigned trainer based on the trainer's published availability.
- After each session: leave dual-log (1-5 stars rating, RPE 1-10, personal notes, discomfort).
- After each session: rate the **trainer** (1-5 stars + optional comment) — strictly private to gym management.
- Log meals, water, view assigned meal plans (only if `nutrition_tracking` / `meal_plans` enabled for the gym).
- Book / cancel / waitlist gym classes; check in via QR or member number at the kiosk.
- Chat 1-on-1 with their assigned trainer; receive trainer broadcasts.
- See own position on every leaderboard even if opted out; opt out of public visibility per leaderboard.
- Toggle Arabic / English, units (kg/lbs, cm/inches), notification preferences.
- Export personal data (Settings → Download My Data) and request account deletion (PDPL).
- Manage consent (data processing, marketing, photo sharing) and withdraw any consent.

**CANNOT**
- See another member's profile, ratings, weight, photos, or chats.
- See individual ratings of trainers (only the aggregate stars on the trainer's public profile).
- See workouts assigned to them when Remote Workout Mode is OFF (the assignment is for trainer-side reference only).
- Create gym content, classes, packages, or payments.
- Modify member numbers (gym staff own those).
- Use any feature whose `feature_code` is OFF for the gym's resolved plan (handler returns `PLAN.FEATURE_NOT_AVAILABLE`).

**Data visibility**
- Sees: own profile, own logs, own progress, own sessions (with trainer's notes), own leaderboard rank, gym branding, gym classes, gym announcements, trainer aggregate rating + anonymised testimonials.

### 1.3 Trainer — CAN / CANNOT / Visibility

**CAN**
- Register; account is **Pending** until the gym owner approves (`PUT /gyms/{id}/trainers/{trainerId}/approve`).
- Build a public profile (bio, photo, specialisations, certifications with expiry, languages).
- Manage assigned clients only: view their progress, log measurements, write private trainer notes.
- Build workouts (drag-drop exercises, sets/reps/weight/rest, supersets/circuits), save as templates, build multi-week programs (`multi_week_programs`), assign workouts/programs to clients.
- Upload custom exercise media (`custom_exercise_library` — Business plan).
- **Activate / deactivate Remote Workout Mode per client** with an optional reason. State persists in `RemoteWorkoutConfig`. Default OFF.
- Set weekly availability slots, default duration, buffer, blocked dates (with auto-notify).
- Run Live Session Mode and submit the trainer-side dual log (effort 1-5, feedback, technique notes, areas to improve, focus next session, attendance).
- View own clients' session-package balances (read-only).
- Chat 1-on-1 with each client; broadcast to all clients or tag groups; respond to form-check videos.
- View own aggregated rating, Coach-of-the-Month badges, and approved anonymised testimonials.
- Generate branded PDF progress reports for a client (`pdf_reports` — Business).
- Generate own earnings dashboard (read-only) — gross figures only; compensation rules driven by Q1.

**CANNOT**
- See clients of other trainers or any data outside their gym.
- See **individual** ratings — only aggregates and approved testimonials.
- Create or price session packages, accept payments, change member memberships, manage staff, see revenue dashboards, or change feature flags.
- Approve other trainers, change member numbers, change gym branding.
- Use any gated feature whose `feature_code` is OFF.

**Data visibility**
- Sees: own profile, own clients (full progress + sessions + workouts + meal plans + chat), own schedule, own monthly score (aggregate only), own clients' shared progress photos.

### 1.4 Gym Staff — CAN / CANNOT (per `staff_role`)

`receptionist`, `manager`, `owner` use the **default permission matrix** unless the gym is on a plan with `custom_permissions` (Business), in which case `gyms.staff_role_permissions` overrides.

| Permission | Owner | Manager | Receptionist |
|---|---|---|---|
| Check-in members (QR / number / manual) | ✅ | ✅ | ✅ |
| View member profiles | ✅ | ✅ | ✅ |
| Record payments + print receipts | ✅ | ✅ | ✅ |
| Add / edit / freeze members | ✅ | ✅ | ✅ |
| Approve trainers / manage trainer assignments | ✅ | ✅ | ❌ |
| Create / manage classes + waitlists | ✅ | ✅ | ❌ |
| Send announcements | ✅ | ✅ | ❌ |
| View analytics & revenue | ✅ | ✅ | ❌ |
| Create / manage session-package templates | ✅ | ✅ | ❌ |
| Manage staff accounts | ✅ | ✅ | ❌ |
| Edit gym settings & operating hours | ✅ | ✅ | ❌ |
| Edit gym branding (logo / colors) | ✅ | ✅ | ❌ |
| Change subscription / billing settings | ✅ | ❌ | ❌ |
| View **individual** trainer ratings & moderate testimonials | ✅ | ✅ | ❌ |

**All staff CANNOT**: see another gym's data, see internal admin pages, change platform-wide flags, override their own permissions on plans without `custom_permissions`.

**Data visibility**: Their gym's full operational data (members, trainers, sessions, payments, classes, branding, analytics depending on role).

### 1.5 Platform Admin (`adminRole`)

| Permission | super_admin | admin | support | analyst |
|---|---|---|---|---|
| Approve / reject gym onboarding | ✅ | ✅ | ❌ | ❌ |
| CRUD plans / features / plan-feature matrix | ✅ | ✅ | ❌ | ❌ |
| Per-gym feature override | ✅ | ✅ | ❌ | ❌ |
| Change gym subscription / limit override | ✅ | ✅ | ❌ | ❌ |
| Suspend / reactivate / terminate gym | ✅ | ✅ | ❌ | ❌ |
| Click "Deploy Changes" | ✅ | ✅ | ❌ | ❌ |
| Manage other admin users | ✅ | ❌ | ❌ | ❌ |
| Access PDPL breach log | ✅ | ❌ | ❌ | ❌ |
| Modify platform settings (payment keys, SMS provider) | ✅ | ❌ | ❌ | ❌ |
| View gym detail / user search / churn / analytics | ✅ | ✅ | ✅ | ✅ |
| Impersonate user (debug) | ✅ | ✅ | ❌ | ❌ |
| Modify content (exercise library, app notices) | ✅ | ✅ | ❌ | ❌ |

**Data visibility**: All gyms, all users, all aggregate metrics. Personal sensitive data (progress photos, health conditions) requires elevated access logged in `billing.platform_audit_log`.

---

## SECTION 2 — USER STORIES

> Each story is tagged: **[ROLE]** **[APP]** **[FEATURE_CODE]** **[PRIORITY P0/P1/P2]**.
> Apps: `trainee-mobile`, `trainer-mobile`, `staff-mobile`, `staff-web`, `admin-web`, `cross-app`.

### 2.1 Identity & onboarding

1. **[trainee][trainee-mobile][—][P0]** As a new gym member, I want to register with my email or phone and verify via OTP, so I can access my gym's app within minutes.
2. **[trainee][trainee-mobile][—][P0]** As a new member, I want to sign in with Google or Apple, so I don't have to remember another password.
3. **[trainee][trainee-mobile][—][P0]** As a new member, I want to enter my fitness profile (height, weight, goal, level, health flags), so my trainer can plan for me.
4. **[trainee][trainee-mobile][—][P0]** As a new member, I want to join my gym by entering a 6-char code or scanning a QR, so my account is linked instantly.
5. **[trainee][trainee-mobile][—][P0]** As a member of multiple gyms, I want to switch between gyms, so I see the right branding and data per gym.
6. **[trainer][trainer-mobile][—][P0]** As a new trainer, I want to register and have the gym owner approve me, so unauthorised people can't claim trainer status.
7. **[gym_staff][staff-mobile|staff-web][multi_staff][P1]** As an owner, I want to invite staff with role (manager/receptionist), so each person sees only what they need.
8. **[platform_admin][admin-web][—][P0]** As a super_admin, I want to add another platform admin with a sub-role, so my team can help operate without full power.
9. **[trainee][cross-app][—][P0]** As any user, I want to reset my password via email OTP, so I can recover access.
10. **[trainee][trainee-mobile][—][P0]** As a member, I want to consent (or refuse) to data processing, marketing, and photo sharing during signup, so I retain PDPL control.
11. **[trainee][trainee-mobile][—][P0]** As a member, I want to download all my data (Settings → Download My Data), so I can exercise my PDPL right of access.
12. **[trainee][trainee-mobile][—][P0]** As a member, I want to delete my account (Settings → Delete Account), so I exercise my PDPL right of erasure (soft-delete + 30-day grace + hard purge).

### 2.2 Gyms / Onboarding from admin side

13. **[gym_owner][staff-web][—][P0]** As a gym owner, I want to register my gym (name, address, contact, hours), so the platform admin can review and approve me.
14. **[platform_admin][admin-web][—][P0]** As an admin, I want to see the pending gym onboarding queue with all submitted info, so I can approve or reject.
15. **[platform_admin][admin-web][—][P0]** As an admin, I want approving a gym to auto-generate the 6-char gym code, assign a default plan, and trigger a welcome email.
16. **[gym_owner][staff-web][gym_branding][P1]** As a gym owner, I want to upload my logo and pick brand colors, so the member apps look like my gym.
17. **[gym_owner][staff-web][white_label_branding][P2]** As a gym owner on Business, I want full white-label (splash, app name), so members feel the app is mine.
18. **[gym_owner][staff-web][—][P1]** As an owner, I want to set operating hours and amenities, so members see accurate info.
19. **[platform_admin][admin-web][—][P0]** As an admin, I want to suspend a gym immediately for non-payment / ToS violation, so all of that gym's apps go read-only / blocked.
20. **[platform_admin][admin-web][—][P0]** As an admin, I want to terminate a gym (with name confirmation), so data is purged after the 30-day PDPL retention window.

### 2.3 Members & check-in

21. **[gym_staff][staff-mobile|staff-web][member_management][P0]** As staff, I want to add, edit, search, freeze, and reactivate members, so I replace my paper book entirely.
22. **[gym_staff][staff-mobile|staff-web][member_management][P0]** As staff adding a new member, I want the system to suggest the next sequential **member number** but allow me to override it, so we keep continuity with our existing IDs.
23. **[gym_staff][staff-mobile|staff-web][member_management][P0]** As staff, I want to edit a member's number if it was wrong, so corrections are simple.
24. **[gym_staff][staff-mobile|staff-web][qr_checkin][P0]** As a receptionist, I want a tablet kiosk that accepts both QR-code and member-number check-in, so members without smartphones still get in.
25. **[trainee][trainee-mobile][qr_checkin][P0]** As a member, I want to show my QR at the kiosk and see my photo + "Welcome!", so I feel recognised.
26. **[gym_staff][staff-mobile|staff-web][qr_checkin][P0]** As staff, I want a manual check-in fallback (search by name or number), so even broken phones are no excuse.
27. **[gym_staff][cross-app][qr_checkin][P0]** As staff, the kiosk must block check-in for expired memberships and show a clear renewal message.
28. **[gym_owner][staff-web][basic_analytics][P1]** As an owner, I want to see today's check-in count and current attendance, so I gauge real-time gym load.

### 2.4 Trainer ↔ Client management

29. **[trainer][trainer-mobile][—][P0]** As a trainer, I want a list of all my assigned clients with last/next session and goal tags, so I plan my day.
30. **[trainer][trainer-mobile][—][P0]** As a trainer, I want a 5-tab client profile (Overview / Progress / Sessions / Workouts / private Notes), so all context is one tap away.
31. **[trainer][trainer-mobile][—][P0]** As a trainer, I want to send a new client a PAR-Q intake form, so I document medical readiness.
32. **[gym_staff][staff-web][—][P1]** As a manager, I want to assign a member to a trainer, so the trainer sees them in their list.

### 2.5 Workouts & programming

33. **[trainer][trainer-mobile][exercise_library_global][P0]** As a trainer, I want to browse 200+ exercises by muscle group / equipment, so I plan workouts fast.
34. **[trainer][trainer-mobile][workout_builder][P1]** As a trainer, I want to drag exercises into a workout, set sets/reps/weight/rest/supersets, and save as a template.
35. **[trainer][trainer-mobile][workout_builder][P1]** As a trainer, I want to assign a workout to a client **for my own session planning**, even when Remote Workout Mode is OFF — the client must NOT see it in that case.
36. **[trainer][trainer-mobile][multi_week_programs][P2]** As a trainer, I want to map workouts across weeks into a multi-week program with themes.
37. **[trainer][trainer-mobile][custom_exercise_library][P2]** As a trainer on Business, I want to add my own custom exercises with my own demo video.
38. **[trainee][trainee-mobile][exercise_library_global][P0]** As a member, I want to browse the exercise library and ad-hoc-log a workout from it, regardless of plan or remote mode.
39. **[trainee][trainee-mobile][—][P0]** As a member, I want a calendar / list of every workout I completed, so I see my history.

### 2.6 Remote Workout Mode (the differentiator)

40. **[trainer][trainer-mobile][remote_workout_mode][P0]** As a trainer, I want a per-client toggle to enable Remote Workout Mode with an optional reason, so I bridge in-person and remote training.
41. **[trainee][trainee-mobile][remote_workout_mode][P0]** As a member whose trainer enabled remote mode, I want a "Today's Workout" card on my home screen with a guided step-by-step experience (demo media, sets/reps, rest timer, log fields).
42. **[trainer][trainer-mobile][remote_workout_mode][P1]** As a trainer, I want to upload exercise demo images / videos so the client can perform exercises correctly without me there.
43. **[trainee][trainee-mobile][remote_workout_mode][P0]** As a member, I want a notification ("Your trainer enabled remote workouts") when remote mode is activated and another when it is deactivated.
44. **[trainer][trainer-mobile][remote_workout_mode][P0]** As a trainer, I want to manage remote mode independently per client — toggling for client A must NOT affect client B.

### 2.7 Live Session & dual logging

45. **[trainer][trainer-mobile][session_logging][P0]** As a trainer, I want a Live Session screen that walks me through the planned workout, lets me log reps/weight per set, swap exercises mid-session, and time the session.
46. **[trainer][trainer-mobile][session_logging][P0]** As a trainer, I want to submit a post-session log (effort 1-5, feedback, technique notes, areas to improve, next focus, attendance).
47. **[trainee][trainee-mobile][session_logging][P0]** As a member, I want a notification right after my session prompting me to leave my own dual log (rating 1-5, RPE 1-10, notes, discomfort).
48. **[trainer][trainer-mobile][session_logging][P0]** As a trainer, I want to see both my log and the member's log on the session detail, so I have a complete picture of every session.
49. **[gym_owner][staff-web][session_logging][P1]** As an owner, I want to see how many trainer-side logs are missing this week, so I can hold trainers accountable.

### 2.8 Scheduling

50. **[trainer][trainer-mobile][—][P0]** As a trainer, I want to set my weekly availability, default session length, and buffer, so members only book valid slots.
51. **[trainer][trainer-mobile][—][P0]** As a trainer, I want to block specific dates (vacation/sick) with auto-notify of impacted clients.
52. **[trainee][trainee-mobile][—][P0]** As a member, I want to see my trainer's free slots and book a session.
53. **[trainee][trainee-mobile][—][P0]** As a member, I want to cancel a session — if <24h, it gets flagged as a "late cancel".
54. **[trainer][trainer-mobile][—][P0]** As a trainer, I want to mark a no-show, so the count is tracked per client (Q5).
55. **[trainer][trainer-mobile][—][P1]** As a trainer, I want to set a recurring weekly slot for a client (up to 12 weeks).

### 2.9 Progress

56. **[trainee][trainee-mobile][progress_tracking][P1]** As a member, I want to log my weight at any time and see a trend chart over 30/90/365 days with kg/lbs toggle.
57. **[trainee][trainee-mobile][progress_tracking][P1]** As a member, I want to log all 9 body measurements with a per-metric chart.
58. **[trainee][trainee-mobile][progress_tracking][P1]** As a member, I want to compute body fat using the Navy method from my measurements.
59. **[trainee][trainee-mobile][progress_photos][P1]** As a member, I want to upload front/side/back photos and use a side-by-side slider to compare any two dates.
60. **[trainee][trainee-mobile][progress_photos][P1]** As a member, I want my photos private by default, with an explicit "share with my trainer" toggle.
61. **[trainer][trainer-mobile][progress_tracking][P1]** As a trainer, I want to log measurements **for** my client (tagged "trainer-measured"), so InBody / caliper data lives in one place.

### 2.10 Nutrition

62. **[trainee][trainee-mobile][nutrition_tracking][P2]** As a member, I want to log meals from a food database with macros and see daily totals vs target.
63. **[trainee][trainee-mobile][nutrition_tracking][P2]** As a member, I want quick-add water buttons (250 / 500 / custom) with a daily ring.
64. **[trainer][trainer-mobile][meal_plans][P2]** As a trainer, I want to build a 7-day meal plan with alternatives and assign it to a client.
65. **[trainee][trainee-mobile][meal_plans][P2]** As a member, I want to view my assigned meal plan and tick off completed meals.

### 2.11 Communication

66. **[trainee][trainee-mobile][push_notifications][P1]** As a member, I want push notifications for session reminders (24h, 1h), trainer feedback posted, membership expiring, announcements, streak milestones, new workout assigned, with quiet hours and per-type opt-outs.
67. **[trainer][trainer-mobile][push_notifications][P1]** As a trainer, I want a 1-on-1 chat with each client (text, photo, voice, video) and broadcasts to a tagged group.
68. **[trainee][trainee-mobile][push_notifications][P1]** As a member, I want all notifications inside an in-app notification center with read/unread badges.

### 2.12 Classes

69. **[gym_staff][staff-mobile|staff-web][class_management][P1]** As staff, I want to create a class (name, trainer, day/time, capacity, recurring).
70. **[trainee][trainee-mobile][class_management][P1]** As a member, I want to book a class spot — and if the class is full, join a waitlist that auto-promotes me.
71. **[gym_staff][staff-mobile|staff-web][class_management][P1]** As staff, cancelling a class must auto-notify every booked + waitlisted member.

### 2.13 Billing / payments / packages

72. **[gym_staff][staff-mobile|staff-web][payment_recording][P1]** As a receptionist, I want to record a payment (cash / card / transfer), pick the purpose (membership / package / product), and auto-generate a receipt number `REC-{GymCode}-{YYYYMMDD}-{SEQ}`.
73. **[gym_staff][staff-mobile|staff-web][payment_recording][P1]** As staff, I want a basic revenue dashboard (today / week / month) split by memberships vs PT packages.
74. **[gym_owner][staff-web][session_packages][P1]** As an owner, I want to **create** PT package templates (e.g., 10 sessions for EGP 5,000).
75. **[gym_staff][staff-mobile|staff-web][session_packages][P1]** As a receptionist, I want to **assign** a template to a member and link it to a specific trainer at the moment of payment.
76. **[trainer][trainer-mobile][session_packages][P1]** As a trainer, I want **read-only** view of my clients' remaining sessions, with alerts at ≤3 and =1.
77. **[gym_owner][staff-web][session_packages][P1]** As an owner, I want trainers to be **unable** to create or price packages — this is staff-only.
78. **[trainee][trainee-mobile][—][P1]** As a member, I want a renewal reminder 7 days before expiry.

### 2.14 Gamification

79. **[trainee][trainee-mobile][member_leaderboards][P2]** As a member, I want three monthly leaderboards (Attendance / Weight Loss % / Consistency) and to see my own rank even if I opt out.
80. **[trainee][trainee-mobile][member_leaderboards][P2]** As a member, I want a privacy toggle that hides me from public boards.
81. **[trainee][trainee-mobile][trainer_ratings][P2]** As a member, I want a post-session prompt to rate my trainer (1-5 + comment) — strictly private to gym management.
82. **[gym_owner][staff-web][trainer_ratings][P2]** As an owner, I want to see every individual rating (member name, stars, comment, session) so I can monitor trainer quality.
83. **[trainer][trainer-mobile][trainer_ratings][P2]** As a trainer, I want to see only my **aggregate** rating (e.g., 4.7 ★) and trend over time — never individual ratings.
84. **[gym_staff][staff-web][coach_of_the_month][P2]** As a manager, I want the system to compute Coach-of-the-Month at month-end using the normalised formula (60% stars + 20% consistency + 20% retention) with min 3 unique-client ratings.
85. **[gym_staff][staff-web][testimonial_moderation][P2]** As a manager, I want a moderation queue for client comments → I approve good ones (anonymised) for the trainer's public profile, reject inappropriate ones.
86. **[trainee][trainee-mobile][trainer_ratings][P2]** As a member browsing trainers, I want to see each trainer's aggregate rating + approved anonymised testimonials, so I choose well.

### 2.15 Analytics

87. **[gym_owner][staff-web][basic_analytics][P1]** As an owner, I want a dashboard showing active members, new members this month, revenue this month, and check-in counts.
88. **[gym_owner][staff-web][advanced_analytics][P2]** As an owner, I want a peak-hours heatmap, retention rate, trainer utilisation, churn alerts (no-visit ≥ 10d), and class popularity.
89. **[gym_owner][staff-web][churn_prediction][P2]** As an owner on Business, I want each member tagged Low/Medium/High churn risk with suggested actions.

### 2.16 Admin platform-side

90. **[platform_admin][admin-web][—][P0]** As an admin, I want a master gym list with filters by plan / status / city, drill-down to per-gym detail.
91. **[platform_admin][admin-web][—][P0]** As an admin, I want full CRUD on plans (`subscription_plans`), features (`features`), and the toggle matrix (`plan_features`), with `is_public`, `is_time_limited`, `available_from/until`.
92. **[platform_admin][admin-web][—][P0]** As an admin, I want per-gym feature overrides (force ON/OFF) with a reason field.
93. **[platform_admin][admin-web][—][P0]** As an admin, I want to override storage limits per gym (e.g., extra photo quota for a VIP gym).
94. **[platform_admin][admin-web][—][P0]** As an admin, I want a "Deploy Changes" button that pushes config to all affected apps (FCM silent push for mobile + SignalR for web). The button shows a pending-change badge.
95. **[platform_admin][admin-web][—][P0]** As an admin, I want a full audit log of every plan / feature / override change (`plan_change_log`), filterable by gym.
96. **[platform_admin][admin-web][—][P0]** As an admin, I want platform-wide analytics: total gyms, total members, MRR, growth, by-city breakdown, feature usage.
97. **[platform_admin][admin-web][—][P1]** As an admin, I want a churn list of at-risk gyms (no login ≥14d, declining members, declining revenue).
98. **[platform_admin][admin-web][—][P1]** As an admin, I want to impersonate any user for debugging.
99. **[platform_admin][admin-web][—][P1]** As an admin, I want a support-ticket inbox with priority, assignee, conversation history.
100. **[platform_admin][admin-web][—][P0]** As an admin, I want a PDPL breach log (access only `super_admin`) with mandatory 72h notification flag.
101. **[platform_admin][admin-web][—][P1]** As an admin, I want to bulk-suspend gyms past their 21-day grace period.

### 2.17 Cross-cutting (i18n, security, settings)

102. **[trainee][cross-app][—][P0]** As any user, I want to toggle Arabic / English with full RTL.
103. **[trainee][cross-app][—][P0]** As any user, I want my session to time out after inactivity (Q23).
104. **[trainee][cross-app][—][P0]** As any user, I want my account locked after N failed login attempts (Q24).
105. **[trainee][cross-app][—][P0]** As any user, I want my data encrypted at rest (TDE) and in transit (TLS 1.3).

> **80+ stories delivered: 105 user stories, all tagged.** Section 2 serves Teams 2-11 (every team).

---

## SECTION 3 — ACCEPTANCE CRITERIA (Given / When / Then)

> Acceptance criteria for the **highest-risk** stories. Lower-risk CRUD stories follow the standard "create → list → update → delete returns 200/204" template and are not repeated here.

### AC-S1 (Story 1: Trainee registers via OTP)
- **Given** the email `m@x.com` does not exist, **When** the user POSTs `/api/v1/identity/register {email, password, firstName, lastName, phone, role:trainee}`, **Then** the API returns `201 + {accessToken, refreshToken}` and a `UserRegisteredDomainEvent` is emitted.
- **Given** the email already exists, **When** registering, **Then** `400 IDENTITY.EMAIL_EXISTS`.
- **Given** the password is `pa1`, **When** registering, **Then** `400` validation error (≥8 chars + uppercase + number).
- **Given** an OTP has been requested, **When** the user requests >3 in 1 hour, **Then** `429 IDENTITY.OTP_RATE_LIMIT`.
- **Given** an OTP older than 5 min, **When** verifying, **Then** `400 IDENTITY.OTP_EXPIRED`.

### AC-S10/11/12 (PDPL flows)
- **Given** a registered trainee, **When** they POST `/api/v1/identity/consents {type:data_processing,granted:true,consentText}`, **Then** a `UserConsent` row is written with `IpAddress`, `GrantedAt`, and exact text. Required for any further write actions.
- **Given** a trainee, **When** they GET `/api/v1/identity/my-data`, **Then** the API returns a JSON archive of all of their personal data across modules within the SLA (Q19), and the request is logged in `platform_audit_log`.
- **Given** a trainee, **When** they DELETE `/api/v1/identity/account`, **Then** the user is soft-deleted with `DeletedAt = now`, all sensitive data flagged for purge, and a Quartz job hard-purges after 30 days.
- **Given** a deleted user re-registers within 30 days, **When** they register again, **Then** they receive the option to restore their account (Q12).

### AC-S22/23/24/25/26/27 (Member number + check-in)
- **Given** a gym creating a new member, **When** staff opens "Add member", **Then** the system suggests `MAX(memberNumber)+1` but the field is editable.
- **Given** a member-number `7824` already exists in this gym, **When** staff tries to use it, **Then** `409 GYMS.MEMBER_NUMBER_EXISTS`.
- **Given** a kiosk in kiosk mode and an active member with QR `abc`, **When** the camera reads `abc`, **Then** the screen shows the member's photo + name + "Welcome!" for 5s, then auto-resets, and a check-in row is written with `method=QR`.
- **Given** member-number `7824` belongs to an active member, **When** staff types it on the kiosk numpad, **Then** identical visual + a check-in row with `method=MEMBER_NUMBER`.
- **Given** a member with an expired membership, **When** any check-in method is attempted, **Then** the kiosk shows "Membership expired — please renew at the desk", **no** check-in row is written, the response is `403 GYMS.MEMBERSHIP_EXPIRED`.
- **Given** a member already checked in within the last 1 hour, **When** another check-in is attempted, **Then** `409 GYMS.CHECKIN_THROTTLED`.
- **Given** the gym is on `Status=Suspended`, **When** any check-in is attempted, **Then** `403 GYM_SUSPENDED`.

### AC-S35/40/41/43/44 (Remote Workout Mode)
- **Given** a trainer assigns workout `W1` to client `C1` while `RemoteWorkoutConfig.IsActive=false`, **When** `C1` opens the trainee app and queries `GET /api/v1/training/today`, **Then** the response is `null` and the home screen shows **no** "Today's Workout" card.
- **Given** the same scenario, **When** `C1` opens the exercise library, **Then** they can still log an ad-hoc workout (this is a separate path).
- **Given** the trainer POSTs `/api/v1/training/remote-workout/activate {clientId:C1, reason:"on leave"}`, **Then** a `RemoteWorkoutConfig` row is written/updated with `IsActive=true, ActivatedAt=now, Reason="on leave"`, an integration event `RemoteWorkoutActivatedIntegrationEvent` is emitted, and `C1` receives an FCM push: "Your trainer has enabled remote workouts for you".
- **Given** activation just happened, **When** `C1` queries `GET /api/v1/training/today`, **Then** the response is the assigned workout with full guided detail (sets, reps, weight, rest, demo media URLs).
- **Given** the trainer activates remote mode for `C1` but **not** for `C2`, **When** `C2` opens the app, **Then** the "Today's Workout" card is **still hidden** for `C2` — the toggle is per-client and independent.
- **Given** the trainer POSTs `/api/v1/training/remote-workout/deactivate {clientId:C1}`, **Then** `IsActive=false, DeactivatedAt=now`, integration event emitted, FCM push to `C1`: "Remote workout mode has been turned off", the "Today's Workout" card disappears on next fetch.
- **Given** the gym is **not** on a plan with `remote_workout_mode` enabled, **When** the trainer attempts activation, **Then** `403 PLAN.FEATURE_NOT_AVAILABLE` with `requiredPlan: pro`.

### AC-S45/46/47/48 (Dual logging)
- **Given** an in-progress session, **When** the trainer POSTs `/api/v1/scheduling/sessions/{id}/trainer-log {effort, feedback, ...}`, **Then** `Session.TrainerLog` is set, `Status=Completed`, and `SessionCompletedIntegrationEvent` is emitted.
- **Given** a completed session, **When** the member POSTs `/api/v1/scheduling/sessions/{id}/member-log {rating, rpe, notes}`, **Then** `Session.MemberLog` is set independently.
- **Given** GET `/api/v1/scheduling/sessions/{id}`, **Then** the response includes BOTH logs side-by-side plus session metadata.
- **Given** the trainer never logs and the member submits theirs, **Then** the dual-log view shows the member side filled and a `Trainer log: pending` placeholder.

### AC-S52/53 (Booking)
- **Given** a trainer with availability Sun-Thu 8AM-6PM and a 60-min default + 15-min buffer, **When** the member POSTs `/api/v1/scheduling/sessions {trainerId, date, startTime}`, **Then** the API checks the slot is within availability AND not overlapping any existing session AND not on a blocked date. If valid → `Session.Status=Pending`, integration event emitted. Else → `409 SCHEDULING.SLOT_UNAVAILABLE`.
- **Given** a session at 10:00 AM today, **When** the member cancels at 11 PM yesterday (>24h before), **Then** the cancellation is normal.
- **Given** the same session, **When** the member cancels at 9 AM today (<24h), **Then** `Session.Status=CancelledByMember, IsLateCancel=true`. The trainer is notified.

### AC-S70/71 (Class booking + waitlist)
- **Given** a class with capacity 20 and 20 confirmed bookings, **When** the 21st member books, **Then** they are added to the waitlist (`Status=Waitlisted`).
- **Given** one of the 20 cancels, **When** the cancellation is processed, **Then** the next waitlisted member is auto-promoted, their status flips to `Booked`, and they receive a notification.
- **Given** a class is cancelled by staff, **Then** every member with `Booked` or `Waitlisted` receives a notification + integration event emitted.

### AC-S74/75/76/77 (Session packages)
- **Given** an owner POSTs `/api/v1/billing/package-templates {name, sessionCount, pricePerSession, totalPrice}`, **Then** a `SessionPackageTemplate` row is created.
- **Given** a trainer (not owner) POSTs the same, **Then** `403 BILLING.TRAINER_CANNOT_MANAGE_PACKAGES`.
- **Given** a receptionist POSTs `/api/v1/billing/packages/assign {templateId, memberId, trainerId}`, **Then** a `SessionPackage` row is created with `RemainingSessions=template.sessionCount`.
- **Given** a `SessionCompletedIntegrationEvent` arrives for that member+trainer, **When** the handler runs, **Then** `RemainingSessions--`. At `=3` → `SessionPackageLow` event. At `=1` → second `SessionPackageLow` event. At `=0` → `SessionPackageExhausted`.
- **Given** the trainer queries `/api/v1/billing/packages/my-clients`, **Then** they get a read-only list — no edit, no price field exposed.

### AC-S81/82/83 (Trainer ratings privacy)
- **Given** a member submits a rating with stars=3 + comment "wasn't focused", **Then** the rating row is written.
- **Given** the trainer queries `/api/v1/gamification/trainers/{me}/ratings`, **Then** `403` — trainers cannot list individual ratings ever.
- **Given** the trainer queries `/api/v1/gamification/trainers/{me}/public-profile`, **Then** they get only their aggregate stars + Coach badges + approved testimonials.
- **Given** the gym manager queries the same `…/{trainerId}/ratings`, **Then** they get the full list (member name, stars, comment, sessionId, createdAt).
- **Given** Coach-of-the-Month month-end job runs and trainer T has 2 unique-client ratings, **When** the job ranks trainers, **Then** T is **excluded** (min 3 unique).
- **Given** weight-loss leaderboard ranking: A (80→78kg = 2.5%) vs B (100→98kg = 2.0%), **Then** A ranks above B (percentage, not absolute).
- **Given** a member toggled `ShowOnLeaderboard=false`, **When** other members fetch the leaderboard, **Then** that member is missing from the list. **When** the same member fetches `/leaderboards/my-position`, **Then** they still see their rank.

### AC-S91/92/94/95 (Plans + Deploy)
- **Given** super_admin POSTs `/api/v1/admin/plans {code:"ramadan_2026", name, monthlyPrice, isPublic:true, isTimeLimited:true, availableFrom, availableUntil}`, **Then** the plan row is created. Visible on signup page only inside that window.
- **Given** super_admin toggles `nutrition_tracking` ON for the Pro plan, **Then** a row is changed in `plan_features`, `plan_change_log` is appended, but apps **do not** update yet (no auto-poll).
- **Given** super_admin clicks "Deploy" with scope "all gyms on Pro", **Then**: (1) Redis cache invalidated for those gyms, (2) FCM silent push `{type:config_refresh}` sent to all mobile apps of those gyms, (3) SignalR `config_updated` broadcast to web clients, (4) audit row written, (5) toast: "Deployed to N gyms (M apps notified)".
- **Given** the trainee app receives the FCM silent push, **When** the handler runs, **Then** the app fetches `/api/v1/billing/plan-features`, updates the local Riverpod state, and the locked nutrition tab unlocks without re-login.
- **Given** the staff web is connected to SignalR, **When** the `config_updated` event arrives, **Then** TanStack Query invalidates `['plan-features']` and the UI refreshes.

### AC-S19/27 (Suspension)
- **Given** a gym in `Status=Active, PaymentStatus=Overdue`, after 14 days `→ Status=GracePeriod` with banner. After 21 days `→ Status=Suspended`. Apps go read-only / kiosk blocked. API returns `403 GYM_SUSPENDED` for all writes.
- **Given** an admin clicks "Suspend now", **Then** Status=Suspended immediately with the supplied reason; all apps receive an FCM `gym_suspended` push.
- **Given** payment is recorded → admin "Reactivate" → Status=Active. FCM `gym_reactivated` push.

### AC plan-gating template (applies to every gated feature)
- **Given** `plan_features.is_enabled = false` for the gym's plan AND no `gym_feature_overrides` row, **When** the user calls a gated handler, **Then** `403 PLAN.FEATURE_NOT_AVAILABLE` with `{feature, currentPlan, requiredPlan, upgradeUrl}`.
- **Given** an admin sets `gym_feature_overrides.override_value=true`, **When** they click Deploy, **Then** the gym's apps unlock the feature within seconds.

### Edge case ACs included throughout
- Plan downgrade past member limit (Section 6.1).
- Storage limit reached (Section 6.7).
- `<3` ratings → no Coach badge (above).
- Override removal → fall back to plan default (above).
- Late cancel <24h flagged (above).
- Member opt-out from leaderboards (above).
- Session timeout / lockout per Q23/Q24.
- Account deletion within 30 days reversal per Q12.

> Section 3 serves Teams 5, 6, 9, 11.

---

## SECTION 4 — FEATURE GATING MATRIX

### 4.1 Plan vs feature

| Feature code | Free | Starter | Pro | Business | Stories |
|---|---|---|---|---|---|
| `member_management` | ✅ | ✅ | ✅ | ✅ | 21-23 |
| `qr_checkin` | ✅ | ✅ | ✅ | ✅ | 24-27 |
| `exercise_library_global` | ✅ | ✅ | ✅ | ✅ | 33, 38 |
| `basic_reporting` | ✅ | ✅ | ✅ | ✅ | 28 |
| `unlimited_trainers` | ❌ | ✅ | ✅ | ✅ | 6 |
| `workout_builder` | ❌ | ✅ | ✅ | ✅ | 34, 35 |
| `session_logging` | ❌ | ✅ | ✅ | ✅ | 45-49 |
| `progress_tracking` | ❌ | ✅ | ✅ | ✅ | 56-58, 61 |
| `progress_photos` | ❌ | ✅ | ✅ | ✅ | 59, 60 |
| `class_management` | ❌ | ✅ | ✅ | ✅ | 69-71 |
| `push_notifications` | ❌ | ✅ | ✅ | ✅ | 66-68 |
| `payment_recording` | ❌ | ✅ | ✅ | ✅ | 72, 73 |
| `session_packages` | ❌ | ✅ | ✅ | ✅ | 74-77 |
| `basic_analytics` | ❌ | ✅ | ✅ | ✅ | 87 |
| `multi_staff` | ❌ | ✅ | ✅ | ✅ | 7 |
| `gym_branding` | ❌ | ❌ | ✅ | ✅ | 16 |
| `nutrition_tracking` | ❌ | ❌ | ✅ | ✅ | 62, 63 |
| `meal_plans` | ❌ | ❌ | ✅ | ✅ | 64, 65 |
| `member_leaderboards` | ❌ | ❌ | ✅ | ✅ | 79, 80 |
| `trainer_ratings` | ❌ | ❌ | ✅ | ✅ | 81-83, 86 |
| `coach_of_the_month` | ❌ | ❌ | ✅ | ✅ | 84 |
| `testimonial_moderation` | ❌ | ❌ | ✅ | ✅ | 85 |
| `advanced_analytics` | ❌ | ❌ | ✅ | ✅ | 88 |
| `remote_workout_mode` | ❌ | ❌ | ✅ | ✅ | 40-44 |
| `multi_week_programs` | ❌ | ❌ | ✅ | ✅ | 36 |
| `white_label_branding` | ❌ | ❌ | ❌ | ✅ | 17 |
| `api_access` | ❌ | ❌ | ❌ | ✅ | — |
| `custom_exercise_library` | ❌ | ❌ | ❌ | ✅ | 37 |
| `custom_permissions` | ❌ | ❌ | ❌ | ✅ | 7 (override) |
| `pdf_reports` | ❌ | ❌ | ❌ | ✅ | (Trainer 2.7) |
| `churn_prediction` | ❌ | ❌ | ❌ | ✅ | 89 |

> The matrix above is the **default seed**. Per docs/01 the matrix is fully editable from Admin Dashboard. The backend never reads `if(plan==pro)`. It always reads `IsFeatureEnabledAsync(gymId, code)`.

### 4.2 UX when feature is disabled vs enabled

| State | Mobile (Flutter) | Web (Next.js) |
|---|---|---|
| **Disabled** | The card / tab renders with a lock icon and an upgrade prompt: feature name + 1-line description + required plan + price + "Upgrade Now" + "Maybe Later". Tapping the locked tab opens the upgrade modal. Backend returns `403 PLAN.FEATURE_NOT_AVAILABLE` with `requiredPlan` so the prompt knows what to ask. | The route still renders but the page body is the same upgrade card. Sidebar item shows a 🔒. |
| **Enabled** | Full feature UI. No upgrade prompt. The `FeatureGate` widget reads from the cached `planFeaturesProvider`. | TanStack Query returns the feature enabled; the gated component is rendered. |
| **Just enabled (admin Deploy)** | FCM silent push → re-fetch `/billing/plan-features` → Riverpod refreshes → locked widgets unlock without re-login. | SignalR `config_updated` → TanStack Query invalidates `['plan-features']` → re-render. |
| **Just disabled (admin Deploy)** | Same path → previously unlocked widgets re-lock, in-progress writes that hit a now-disabled endpoint return `403`. | Same. |

### 4.3 Limit-based gates (also stored in plan_features.limit_value or plan_storage_limits)

| Resource | Free | Starter | Pro | Business | Enforced by |
|---|---|---|---|---|---|
| Members | 25 | 75 | 200 | 500 | `AddMemberCommand` checks `GetMemberLimitAsync` |
| Trainers | 1 | ∞ | ∞ | ∞ | `AddTrainerCommand` checks `GetTrainerLimitAsync` |
| Staff accounts | 1 | 3 | 10 | ∞ | `AddStaffCommand` |
| Progress photos / member | 5 | 20 | 50 | 200 | `UploadProgressPhotoCommand` checks `GetPhotoLimitPerMemberAsync` |
| Total file storage / gym | 100MB | 500MB | 2GB | 10GB | All upload handlers check `GetCurrentStorageUsedBytes + size ≤ GetStorageLimitBytes` |
| Check-in retention | 30d | 6mo | 1y | 2y | Quartz purge job |
| Workout-log retention | 30d | 1y | 2y | 5y | Quartz purge job |
| Analytics retention | 7d | 3mo | 1y | 2y | Quartz purge job |
| API rate limit (req/min) | 0 | 0 | 0 | 60 | API gateway middleware |
| Custom exercise videos | 0 | 0 | 0 | 50 (500MB) | `CreateCustomExerciseCommand` |

> Section 4 serves Teams 4, 6, 7, 8, 9, 11.

---

## SECTION 5 — DATA FLOWS

### 5.1 Trainee registration → linked to gym

```
1. POST /api/v1/identity/register → Identity creates User (Status=Active), emits UserRegisteredDomainEvent.
2. Outbox → IntegrationEvent UserRegistered → Communication: schedule welcome notification (no-op until linked).
3. POST /api/v1/identity/send-otp → store OTP (5min TTL, 3/hr rate).
4. POST /api/v1/identity/verify-otp → User.IsVerified=true, emits UserVerifiedDomainEvent.
5. PUT /api/v1/identity/profile → fitness profile saved, consents recorded.
6. POST /api/v1/identity/link-gym {gymCode} → Identity validates code via IGymQueryService.GetGymByCodeAsync, adds GymMembership(gymId, role:Trainee, joinedAt). Emits UserLinkedToGymDomainEvent.
7. UserLinkedToGymIntegrationEvent → Gyms (auto-add to gym.members), Communication (welcome notification with gym branding).
8. Trainee app fetches GET /api/v1/billing/plan-features → caches features.
9. Trainee app fetches GET /api/v1/gyms/{gymId}/branding → applies theme.
```

### 5.2 Workout logging — normal (no remote mode)

```
NORMAL: Trainer assigns workout for SELF (status=Pending, IsRemoteMode=false).
1. Trainer POST /api/v1/training/workouts (creates Workout aggregate).
2. Trainer POST /api/v1/training/assignments {clientId, workoutId, mode:planning} (IsRemoteMode=false).
3. Client opens trainee app → GET /api/v1/training/today returns null (because IsRemoteWorkoutActiveAsync(clientId) == false).
4. Client home screen has NO "Today's Workout" card.
5. In-person session occurs → trainer uses Live Session Mode against the planning assignment.
6. Trainer POST /api/v1/scheduling/sessions/{id}/trainer-log → SessionCompletedIntegrationEvent.
7. Member is notified to leave dual-log → POST /api/v1/scheduling/sessions/{id}/member-log.
8. Both logs are visible on session detail.
```

### 5.3 Workout logging — REMOTE mode

```
1. Trainer POST /api/v1/training/remote-workout/activate {clientId, reason}.
2. Training writes RemoteWorkoutConfig {IsActive:true, ActivatedAt, Reason}, emits RemoteWorkoutActivatedDomainEvent.
3. Outbox → RemoteWorkoutActivatedIntegrationEvent → Communication.
4. Communication creates Notification (type:RemoteWorkoutActivated) and triggers FCM push to client: "Your trainer enabled remote workouts".
5. Client app receives push → re-fetches GET /api/v1/training/today.
6. /today returns the assigned workout in full detail because IsRemoteWorkoutActiveAsync == true.
7. Client home screen now shows "Today's Workout" card.
8. Client taps it → guided workout screen with demo media → logs sets/reps/weight live.
9. POST /api/v1/training/workout-logs → WorkoutLog created, WorkoutLoggedIntegrationEvent → Progress (streak update) + Communication (trainer notified).
10. Trainer POST /api/v1/training/remote-workout/deactivate {clientId} → reverse: card hides, FCM "Remote workouts turned off".
```

### 5.4 Dual session logging

```
1. Member books session → POST /api/v1/scheduling/sessions → Status=Pending → notify trainer.
2. Trainer confirms → Status=Confirmed → notify member.
3. Session time arrives → trainer opens Live Session → Status=InProgress.
4. Trainer logs → POST /api/v1/scheduling/sessions/{id}/trainer-log → Status=Completed → SessionCompletedIntegrationEvent.
5. Event consumed by:
   a. Training (no-op except metrics).
   b. Progress (streak update).
   c. Billing (decrement matching SessionPackage if any).
   d. Communication (FCM push to member: "Leave your post-session feedback").
6. Member opens session → POST /…/member-log → MemberLog set.
7. GET /…/sessions/{id} now returns BOTH logs (TrainerLog + MemberLog). The dual-log view is the unique selling point.
```

### 5.5 Admin Deploy → push → app config refresh

```
1. Super_admin makes config changes in Admin Dashboard (CRUD plans/features/overrides). DB is updated; apps DON'T know yet.
2. Super_admin clicks "Deploy Changes" (scope: all / plan / gym).
3. POST /api/v1/admin/deploy/{scope}/{id?}.
4. Backend:
   a. Computes affected gymIds.
   b. Invalidates Redis "plan-features:{gymId}" for each.
   c. For each gym: fetches device tokens of all logged-in users → FCM batch send {data:{type:"config_refresh", gymId}}.
   d. Broadcasts SignalR event "config_updated" with affected gymIds to all admin/staff web clients.
   e. Writes plan_change_log row (deployment audit).
5. Mobile app (background): FCMService receives data-only message → calls planFeaturesRepository.refresh() → updates Riverpod state → UI rebuilds.
6. Web app: SignalR handler invalidates ['plan-features'] React Query → auto refetch → component re-render.
7. Admin dashboard receives deployment summary → shows toast "Deployed to N gyms (M apps notified)".
```

### 5.6 Trainer rating → coach scoring

```
1. After session, member POST /api/v1/gamification/trainer-ratings {trainerId, sessionId, stars, comment}.
2. TrainerRating row written; TrainerRatedIntegrationEvent → notify gym manager.
3. Daily Quartz job aggregates today's ratings into TrainerMonthlyScore (rolling) → AvgStars updated.
4. Month-end Quartz job CalculateMonthlyTrainerScoresCommand:
   a. For each trainer in gym: compute NormalizedScore = AvgStars*0.6 + ConsistencyBonus*0.2 + ClientRetentionRate*0.2.
   b. Skip trainers with <3 unique-client ratings.
   c. Pick highest → mark IsCoachOfTheMonth=true, emit CoachOfTheMonthSelectedDomainEvent.
5. Outbox → CoachOfTheMonthSelectedIntegrationEvent → Communication:
   a. FCM push to winning trainer: "You are Coach of the Month".
   b. FCM push to all gym members: "Coach of the Month announced!".
6. TrainerPublicProfile.CoachOfTheMonthCount++.
```

### 5.7 Payment recorded → receipt + revenue + package decrement

```
1. Receptionist POST /api/v1/billing/payments {memberId, amount, method, purpose:SessionPackage, referenceId:templateId}.
2. Payment row created (Status=Completed). ReceiptNumber = "REC-{GymCode}-{YYYYMMDD}-{NextSeq}".
3. PaymentReceivedDomainEvent → PaymentReceivedIntegrationEvent.
4. If purpose=SessionPackage: also POST /api/v1/billing/packages/assign internally → SessionPackage row created.
5. Notification to member: "Payment received — receipt REC-…".
6. Revenue dashboard reflects new payment (read-side via AsNoTracking).
7. SessionCompleted event later → handler calls billing.PackageUsageService.Decrement(memberId, trainerId).
8. At RemainingSessions==3 → SessionPackageLow event → notify member + trainer + manager.
9. At ==1 → second alert. At ==0 → SessionPackageExhausted → notify all + flag in dashboard.
```

### 5.8 PDPL "Download my data"

```
1. Trainee POST /api/v1/identity/my-data → enqueue export job (Quartz).
2. Job collects: Identity (profile, consents), Gyms (memberships, check-ins), Training (workout logs), Progress (metrics, photos), Scheduling (sessions, dual logs), Nutrition (food logs, water, meal plans), Communication (chats, notifications), Billing (payments), Gamification (ratings given, rankings).
3. Job writes a JSON archive to encrypted blob storage and emails the user a signed download URL (24h TTL).
4. platform_audit_log row written.
5. SLA driven by Q19 (default: within 7 days per PDPL guidance).
```

### 5.9 Gym suspension flow (non-payment)

```
Active+Paid → due date passes → PaymentStatus=Overdue (still works).
+7 days overdue → email/notification to owner.
+14 days overdue → Status=GracePeriod, banner shown, admin sees amber badge.
+21 days overdue → Status=Suspended:
  - All apps: full-screen overlay "Gym Suspended". Read-only.
  - Kiosk: "This gym's subscription is suspended."
  - API: 403 GYM_SUSPENDED for writes.
  - Data preserved.
Payment → admin Reactivate → Status=Active → FCM gym_reactivated.
```

> Section 5 serves Teams 3, 5, 6, 7, 8, 11.

---

## SECTION 6 — EDGE CASES & DECISIONS

### 6.1 Plan downgrade past resource limit
- **Scenario**: Gym is on Pro (200 members, currently 180). Admin downgrades to Starter (75).
- **Decision**: The downgrade succeeds. **No member data is deleted.** New write attempts that exceed Starter limits are blocked with `400 PLAN.LIMIT_EXCEEDED {currentUsage:180, newLimit:75}`. The Admin Dashboard surfaces a "Gyms over their plan limit" warning. The gym owner must remove members until under the new limit before new members can be added. No automatic freezing or deletion.

### 6.2 Feature disabled while in use
- **Scenario**: Trainer is mid-workout build with `workout_builder`; admin disables it for the plan and clicks Deploy.
- **Decision**: The trainer can finish the in-flight save (idempotent, returns 200). Subsequent calls return `403 PLAN.FEATURE_NOT_AVAILABLE`. The mobile app re-locks the tab on next refresh.

### 6.3 Feature override removed
- **Scenario**: Gym Y had `nutrition_tracking` force-ON via override even though their plan is Starter. Admin removes the override.
- **Decision**: Resolution falls back to plan default → OFF. On Deploy, gym Y's apps re-lock the nutrition tab. Existing food logs are **preserved** but unreadable until re-enabled (read endpoint returns `403`). PDPL: the user can still export this data via `my-data`.

### 6.4 Trainer with <3 ratings
- **Scenario**: Trainer T finishes the month with 2 unique-client ratings.
- **Decision**: T's `NormalizedScore` is computed but T is **excluded** from Coach-of-the-Month ranking. Their public profile still shows the aggregate stars. UI shows "More ratings needed for Coach award".

### 6.5 Late cancel (RESOLVED — configurable policy per gym)
- **Scenario**: Member cancels within the gym's cancellation window (configurable, default 24h).
- **Decision (Founder Q5)**: The cancellation time window is **configurable by the gym** from Gym Staff app → Settings → Session Policies (`GymSessionPolicy.cancellation_window_hours`, default 24). If the member cancels **within** the window (late cancel): `Session.Status=CancelledByMember, IsLateCancel=true`, **the session IS deducted from the package** (trainer blocked their schedule and should be paid), trainer is notified. No additional fee. No-show/late-cancel suspension threshold is **also configurable** from Staff app → Settings: "After X no-shows/late-cancels in Y days → action" (default: 3 in 30d → flag + notify trainer). New entity: `gyms.gym_session_policies`.

### 6.6 Remote Workout Mode is OFF by default
- **Scenario**: New trainer-client pair.
- **Decision**: `RemoteWorkoutConfig.IsActive=false` always. Trainee's `GET /training/today` returns `null` and the home card is hidden until the trainer flips the toggle.

### 6.7 Storage limit reached
- **Scenario**: Gym is at 99/100 photos for a member on Free.
- **Decision**: `UploadProgressPhotoCommand` checks `GetPhotoLimitPerMemberAsync`. At limit: `400 PLAN.STORAGE_LIMIT_REACHED {resource, limit}` with upgrade prompt. No silent overwrite of oldest photo.

### 6.8 Member opted out of leaderboards
- **Scenario**: Member sets `ShowOnLeaderboard=false`.
- **Decision**: `GET /leaderboards/{type}` excludes them from the public list. `GET /leaderboards/my-position` still returns their private rank. They can re-opt in any time.

### 6.9 PDPL — consent withdrawal
- **Scenario**: Member withdraws data-processing consent.
- **Decision**: Account flagged for deletion. 30-day soft-delete window. Hard purge after. Anonymous aggregate data (member count, revenue) survives in the gym's analytics.

### 6.10 PDPL — minor (under 18)
- **Scenario**: Member registers as 16 years old.
- **Decision (Q16)**: 16+ allowed. Ages 13-15 require parental consent (parent registers on child's behalf). Under 13 blocked entirely.

### 6.11 Payment void (refunds are Phase 2)
- **Scenario**: Receptionist records cash payment incorrectly, or member asks for a refund.
- **Decision (Founder Q9)**: Refunds are **disallowed in MVP**. If a payment was recorded incorrectly, staff can **void** it (`Payment.Status=Voided` with reason). Voided payments are excluded from revenue. No negative-amount rows. Voiding does NOT auto-restore session-package count — staff adjusts manually. Full refund system (with partial refunds and installments) is Phase 2.

### 6.12 Multiple gyms — same user
- **Scenario**: Trainee belongs to Gym A and Gym B.
- **Decision**: User has two `GymMembership` rows. They switch context in the app via a gym switcher. Branding, classes, trainer all swap accordingly. Plan-feature resolution is per-gym.

### 6.13 Trainer leaves a gym
- **Scenario**: Trainer T departs Gym A.
- **Decision (Q4)**: T's clients are auto-unassigned; gym owner reassigns manually via "reassign clients" UI. Templates/programs owned by T become invisible at the gym (owner can clone before removal). Session and chat history preserved (immutable audit). T's name remains on historical records.

### 6.14 No-show threshold (RESOLVED — configurable per gym)
- **Scenario**: Member missed 3 sessions in a row.
- **Decision (Founder Q5)**: Threshold is **configurable** by the gym from Staff app → Settings → Session Policies. Default: 3 no-shows/late-cancels in 30 days → flag profile + notify trainer. Gym can change the count, window, and action (`flag_only`, `notify_trainer`, `suspend_booking`). See `gyms.gym_session_policies` table.

### 6.15 Session timeout & lockout
- **Scenario**: User idle on app or web.
- **Decision (Q29/Q30)**: Mobile: JWT 1h + refresh 30d (sliding). Staff Web: idle timeout 30 min. Admin Web: idle timeout 15 min. Kiosk: never times out. Account lockout: 5 failed attempts in 15 min → locked 30 min. After 3 lockout cycles → must reset via email OTP.

### 6.16 Profile photo & file size
- **Scenario**: User uploads a 30 MB photo.
- **Decision (Q27)**: Client-side hard limit: 10 MB. Server-side: long-edge ≤2048 px, JPEG q80, strip EXIF. Profile photos: ≤512 px, q80.

### 6.17 Offline queue
- **Scenario**: Trainer logs a session in airplane mode.
- **Decision (Q26)**: Write-queue offline with 7-day local retention. Conflict resolution = last-writer-wins by record ID. Trainee/Trainer apps queue writes and replay on reconnection. Kiosk does NOT support offline (must be online).

### 6.18 Multi-trainer per member
- **Scenario**: Member books with two different trainers in the same gym.
- **Decision (Q2)**: Allowed. A member CAN have multiple trainers simultaneously. Each `Session` and `WorkoutAssignment` is owned by exactly one trainer. Each `RemoteWorkoutConfig` is per trainer-client pair. Chat is 1-on-1 per trainer. No "primary trainer" concept.

### 6.19 Trainer at multiple gyms
- **Scenario**: Trainer T works at Gym A and Gym B.
- **Decision (Founder Q3)**: Allowed. One login, multiple `GymMembership` rows, gym-context switcher in Trainer app (same as trainee gym switcher). ALL data (clients, workouts, sessions, packages, ratings, availability) is scoped to the selected gym via `X-Gym-Id` header. Coach-of-the-Month is per-gym. Session counts per-gym.

### 6.20 Gym sections (men's / women's / mixed)
- **Scenario**: Egyptian gym with separate men's and women's sections.
- **Decision (Founder Q31)**: Section is an enum (`Mens`, `Womens`, `Mixed`). Gym owner enables sections from Settings. Member's `GymMembership.section` is set on add and validated against enabled sections. Trainer assignment checks section compatibility. Classes tagged with section. New table: `gyms.gym_sections`.

### 6.21 Ramadan operating hours
- **Scenario**: Gym shifts to 10 PM-3 AM during Ramadan.
- **Decision (Q32)**: Seasonal operating-hour overrides supported. Gym owner adds a named schedule with a date range. Resolves: seasonal (if active) > default. New table: `gyms.gym_seasonal_hours`.

### 6.22 Storage limits and PDPL data export
- The `my-data` export ignores plan-based storage limits and always returns everything the user owns. The export is sent via signed URL with TTL (24h).

> Section 6 serves Teams 3, 6, 9, 11.

---

## SECTION 7 — REVIEW CRITERIA (per-team self-validation checklists)

> Each team MUST tick every box before declaring itself done. The next team is unblocked only when the prior team's checklist is 100% green.

### FOR TEAM 2 (Architect)
- [ ] Solution `GymApp.sln` builds with zero warnings.
- [ ] `Common.Domain` exposes `Entity`, `IDomainEvent`, `Result<T>`, `Error`, `IUnitOfWork` matching Bookify verbatim.
- [ ] `Common.Application` exposes `ICommand`, `ICommand<T>`, `IQuery<T>`, handlers, MediatR pipeline behaviours (Logging, Validation, Caching), `IDateTimeProvider`, `ICacheService`.
- [ ] `Common.Application.IGymPlanService` interface contains: `IsFeatureEnabledAsync`, `GetEnabledFeaturesAsync`, `GetMemberLimitAsync`, `GetTrainerLimitAsync`, `GetStaffLimitAsync`, `GetPlanCodeAsync`, `GetStorageLimitBytesAsync`, `GetCurrentStorageUsedBytesAsync`, `GetPhotoLimitPerMemberAsync`, `GetCheckInRetentionDaysAsync`. (≥6 methods, plus storage.)
- [ ] `PlanErrors` static contains `FeatureNotAvailable`, `LimitExceeded`, `StorageLimitReached`.
- [ ] `Common.Infrastructure` has Outbox (pure EF Core, Quartz `[DisallowConcurrentExecution]`), Inbox, `AddDomainEventsAsOutboxMessages` interceptor, `DateTimeProvider`, Redis cache, JWT token service.
- [ ] `Common.Presentation` has the `IEndpoint` interface.
- [ ] API host `GymApp.Api` has `Program.cs`, `Dockerfile`, `docker-compose.yml`, `GlobalExceptionHandler`, Serilog+Seq config, Swagger.
- [ ] All 9 module skeletons exist as `.csproj` files (Identity, Gyms, Training, Progress, Scheduling, Nutrition, Communication, Billing, Gamification) with the four layers + IntegrationEvents.
- [ ] No module references another module's Domain/Application/Infrastructure.
- [ ] All handlers are `internal sealed` (architecture test stub passes).
- [ ] `UseSqlServer` is the only DB provider used. No Dapper.
- [ ] Architecture test project exists with NetArchTest rules wired (even if assertions are TODO).

### FOR TEAM 3 (Domain)
- [ ] Every aggregate has a private parameterless constructor and a public static factory.
- [ ] Every domain event is `sealed` and ends with `DomainEvent`.
- [ ] Identity has `User`, `UserConsent` entities (PDPL).
- [ ] Gyms has `Gym` (with suspension state machine), `GymClass`, `Announcement`, `StaffRole`.
- [ ] Training has `Exercise`, `Workout`, `Program`, `WorkoutLog`, `WorkoutAssignment`, `RemoteWorkoutConfig`.
- [ ] Progress has `BodyMetricEntry`, `ProgressPhoto`, `Streak`.
- [ ] Scheduling has `TrainerAvailability`, `Session` with `TrainerLog` + `MemberLog` value-object children.
- [ ] Nutrition has `FoodLog`, `WaterLog`, `MealPlan`.
- [ ] Communication has `Conversation`, `Notification`, `NotificationPreference`.
- [ ] Billing has `MembershipPlan`, `Payment`, `SessionPackageTemplate`, `SessionPackage`, `subscription_plans`-side aggregate `SubscriptionPlan`, `Feature`, `PlanFeature`, `GymSubscription`, `GymFeatureOverride`, `PlanChangeLog`, `PlanStorageLimit`, `GymStorageUsage`.
- [ ] Gamification has `MemberLeaderboard`, `TrainerRating`, `TrainerMonthlyScore`, `TrainerPublicProfile`, `LeaderboardOptOut`.
- [ ] Repository interfaces declared per aggregate root.
- [ ] IntegrationEvents project lists all 21 events from docs/01.
- [ ] Domain unit tests (Team 9 will fill out) are at least scaffolded.

### FOR TEAM 4 (Database)
- [ ] Each module has its own `DbContext`, schema name matches docs/01 table.
- [ ] Each `DbContext` implements `IUnitOfWork` and uses `UseSqlServer`.
- [ ] EF Configurations cover every entity.
- [ ] Repos use pure EF Core with `AsNoTracking()` for reads.
- [ ] Billing seeds: `subscription_plans` (Free/Starter/Pro/Business), all 31+ feature codes, full `plan_features` matrix from Section 4, `plan_storage_limits` rows for every plan.
- [ ] Identity seeds: `user_consents` schema present.
- [ ] Training seeds: 200+ exercise rows.
- [ ] Nutrition seeds: 50+ Egyptian food items.
- [ ] All TDE-encrypted columns flagged in EF configuration.
- [ ] Migrations build and `dotnet ef database update` runs against the docker-compose SQL Server.

### FOR TEAM 5 (API)
- [ ] Every story (Section 2) has at least one endpoint mapped to it.
- [ ] Admin endpoints (Section 2.16) all exist with `[Authorize(Roles="platform_admin")]` and per-adminRole policies.
- [ ] `POST /api/v1/admin/deploy/{scope}/{id?}` triggers FCM + SignalR + audit log.
- [ ] `GET /api/v1/identity/my-data` and `DELETE /api/v1/identity/account` exist (PDPL).
- [ ] SignalR `config-hub` registered.
- [ ] FCM service registered.
- [ ] All endpoints respect the standard response envelope `{ data, errors, meta }`.
- [ ] Storage limit endpoints exist (`PUT /admin/gyms/{id}/limits`, `GET /admin/gyms/{id}/storage-usage`).
- [ ] Swagger renders all routes grouped by module.

### FOR TEAM 6 (Application Logic)
- [ ] Every command/query handler is `internal sealed`.
- [ ] Every gated handler calls `IGymPlanService.IsFeatureEnabledAsync` BEFORE doing real work.
- [ ] Every limit-gated handler checks the relevant `Get…LimitAsync` / `GetCurrentStorageUsedBytesAsync` method.
- [ ] Every command has a FluentValidation validator.
- [ ] Domain events flow Domain → Outbox → MediatR → integration handlers.
- [ ] PDPL handlers exist: `RecordConsentCommand`, `ExportMyDataCommand`, `DeleteAccountCommand`.
- [ ] All 21 integration events have at least one handler (or a documented "no consumers yet").

### FOR TEAM 7 (Flutter)
- [ ] `gymapp_core` exposes `ApiClient`, `FeatureGate` widget, `FCMService`, theme, l10n (ar+en), consent UI.
- [ ] Trainee app: home screen conditional "Today's Workout" card driven by `IsRemoteWorkoutActiveAsync`.
- [ ] Trainee app: PDPL consent on registration; "Download my data" + "Delete account" in Settings.
- [ ] Trainer app: per-client Remote Workout Mode toggle with reason field.
- [ ] Gym Staff app: kiosk mode with QR + numpad check-in side-by-side.
- [ ] Arabic + English with full RTL on every screen.
- [ ] No polling — only FCM push + manual pull-to-refresh + cold-start fetch.
- [ ] Riverpod `planFeaturesProvider` cached and refreshed on FCM `config_refresh`.

### FOR TEAM 8 (Web)
- [ ] Turborepo `gymapp-web/` with `packages/@gymapp/ui/`, `apps/admin/`, `apps/gym-staff/`.
- [ ] Lovable exports copied to `lovable-export-admin/` and `lovable-export-gym-staff/`.
- [ ] Reusable components extracted to `@gymapp/ui` (data-table, stat-card, sidebar-nav, modal, chart-container, kpi-card).
- [ ] Both apps migrated to Next.js App Router.
- [ ] All mock data replaced with TanStack Query hooks against the real API.
- [ ] next-auth + middleware enforcing `adminRole` (admin app) and `gym_staff/gym_owner` (staff app).
- [ ] SignalR connection invalidates `['plan-features']` on `config_updated`.
- [ ] Suspension overlay shows on `gym_suspended` event.
- [ ] Lovable visual designs preserved (admin: dark dense; staff: warm light, First Gym red).

### FOR TEAM 9 (Dev Testing)
- [ ] Domain unit tests ≥ 90% per module.
- [ ] Application unit tests ≥ 80% per module, **every gated handler has a test for `feature OFF → 403` and `feature ON → success`**, every storage-limited handler has a test for `at limit → 400`.
- [ ] Integration tests use `Testcontainers.MsSql` + `Testcontainers.Redis`.
- [ ] Functional tests cover 401, 400, 200/201 for every endpoint (`[Theory]` + `[InlineData]`).
- [ ] PDPL functional tests: register → consent → my-data → delete → re-register within 30d.
- [ ] Architecture tests pass: Domain events sealed, handlers internal, layer dependencies enforced, module boundaries respected.

### FOR TEAM 10 (DevOps)
- [ ] `docker-compose.yml`: SQL Server 2022, Redis, RabbitMQ, Seq, with health checks + named volumes + TDE.
- [ ] Multi-stage `Dockerfile` for the API.
- [ ] `.github/workflows/tests.yml` (build + unit + arch + integration on PR).
- [ ] `.github/workflows/deploy.yml` (build image + push registry + deploy environment).

### FOR TEAM 11 (QA Engineer)
- [ ] `QA_TEST_PLAN.md` covers every story in Section 2.
- [ ] All 20 docs/04 scenarios are mapped to test cases.
- [ ] Plan-based and role-based access matrices are exercised.
- [ ] Deploy flow end-to-end test exists.
- [ ] PDPL compliance test pack exists (consent, export, deletion, retention).
- [ ] Storage limit boundary tests exist.
- [ ] Cross-app consistency checks (mobile/web parity per feature) exist.
- [ ] Regression checklist ready for every release.

> Section 7 serves the team named in each subsection — gating their hand-off to the next team.

---

## APPENDIX — Resolved (Team 1.5 complete)

All 34 domain questions have been answered (6 by founder, 28 by Team 1.5 defaults). See `FOUNDER_DECISIONS.md` for the full record. Phase 2 features are listed in `PHASE_2_FEATURES.md`.

**Key founder decisions that changed the spec:**
1. **Q3** — Trainers CAN work at multiple gyms (adds gym switcher to Trainer app, all data scoped via `X-Gym-Id`).
2. **Q5** — Late-cancel policy is gym-configurable (not hardcoded 24h). Late cancel deducts session from package. No-show threshold also configurable. New entity: `gyms.gym_session_policies`.
3. **Q9** — No refunds in MVP; void only. Phase 2.
4. **Q28** — Notification caps are Phase 2 (configurable by gym owner + admin).
5. **Q31** — Gym sections are an enum (`Mens`/`Womens`/`Mixed`), gym-configured. New table: `gyms.gym_sections`.
6. **Q33** — Cash reconciliation report is Phase 2, Pro plan feature.

**Teams 2-11 are UNBLOCKED.** Proceed in order.

— end of PRODUCT_SPEC.md —
