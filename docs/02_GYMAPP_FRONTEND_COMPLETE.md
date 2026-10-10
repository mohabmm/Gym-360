# GymApp — Complete Frontend Reference

> **Purpose**: Single document for Claude to implement all client-facing apps. Covers Flutter mobile apps (Trainee, Trainer, Gym Staff Mobile) + Next.js web apps (Gym Staff Web, Admin Dashboard) + shared packages + Lovable-to-production migration workflow.

---

# PART 1: DELIVERABLES OVERVIEW

| # | App | Platform | Framework | Who Uses It | Build Method |
|---|-----|----------|-----------|------------|-------------|
| 1 | Trainee App | iOS + Android | Flutter | Gym members | Claude agents |
| 2 | Trainer App | iOS + Android | Flutter | Personal trainers | Claude agents |
| 3 | Gym Staff Mobile | iOS + Android + Tablet | Flutter | Gym owner, managers, front desk (on-the-floor) | Claude agents |
| 4 | Gym Staff Web | Desktop browser | Next.js | Gym owner, managers (at-the-desk: data tables, billing, analytics) | **Lovable prototype → Claude agents restructure** |
| 5 | Admin Dashboard | Desktop browser | Next.js | Platform operator (you — manages all gyms) | **Lovable prototype → Claude agents restructure** |

**Key decisions**:
- Apps 1-3: Flutter native — optimized for touch, speed, camera (QR), offline
- Apps 4-5: Next.js — optimized for data-heavy desktop work (native HTML tables, Ctrl+F, copy-paste, print, browser back/forward)
- Apps 4-5 share a **Next.js component library** (`@gymapp/ui`) — data tables, stat cards, charts, forms, modals built once, used in both
- **BOTH Apps 4 and 5 are built in Lovable first** as separate projects:
  - **Doc 03a** (`03a_LOVABLE_ADMIN_DASHBOARD.docx`) → Admin Dashboard Lovable prompt. UX: data-dense, dark sidebar, power-user (you).
  - **Doc 03b** (`03b_LOVABLE_GYM_STAFF_WEB.docx`) → Gym Staff Web Lovable prompt. UX: warm, friendly, light theme, approachable (gym owners who may not be tech-savvy).
  - After Lovable, Claude agents restructure both into production Next.js.

---

# PART 2: DEVELOPMENT WORKFLOW

## Phase 1: Lovable Prototype (Week 1)
1. Paste Lovable prompts (doc 03 for Admin, doc 04 for Gym Staff Web)
2. Get working prototypes with mock data
3. Use as **visual reference and stakeholder demo**
4. Export the Lovable code (React + Tailwind + shadcn components)

## Phase 2: Production Architecture (Week 2+)
1. Claude agent creates Next.js Turborepo: apps/gym-staff + apps/admin + packages/@gymapp/ui
2. Claude agent extracts Lovable's UI components into @gymapp/ui shared library
3. Claude agent replaces mock data with real API calls matching backend doc endpoints
4. Claude agent adds TypeScript types matching backend DTOs
5. Claude agent adds auth (JWT), error handling, loading states

## Why This Works for Agent Teams
- **Backend Agent** builds API endpoints → defined in `01_GYMAPP_BACKEND_COMPLETE`
- **Frontend Agent** builds Next.js pages → consumes those exact endpoints
- **Both reference the same API contract** → minimal integration friction
- Lovable gives the visual layer, Claude agents add the engineering layer

---

# PART 3: FLUTTER ARCHITECTURE (Apps 1-3)

## Tech Stack
Flutter 3.x + Dart | Riverpod (state management) | GoRouter (navigation) | Dio (HTTP) | freezed + json_serializable (models) | dartz (Either/Functional) | flutter_secure_storage | fl_chart | firebase_messaging | image_picker | cached_network_image | intl + flutter_localizations (AR/EN + RTL)

## Clean Architecture Per Feature
```
features/{feature}/
├── data/
│   ├── datasources/          # Remote (API calls) + Local (cache)
│   ├── models/               # extends entity, has fromJson/toJson (freezed)
│   └── repositories/         # Implements domain repository interface
├── domain/
│   ├── entities/             # Pure entity, no framework deps
│   ├── repositories/         # Abstract class
│   └── usecases/             # UseCase<Type, Params> → Future<Either<Failure, Type>>
└── presentation/
    ├── providers/            # Riverpod providers + state classes (sealed)
    ├── screens/
    └── widgets/
```

## Riverpod Conventions
Repository: {feature}RepositoryProvider (Provider). UseCase: {action}UseCaseProvider (Provider). State: {feature}Provider (AsyncNotifierProvider with sealed state class). AsyncNotifier pattern: build() → initial state, methods → state = AsyncLoading → result.

## API Client (Shared)
Dio with interceptors: AuthInterceptor (injects JWT), GymIdInterceptor (injects X-Gym-Id header), LoggingInterceptor, ErrorInterceptor (maps DioException → Failure). Base URL from config. 15s timeout.

## Shared Flutter Package: packages/gymapp_core/
Contains: constants (api_endpoints, app_constants, storage_keys), models (user, gym, exercise, workout, body_metric, session, message, notification, meal_plan, payment — all with freezed), network (ApiClient, interceptors, api_exceptions), errors (failure.dart, either), usecases (base UseCase<T,P>), theme (app_theme, app_colors, app_typography, **gym_branding** for dynamic colors from API), widgets (app_button, app_text_field, app_card, loading_shimmer, error_widget, empty_state, stat_card, avatar_widget, chart_widgets), utils (date_formatter, number_formatter, validators, extensions, debouncer), l10n (app_en.arb, app_ar.arb — ~350 strings).

### Dynamic Gym Branding (How the member app gets the gym's colors/logo)

When a gym owner updates branding in the Gym Staff Web or Mobile (Settings → Branding → upload logo, pick colors → Save), the branding data flows to member and trainer apps like this:

```
Gym Owner saves branding → API: PUT /gyms/{gymId}/branding → stores logo URL + primaryColor + secondaryColor in DB
                         → On next app open: Trainee/Trainer app calls GET /gyms/{gymId} → receives branding object
                         → App applies: primaryColor as theme accent, logo in header/splash, secondaryColor for backgrounds
                         → If gym_branding feature DISABLED (Free/Starter): app uses default GymApp green (#10B981) theme
```

```dart
// In gymapp_core/theme/gym_branding.dart
// Fetched on login and cached. Re-fetched on app open.
class GymBranding {
  final String? logoUrl;
  final String primaryColor;     // hex e.g., "#C41E3A"
  final String secondaryColor;   // hex e.g., "#1A1A1A"
  final String? coverPhotoUrl;
}

// Provider: fetches branding from API, applies to ThemeData
final gymBrandingProvider = FutureProvider<GymBranding>((ref) async {
  final gym = await ref.read(gymRepositoryProvider).getMyGym();
  return gym.branding; // null branding = use default theme
});

// In app MaterialApp: use gym colors as theme seed if available
// ThemeData(colorSchemeSeed: Color(int.parse(branding.primaryColor.replaceFirst('#','0xFF'))))
```

The preview card in Settings (showing "How members see your gym" — gym name + logo + colors in a phone-shaped mockup) is rendered CLIENT-SIDE using the same branding data, giving the owner an instant preview before saving.

## Localization: Arabic (primary) + English. Full RTL support. Arabic-Indic numerals supported. ~350 strings.

## FEATURE GATING BY PLAN (All Apps Must Enforce)

**Apps do NOT poll. Apps fetch plan features ONCE on login/startup and cache locally. When the admin deploys changes from the Admin Dashboard, a silent push notification (FCM) tells the app to re-fetch immediately. Web apps receive a SignalR event instead.**

### When Apps Fetch Plan Features
1. **On login / app startup** — fetch from API, cache locally
2. **On silent push received** (FCM data message: `{ type: "config_refresh" }`) — re-fetch, update cache, rebuild UI
3. **On SignalR event** (web apps only: `config_updated`) — invalidate query cache, auto-refetch
4. **On pull-to-refresh** (manual) — re-fetch
5. **Never on a timer** — no polling, no wasted bandwidth

### Gym Suspension Handling (All Apps)
When the admin suspends a gym (non-payment or manual), the backend sends a FCM push `{ type: "gym_suspended" }` to all apps of that gym. Apps respond:
- **Trainee App + Trainer App**: Full-screen overlay message: "Your gym's subscription has been suspended. Please contact your gym management." All features blocked. No dismiss button.
- **Gym Staff Mobile**: Read-only mode. Owner can VIEW data (members, payments, analytics) but cannot ADD/EDIT/DELETE anything. Banner: "Your subscription is suspended. Contact GymApp support to reactivate."
- **Gym Staff Web**: Same read-only mode with banner. Settings page shows "Subscription Suspended" with support contact.
- **Kiosk Check-in**: Shows full-screen "This gym is temporarily unavailable. Please contact gym management." — blocks all check-ins.
- **Grace Period** (14-21 days overdue): Apps work normally but show a persistent amber banner to gym owner/staff: "Your subscription payment is overdue. Pay within X days to avoid suspension."
- **On reactivation**: Admin clicks Reactivate → FCM push `{ type: "gym_reactivated" }` → apps remove suspension overlay immediately.

### Flutter Implementation
```dart
// Provider — fetches once, cached until push signal invalidates it
final planFeaturesProvider = FutureProvider<PlanFeatures>((ref) async {
  return ref.read(billingRepositoryProvider).getPlanFeatures();
});

// FCM silent push handler (in app initialization):
// FirebaseMessaging.onMessage.listen((message) {
//   if (message.data['type'] == 'config_refresh') {
//     ref.invalidate(planFeaturesProvider); // triggers re-fetch → UI rebuilds
//   }
// });

// Reusable gate widget — wraps any screen/widget
// When planFeaturesProvider is invalidated by push signal and re-fetches,
// this widget automatically rebuilds: shows child if enabled, shows UpgradePrompt if not
class FeatureGate extends ConsumerWidget {
  final String featureCode;
  final String requiredPlan;
  final Widget child;

  Widget build(context, ref) {
    final features = ref.watch(planFeaturesProvider);
    return features.when(
      data: (f) => f.isEnabled(featureCode) ? child : UpgradePrompt(feature: featureCode, plan: requiredPlan),
      loading: () => Shimmer(),
      error: (e, s) => child, // fail-open: show feature if can't check
    );
  }
}

// Usage: wrap any gated screen or widget
FeatureGate(featureCode: 'nutrition_tracking', requiredPlan: 'Pro', child: NutritionTab())
```

### Next.js Implementation
```typescript
// Hook — fetches once, cached until SignalR signal invalidates it
// staleTime: Infinity means "never refetch automatically — only when we tell it to"
const usePlanFeatures = () => useQuery(['plan-features'], fetchPlanFeatures, { staleTime: Infinity });

// SignalR listener (in app layout):
// signalRConnection.on('config_updated', () => {
//   queryClient.invalidateQueries(['plan-features']); // triggers re-fetch → UI rebuilds
// });

// Component
function FeatureGate({ code, plan, children }) {
  const { data } = usePlanFeatures();
  if (!data?.features[code]) return <UpgradePrompt feature={code} requiredPlan={plan} />;
  return children;
}
```

### What Each Plan Shows/Hides in the Apps

**Trainee App**:
- Free: Home (no workout card), exercise library, manual workout logging, QR check-in, membership status
- Starter: + guided workouts (if remote mode ON), progress tracking (weight, body fat, measurements, photos), session booking, post-session feedback, class booking, announcements
- Pro: + nutrition log, water tracker, meal plan view, leaderboards, rate trainer, trainer public profile with rating

**Trainer App**:
- Free: Not available (gym must be on Starter+)
- Starter: Client roster, calendar, availability, booking management, workout builder, templates, live session mode, post-session logging, client progress view
- Pro: + remote workout mode toggle, meal plan builder, view client nutrition, multi-week programs, my ratings dashboard, leaderboard view
- Business: + PDF report generation, custom exercise library with own media

**Gym Staff App (Mobile + Web)**:
- Free: Member list (25 max), basic check-in, 1 staff account, basic member count
- Starter: + unlimited members (75 cap), payment recording, receipts, class management, announcements, 3 staff accounts, basic analytics
- Pro: + branded app colors/logo, advanced analytics, churn alerts, trainer ratings detail, coach leaderboard, testimonial moderation, 10 staff accounts
- Business: + white-label, API access, custom exercise library, PDF reports, churn AI, unlimited staff, **custom permission matrix** (editable per role — `custom_permissions` feature)

### Upgrade Prompt Component

Every gated feature shows a consistent upgrade prompt:
- Icon of the locked feature
- "Unlock [Feature Name]"
- "Available on the [Plan Name] plan — EGP [Price]/mo"
- [Upgrade Now] button → deep-links to billing/upgrade page in Gym Staff Web
- [Maybe Later] dismiss button

### Navigation Gating

Bottom nav tabs and sidebar items for gated features should still be visible but show a lock icon and the plan badge. Tapping opens the upgrade prompt. Do NOT hide the tabs entirely — the user needs to know the feature exists so they're motivated to upgrade.

---

# PART 4: TRAINEE APP (gymapp_trainee — Flutter)

## Navigation: 5 bottom tabs
Home | Workout | Progress | Schedule | More (Profile, Nutrition, Chat, Membership, Announcements, Settings)

### Auth & Onboarding (Sprint 1-2)
LoginScreen (/login): POST /identity/login. RegisterScreen (/register): POST /identity/register + send-otp + verify-otp + **ConsentScreen** (PDPL: explicit consent checkbox for data processing + optional marketing consent — consent text stored with timestamp. Must accept before proceeding). ProfileSetupScreen (/onboarding/profile): PUT /identity/profile. LinkGymScreen (/onboarding/link-gym): POST /identity/link-gym.

LoginScreen: App logo → email → password (eye toggle) → Login → Google/Apple social → forgot password → sign up.
ProfileSetupScreen: Photo → name → DOB → gender → height → weight → fitness level → health conditions → goals checkboxes → Continue.
LinkGymScreen: Gym code input → Scan QR → Skip.

### Home (Sprint 5)
HomeScreen (/home): "Good morning, Ahmed!" → gym logo → **TODAY'S WORKOUT card (CONDITIONALLY VISIBLE — only shown if trainer has activated Remote Workout Mode for this client. If not active, card is hidden.)** → NEXT SESSION card → 3 stat cards (streak, weight, water) → weekly bar chart. API: GET /progress/dashboard + /scheduling/sessions/upcoming + /training/today **(returns null if remote mode OFF → hide the workout card)**.

### Workout (Sprint 3-5)
ExerciseLibraryScreen (/exercises): GET /training/exercises (search, filter) — **always available, trainee can always log ad-hoc workouts regardless of remote mode**. GuidedWorkoutScreen (/workout/guided/{id}): exercise demo (images/videos uploaded by trainer) → sets×reps×weight inputs → rest timer → prev/next → complete — **this screen is ONLY accessible when Remote Workout Mode is active for this client. If not active, this route redirects to workout history.** LogWorkoutScreen: POST /training/workout-logs — **always available for manual/ad-hoc logging**. WorkoutHistoryScreen: GET /training/workout-logs — **always available, shows both guided and ad-hoc workout logs**.

### Progress (Sprint 3-5)
LogWeightScreen, LogMeasurementsScreen: POST /progress/body-metrics. ProgressPhotosScreen: GET/POST /progress/photos. ProgressDashboardScreen: weight trend line chart → body fat % → measurements with deltas → photo compare slider → monthly summary.

### Scheduling (Sprint 4-6)
UpcomingSessionsScreen, BookSessionScreen (GET availability/slots → POST sessions), SessionFeedbackScreen (POST sessions/{id}/logs — member log), ClassScheduleScreen.

### Nutrition (Sprint 6-7)
NutritionLogScreen (food-log), WaterTrackerScreen (water-log), MealPlanScreen (meal-plans).

### Communication (Sprint 7)
ChatScreen (messages + WS), AnnouncementsScreen.

### Other
CheckInScreen (shows QR code for scanning + **Member Number displayed prominently** — e.g., "Your Member Number: 7824" — so member can type it at the kiosk if they prefer), MembershipScreen, SettingsScreen (language, units, notifications, privacy, **leaderboard opt-out toggle**, dark mode, **Download My Data** button → GET /identity/my-data → generates JSON export of all user data (PDPL compliance), **Delete My Account** button → confirmation modal "This will permanently delete all your data after 30 days. This cannot be undone." → DELETE /identity/account → logout).

### Storage Limit Handling (All Apps)
When a member or trainer hits a storage limit (e.g., progress photo limit, file storage), the app shows a friendly error: "You've reached your photo limit (20/20). Ask your gym to upgrade their plan for more storage." This is NOT an upgrade prompt to the MEMBER — it tells them to ask their GYM OWNER. The gym owner sees their storage usage in Settings and on the Admin Dashboard.

### Gamification (Sprint 8-9)
**LeaderboardScreen** (/leaderboard): 3 tabs — Attendance (most sessions this month), Weight Loss (highest % lost), Consistency (highest session completion rate). Each tab shows ranked list: position, member name+avatar, score. Members who opted out are hidden. Member always sees their own position at the bottom of the screen even if opted out. API: GET /gamification/leaderboards/{type} + GET /gamification/leaderboards/my-position.

**TrainerProfileScreen** (when viewing assigned trainer): Shows aggregated star rating (e.g., "4.7 ★ from 24 reviews"), Coach of the Month badges, anonymized testimonials ("Great at motivating me through tough sets"). API: GET /gamification/trainers/{id}/public-profile.

**RateTrainerScreen** (after session — prompted via notification): 1-5 stars + optional comment textarea → submit. Rating is **private to gym management only** — trainer never sees who rated what. API: POST /gamification/trainer-ratings.

### Providers
authProvider (global), profileProvider (global), gymProvider (global), todaysWorkoutProvider, upcomingSessionsProvider, progressDashboardProvider, weightTrendProvider.family(dateRange), workoutLogsProvider (paginated), exerciseLibraryProvider (search/filter), nutritionLogProvider, waterLogProvider, chatProvider, notificationCountProvider (StreamProvider global), **leaderboardProvider (auto-dispose, by type)**, **trainerPublicProfileProvider.family(trainerId)**. Offline: cached workouts + queued logs (Hive/Isar).

---

# PART 5: TRAINER APP (gymapp_trainer — Flutter)

## Navigation: 5 bottom tabs
Home | Clients | Workouts | Schedule | More (Earnings, Chat, Packages, Analytics, Profile, Settings)

### Home (Sprint 4)
TrainerHomeScreen: "Hello, Coach Karim!" → TODAY'S SCHEDULE → 3 stats (Pending, Unread, Clients) → QUICK ACTIONS → month summary.

### Client Management (Sprint 3-4)
ClientRosterScreen, ClientDetailScreen (5 tabs: Overview, Progress, Sessions, Workouts, Notes), OnboardingFormScreen (intake: goals, injuries, medical, PAR-Q), ClientNotesScreen (private).

### Scheduling (Sprint 4-6)
AvailabilityScreen (weekly slots + buffer), BookingRequestsScreen (accept/reject), CalendarScreen (day/week, color-coded), RecurringSessionScreen, LeaveManagementScreen (block dates, auto-notify).

### Workout Programming (Sprint 5-6)
WorkoutBuilderScreen (drag-reorder exercises, sets×reps, supersets, save draft/template/assign), TemplateLibraryScreen, ProgramBuilderScreen (multi-week), ExerciseManagerScreen (custom exercises + demo media upload — images/videos).

**Remote Workout Mode** (per-client toggle): In the client detail screen or workout assignment flow, the trainer sees a toggle: "Enable Remote Workouts for [Client Name]". When activated, the trainer provides an optional reason (e.g., "I'm on leave until April 20", "Client traveling", "Supplementary home program"). The client's app then shows the guided workout experience with the trainer's uploaded exercise images/videos. The trainer can activate/deactivate this independently for each client at any time, for any reason — it is NOT limited to leave only. **Default is OFF** — in normal operation, the trainer coaches in person and the trainee does not see guided workouts. API: POST /training/remote-workout/activate, POST /training/remote-workout/deactivate, GET /training/remote-workout/status.

### Session Execution (Sprint 5-6) — KEY DIFFERENTIATOR
LiveSessionScreen: LIVE indicator + timer → exercise-by-exercise → quick input → rest timer → swap → end. PostSessionLogScreen: effort ⭐ → feedback → technique → areas to improve → next focus → submit. API: POST /scheduling/sessions/{id}/logs.

### Other
Client Progress (charts, photos, log for client, PDF report). Nutrition (MealPlanBuilder 7-day, view client log). Communication (ChatList, Chat, Broadcast, FormCheck video). Revenue (ClientPackages VIEW-ONLY — see remaining sessions per client, package expiry, low alerts. Trainer CANNOT create/edit/delete packages — that's done by gym staff. EarningsDashboard — sessions count, revenue generated). Analytics (RetentionInsights).

### My Ratings (Sprint 8-9)
**MyRatingsDashboardScreen** (/ratings): The trainer sees their **aggregated** rating (e.g., "4.7 ★") and how it trends month-over-month. They see their Coach of the Month badges. They see their anonymized testimonials. They do **NOT** see individual ratings, who gave them, or individual comments — only the aggregated score and gym-manager-curated testimonials. API: GET /gamification/trainers/{id}/public-profile.

---

# PART 6: GYM STAFF MOBILE (gymapp_gym — Flutter)

> Mobile/tablet companion. Desktop web = PART 8.

## Navigation: Dashboard | Members | Trainers | Classes | More (Exercises, Billing, Announcements, Check-in, Staff, Settings)

**Sign-in and roles**: staff (`role=staff`) and owners (`role=owner`) only. The per-gym tier comes from the access token's `staffSubRole` claim (Owner / Manager / Receptionist; owner principals carry `staffRole=owner`) and drives every permission check — the default matrix in docs/01 "Staff Roles & Permissions". The More sheet, Settings tiles and the revenue KPI are hidden when the role lacks the permission. Owners are scoped to a gym at login via `POST /identity/select-gym` (picker when they own several). An account with no gym gets a "not linked to a gym" state with sign-out reachable.

Known gaps between this section and the shipped app are tracked in `DISCOVERED_ISSUES.md` → "Staff Mobile App — MobAI QA Run (2026-10-10)".

GymDashboardScreen: 3 KPIs → revenue chart → peak hours → top classes → at-risk members → Coach of the Month spotlight.
MemberListScreen: search + filter → list cards → detail + payment history.
TrainerListScreen: card grid → assign to member.
ClassListScreen: create/edit → bookings + attendance.
CheckInKioskScreen: FULL-SCREEN TABLET — gym logo → **TWO CHECK-IN MODES side by side**:
  **Mode 1 (left): QR Scan** — camera viewfinder, member scans QR from their Trainee App → system reads QR token → member photo + "Welcome to [Gym Name], Ahmed!" → auto-reset 5s.
  **Mode 2 (right): Member Number** — large numpad (0-9 buttons, big touch targets, backspace, enter) → member types their number (e.g., "7824") → system looks up by memberNumber + gymId → same welcome screen with photo + name → auto-reset 5s.
  If member not found → "Member not found. Please check your number or ask staff." If membership expired → "Your membership has expired. Please visit the front desk to renew." Block entry.
  Both modes produce the same check-in record. The member number mode is for members who don't have the app or forgot their phone — very common in Egyptian gyms.
CreateAnnouncementScreen, StaffManagementScreen (add staff with role: Owner/Manager/Receptionist — preset permissions for Free/Starter/Pro, editable matrix for Business+ with `custom_permissions` feature gate).

### Billing (Sprint 3-6)

**ExerciseLibraryScreen**: Browse global 200+ exercises (search, filter by muscle group/equipment). Read-only for global exercises. **CustomExerciseScreen** (feature gated: `custom_exercise_library` — Business only): Add gym-specific exercises with name, muscle groups, equipment, demo photo/video. These are only visible to this gym's trainers.

**RecordPaymentScreen**: Member search → amount → method (Cash/Card/Online/Bank Transfer) → purpose (Membership/Package) → confirm → receipt generated (REC-{GymCode}-{YYYYMMDD}-{SEQ}). Mobile: the member is entered by member number and resolved server-side (`GET /gyms/{id}/members?search=`, leading zeros ignored). A Package payment also takes the trainer and session count, which the API requires to create the package it pays for.

**PackageTemplatesScreen** (gym_owner/gym_staff only): List of package templates the gym offers. Each card: Name + Sessions + Price EGP + Price/Session + Active toggle. **"Create Template"** button → Name, Session Count, Total Price, Expiry (optional). Gym owner sets the pricing — trainers cannot access this screen.

**AssignPackageScreen** (gym_staff): "Assign Package" → select member (search) → select trainer (dropdown) → select package template (dropdown, shows price) → confirm → toast "Package assigned". This is done at the reception desk when member pays.

**ActivePackagesScreen** (API: `GET /billing/packages?status=Active`, gym-wide via `X-Gym-Id`): Table of all assigned packages. Client, Trainer, Package Name, Remaining/Total with progress bar, Status (Active/Low/Exhausted). Filter by trainer, status.

### Gamification Management (Sprint 8-9)
**CoachLeaderboardScreen**: All trainers ranked by monthly normalized score. Shows: rank, trainer name+photo, score, sessions this month, unique clients. Highlights Coach of the Month with badge. API: GET /gamification/trainers/leaderboard.

**TrainerRatingsDetailScreen** (gym manager only): Select a trainer → see ALL individual ratings (stars + full comments + client name + session date). This is the **private management view** — trainers and members never see this. API: GET /gamification/trainers/{id}/ratings.

**ModerateTestimonialsScreen**: Review client comments → approve/reject for display on trainer's public profile (comments are anonymized — client name removed). API: POST /gamification/trainers/{id}/testimonials.

**MemberLeaderboardScreen**: View all 3 member leaderboards (Attendance, Weight Loss, Consistency). Same view members see but with management context.

### Settings (Sprint 2-3)
**GymProfileScreen**: Edit gym name, description, phone, email, address, operating hours (Mon-Sun with open/close time + "24/7" toggle).

**BrandingScreen** (feature gated: `gym_branding` — Pro+ plan): Upload gym logo (camera or gallery), pick primary color and secondary color from palette. **Live preview** showing how the Trainee App and Trainer App will look with the gym's branding — gym name + logo + colors in a phone-shaped mockup. Save → applies to all members and trainers of this gym immediately. Feature gated: if gym is on Free/Starter, show the branding section locked with "Upgrade to Pro to brand your app" prompt.

**AmenitiesScreen**: Toggle checklist (Parking, Showers, Lockers, WiFi, Sauna, Pool, Juice Bar, Pro Shop) + custom add.

**NotificationSettingsScreen**: Configure which auto-notifications members receive (session reminders, expiry warnings, announcements). Toggle per type.

---

# PART 7: NEXT.JS ARCHITECTURE (Apps 4-5)

## Tech Stack
Next.js 14 (App Router) | TypeScript | Tailwind CSS | shadcn/ui | Recharts | TanStack Table | TanStack Query | next-auth (JWT) | Zod | Axios

## Monorepo (Turborepo)
```
gymapp-web/
├── packages/@gymapp/ui/          # SHARED: data-table, stat-card, chart-container, status-badge, search-input, date-range-picker, confirm-modal, slide-over-panel, empty-state, loading-skeleton, page-header + hooks (use-api, use-auth, use-debounce) + types (user, gym, member, trainer, workout, session, payment, notification, api-response) + lib (api-client, api-endpoints, utils)
├── apps/gym-staff/               # GYM STAFF WEB → Next.js app/ directory
├── apps/admin/                   # ADMIN DASHBOARD → Next.js app/ directory
├── turbo.json
└── package.json
```

## TypeScript Types (match backend DTOs)
```typescript
interface User { id: string; email: string; firstName: string; lastName: string; role: 'trainee'|'trainer'|'gym_staff'|'gym_owner'|'platform_admin'; adminRole?: 'super_admin'|'admin'|'support'|'analyst'; status: string; gymMemberships: GymMembership[]; }
interface ApiResponse<T> { data: T; errors: string[]; meta?: { totalCount: number; pageSize: number; currentPage: number; totalPages: number; }; }
interface Gym { id: string; name: string; address: Address; branding: GymBranding; gymCode: string; status: string; activeMemberCount: number; }
```

## Data Fetching: TanStack Query for all server state. useQuery for reads, useMutation for writes with optimistic updates + toast.

---

# PART 8: GYM STAFF WEB PAGES (Next.js)

## Sidebar: Dashboard | Members | Trainers | Classes | Exercises | Billing | Announcements | Analytics | Gamification | Check-Ins | Staff | Settings

**Dashboard** (/): 3 KPIs (Active Members, New/mo, Retention%) → revenue area chart → peak hours heatmap → top classes → at-risk members → **Coach of the Month spotlight card** (trainer photo, name, score, badge).

**Members** (/members): Full data table — search + filter (status/plan/date) → Name, Email, Phone, Plan, Joined, Last Check-in, Trainer, Status, Actions. Sortable, paginated 20/page, export CSV, bulk actions. Click → /members/[id] tabs: Profile, Payments, Sessions, Progress.

**Trainers** (/trainers): Card grid + table toggle → detail (clients, schedule, performance, commission). **Each trainer card shows aggregated star rating (e.g., "4.7 ★")**.

**Classes** (/classes): Calendar + list toggle → create modal → detail with bookings + attendance + waitlist.

**Exercises** (/exercises): Browse global exercise library (200+, read-only, search + filter by muscle group/equipment/category). Custom Exercises section (feature gated: `custom_exercise_library` — Business only): add gym-specific exercises with name, muscle groups, equipment, demo media. Custom exercises visible only to this gym's trainers.

**Billing** (/billing): Tabs — Payments table | Plans CRUD | Packages. Record Payment modal. Revenue summary.

**Analytics** (/analytics): Member trend → revenue → new vs churned → peak hours → class ranking → trainer utilization → retention cohort.

**Gamification** (/gamification): 3 sub-pages:
- **/gamification/leaderboards**: View all 3 member leaderboards (Attendance, Weight Loss, Consistency). See opted-out members count. Toggle display periods (this month, last month).
- **/gamification/coach-rankings**: All trainers ranked by normalized monthly score. Detailed table: Rank, Trainer, Avg Stars, Sessions, Unique Clients, Normalized Score. Highlight Coach of the Month. Click trainer → individual ratings detail (stars + full comments + client names + dates — **gym manager eyes only**).
- **/gamification/testimonials**: Queue of pending client comments to moderate. Approve → anonymized and displayed on trainer's public profile. Reject → discarded. Edit → modify before publishing.

**Announcements** (/announcements): List + create (title, rich text, image, category, target, schedule).

**Check-Ins** (/check-ins): Today's log table → trends chart → manual check-in.

**Staff** (/staff): Table + add modal (role: Owner/Manager/Receptionist with preset permissions). Role permission matrix view: read-only for Free/Starter/Pro (shows fixed defaults), editable for Business+ (feature: `custom_permissions`).

**Settings** (/settings): Gym profile + branding (logo, colors, preview) + plans + notifications.

---

# PART 9: ADMIN DASHBOARD PAGES (Next.js)

## Sidebar: Dashboard | Gyms (+Onboarding) | Users | Subscriptions | Plan & Features | Platform Analytics | Churn Analytics | Reports | Support | Content | Settings

**Dashboard** (/): 4 KPIs (Gyms 142, Members 8420, MRR EGP 284K, Trainers 286) → Revenue Trend → Gym Growth → Recent Registrations → At-Risk Gyms → Top Gyms → Support donut. + **Plan distribution donut chart** (how many gyms on each plan).

**All Gyms** (/gyms): Filterable table → Name, Location, Owner, Members, Trainers, Revenue/mo, Status, **Current Plan (badge: Free/Starter/Pro/Business)**, Registered, Actions. Paginated 20/page. **Filter by plan** dropdown.

**Gym Detail** (/gyms/[id]): Header (shows current plan badge prominently) → 6 tabs: Overview | Members | Trainers | Revenue | **Subscription & Features** | Settings.

- **Subscription & Features tab** (NEW — admin manages this gym's plan and features):
  - Current plan card: plan name, price, started date, next billing date, member/trainer/staff usage vs limits with progress bars
  - **Change Plan** button → modal: select new plan → confirm → immediate effect → gym owner notified
  - **Feature Overrides** table: Full list of all 32 features. Each row: Feature Name | Plan Default (YES/NO based on their current plan) | **Override Toggle** (admin can enable/disable ANY feature for THIS specific gym regardless of their plan). Override column shows: "Default" (grey, follows plan), "Force ON" (green, enabled even if plan doesn't include it), "Force OFF" (red, disabled even if plan includes it).
  - Use case: Give a gym a free trial of Pro features while they're on Starter. Or disable a buggy feature for one gym without affecting others.
  - **Member Limit Override**: Input field to set a custom member cap (overrides plan default). Useful for gyms negotiating a special deal.
  - **History log**: Table showing all plan changes and feature overrides with date, admin who made the change, and reason.

**Onboarding** (/gyms/onboarding): Pending queue → Approve modal now includes: **assign initial plan** (dropdown: Free/Starter/Pro/Business), generate gym code, welcome email toggle. The assigned plan determines which features the gym gets from day 1.

**Users** (/users): Table → Name, Email, Role badge, Gym(s), Joined, Last Active, Status, Actions.

**Subscriptions** (/subscriptions): This is the **platform-wide subscription management** page.

- **Plan Cards** (top): 4 default cards (Free, Starter, Pro, Business) + any custom plans. Each shows: price, member limit, gym count, MRR, visibility status (🟢 Public / 🟡 Hidden / ⏰ Time-Limited). **"Edit Plan" button** on each → opens plan editor. **"Hide"/"Show" quick toggle** per card.
- **Plan Editor Modal** (when clicking Edit Plan):
  - Plan name, prices (monthly/annual), all limits (members/trainers/staff/storage)
  - **Visibility**: Public toggle (show on signup page) + Time-Limited toggle → Available From / Available Until date pickers
  - Note: "Gyms already on this plan keep their subscription even when hidden"
  - Plan name (editable)
  - Monthly price (EGP)
  - Annual price (EGP, auto-suggests 10 months)
  - Member limit
  - Trainer limit
  - Staff limit
  - **Feature checklist**: All 32 features with toggles. Admin turns each ON/OFF for this plan. This is the MASTER control — changes here affect ALL gyms on this plan (unless they have a per-gym override).
  - "Save Changes" → confirmation modal: "This will affect X gyms currently on this plan. Continue?" → saves → all affected gyms' cached features invalidated in Redis.
- **Create New Plan** button → same editor modal but blank. Use case: Create a "Ramadan Special" plan or a custom tier for a specific deal.
- **Subscription Table** (below cards): All gyms with their subscription details. Columns: Gym Name, Plan, Members (current/limit), MRR, Payment Status (Paid/Overdue/Grace), Started, Next Billing, Feature Overrides Count, Actions (Change Plan, Suspend, Send Reminder).
- **Revenue Breakdown**: MRR by plan tier (stacked bar chart). Annual projection. Revenue growth trend.

**Plan & Features** (/plans) — **NEW PAGE: Master Feature Configuration**

This is the **central control panel** for all plans and features. Three views:

- **Feature Matrix View** (default): Interactive grid. Rows = all features. Columns = all plans (Free → Business). Each cell = toggle switch (ON/OFF). Admin can click any toggle to enable/disable a feature for a plan. Changes apply to all gyms on that plan immediately. Shows "X gyms affected" count per column.

  ```
  ┌─────────────────────┬──────┬─────────┬──────┬──────────┬────────────┐
  │ Feature             │ Free │ Starter │ Pro  │ Business │
  ├─────────────────────┼──────┼─────────┼──────┼──────────┼────────────┤
  │ Member Management   │  ✅  │   ✅    │  ✅  │    ✅    │     ✅     │
  │ QR Check-in         │  ✅  │   ✅    │  ✅  │    ✅    │     ✅     │
  │ Workout Builder     │  ❌  │   ✅    │  ✅  │    ✅    │     ✅     │
  │ Nutrition Tracking  │  ❌  │   ❌    │  ✅  │    ✅    │     ✅     │
  │ Leaderboards        │  ❌  │   ❌    │  ✅  │    ✅    │     ✅     │
  │ White Label         │  ❌  │   ❌    │  ❌  │    ✅    │     ✅     │
  │ Multi-Location      │  ❌  │   ❌    │  ❌  │    ❌    │     ✅     │
  │ ...                 │      │         │      │          │            │
  └─────────────────────┴──────┴─────────┴──────┴──────────┴────────────┘
  Gyms on plan:           10      58        32       10          2
  ```

- **Per-Plan View**: Select a plan → see all features for that plan + which gyms are on it + any per-gym overrides. Edit limits (members, trainers, staff) and pricing.

- **Override Audit View**: Table of ALL per-gym feature overrides across the platform. Columns: Gym Name, Feature, Override (Force ON/Force OFF), Set By (admin name), Set Date, Reason. "Remove Override" action to revert to plan default. This is important for auditing — you need to know which gyms have special deals.

**Platform Analytics** (/analytics): 6 KPIs → member growth → gym registrations → revenue by city → top 10 gyms → plan distribution → DAU → **feature adoption chart** (which features are most/least used across the platform — helps you decide what to include in lower plans to drive upgrades).

**Churn Analytics** (/analytics/churn): 3 KPIs → churn funnel → **churn by plan** (which plan has highest churn? Free is expected, but if Starter churns more than Pro, your Starter value prop is weak) → at-risk table with risk scores + sparklines.

**Support** (/support): Table → slide-over detail (conversation, reply, internal notes, status change).

**Content** (/content): 2 tabs — Exercise Library (global CRUD) + App Notices (platform announcements).

**Settings** (/settings): Platform settings, payment gateway keys, email/SMS config, security (2FA, timeout, IP whitelist).

---

# PART 10: LOVABLE-TO-PRODUCTION MIGRATION GUIDE

## What Lovable Gives (keep): Visual design, layouts, shadcn/ui patterns, Recharts configs, responsive breakpoints, mock data structure.

## What Claude Agent Replaces:
| Lovable | Production |
|---------|-----------|
| Monolithic App.tsx | Next.js App Router app/ directory |
| Inline mock data | TanStack Query + real API calls |
| No auth | next-auth JWT from /identity/login |
| No TypeScript types | Typed interfaces matching backend DTOs |
| No error handling | Error boundaries + toasts + retry |
| No loading states | Skeleton loaders from @gymapp/ui |
| localStorage state | TanStack Query cache |
| Components in one file | Extracted to @gymapp/ui shared library |

## Migration Steps
1. Create Turborepo: apps/gym-staff + apps/admin + packages/@gymapp/ui
2. Extract reusable components from Lovable → @gymapp/ui
3. Create TypeScript types matching backend DTOs
4. Create API client with Axios + JWT + X-Gym-Id interceptors
5. Per page: keep JSX + Tailwind → replace mock with useQuery → add loading/error → wire mutations
6. Add next-auth + middleware.ts for route protection. Admin Dashboard routes check `adminRole` from JWT: `super_admin` sees everything, `admin` sees everything except Settings/Admin Team, `support` sees read-only views (no edit/create/deploy buttons), `analyst` sees only Analytics + Churn pages.
7. Test against real backend API

---

# PART 11: AGENT ROUTING

| Agent | Deliverable | Framework | Focus |
|-------|-----------|-----------|-------|
| **Flutter-Shared** | gymapp_core package | Flutter | PART 3 |
| **Flutter-Trainee** | Trainee mobile app | Flutter | PART 4 |
| **Flutter-Trainer** | Trainer mobile app | Flutter | PART 5 |
| **Flutter-GymMobile** | Gym staff mobile + tablet kiosk | Flutter | PART 6 |
| **Web-Shared** | @gymapp/ui component library | Next.js/React | PART 7 |
| **Web-GymStaff** | Gym staff web app | Next.js | PART 8 |
| **Web-Admin** | Admin dashboard | Next.js | PART 9 |
| **Web-Migration** | Lovable → production restructure | Next.js | PART 10 |

**Execution**: Lovable prototype → Shared packages → all apps in parallel → migration agent restructures Lovable to production.

## Definition of Done (Flutter)
All screens with API integration, Riverpod + GoRouter, loading/error/empty states, pull-to-refresh, Arabic + English RTL, 60fps, widget tests.

## Definition of Done (Next.js)
All pages with TanStack Query + real API, TypeScript (zero any), JWT auth with middleware, data tables (sort/filter/paginate/export CSV), responsive (1440px+ desktop, 768px+ tablet), skeletons + error boundaries + toasts, dark mode, keyboard accessible.
