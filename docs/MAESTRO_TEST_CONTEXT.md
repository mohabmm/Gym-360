# GymApp — Maestro Test Context

> **Purpose:** Single document to feed Claude (project context) so it can generate, expand, and maintain Maestro edge-case test flows for the 3 Flutter apps.
> **Audience:** Claude project / QA engineer
> **Stack:** Flutter 3 monorepo (Melos) × 3 apps + .NET 8 backend
> **Last refreshed:** 2026-05-12

---

## 1. Platform snapshot

GymApp is a B2B SaaS gym management platform for the Egyptian market. One .NET 8 modular monolith backend + **three Flutter mobile apps** + two Next.js web apps. Multi-tenant, schema-per-gym hard isolation. EGP only. Arabic-first + English (full RTL).

**Mobile apps under test:**

| App | Path | Android applicationId | Primary persona |
|---|---|---|---|
| Trainee | `gymapp-mobile/apps/gymapp_trainee` | `com.example.gymapp_trainee` | Gym member |
| Trainer | `gymapp-mobile/apps/gymapp_trainer` | `com.example.gymapp_trainer` | Personal trainer (gym-approved) |
| Gym Staff | `gymapp-mobile/apps/gymapp_gym` | `com.example.gymapp_gym` | Owner / manager / receptionist (also kiosk mode) |

Backend dev URL: `http://localhost:8080` (Docker). API restart: `docker compose build gymapp-api && docker compose up -d gymapp-api`.

---

## 2. Demo credentials (seeded by `db/seed.sql`)

Password for every seeded user: **`123456`**.

| Role | Email |
|---|---|
| Trainee | `trainee@demo.local` |
| Trainer | `trainer@demo.local` |
| Gym staff (manager) | `staff@demo.local` |
| Receptionist | `receptionist@demo.local` |
| Owner | `owner@demo.local` |
| Platform admin | `admin@demo.local` |

Gym code (linking flow): `GYM001`. Sample member number (kiosk): `1001`.

---

## 3. Domain rules that shape edge cases

These are the non-obvious rules. Edge tests should poke at every boundary.

### Identity / auth
- **Min age 16+**. 13-15 needs parental consent. <13 blocked entirely. (Q16)
- **Account lockout**: 5 failed logins / 15min → 30min lock. 3 lockout cycles → must reset via email OTP. (Q30)
- **OTP rate limit**: max 3 / hour. 5-min expiry.
- **JWT 1h, refresh 30d** sliding. Kiosk never times out. Staff Web 30min idle, Admin Web 15min idle. (Q29)
- **One-Gym principal rule**: Trainees/trainers/staff have ≤1 Active `GymMembership`. Only owners are multi-gym. (Q3, Rule 11)
- **Account deletion** is soft for 30d → hard purge. Re-register inside 30d → restore via OTP. (Q15)

### Scheduling
- **Cancellation window is configurable per gym** (default 24h). Late cancel = session deducted, no extra fee. (Q5)
- **No-show suspension** is configurable: threshold/window/action. Default 3-in-30d → notify+flag. (Q5)
- **Booking requires active trainer assignment** (no open-trainer booking). (Q11)
- **Max sessions/day per trainer**: soft warn at 8, confirm at 10, no hard cap. (Q12)
- **Recurring sessions** auto-create up to 12 weeks.
- **No auto holiday block** in MVP. (Q14)

### Billing
- **EGP only**. Symbol `ج.م` (Ar), `EGP` (En). (Q10, Q34c)
- **Refunds disallowed in MVP** — payments can be `Voided` (status), no money flow back. Voided payments excluded from revenue. (Q9)
- **Session-package decrement**: each completed session decrements; alerts at 3 remaining, 1 remaining, 0.
- **Late-cancel = decrement** (trainer earned the slot).

### Training & remote mode
- **Remote Workout Mode is OFF by default**, per-client toggle. Trainee app shows "Today's Workout" card only when ON.
- Trainer can swap exercise mid-live-session.
- **Workout-log visibility**: member + assigned trainer only. Managers see counts, not detail. (Q21)

### Progress / privacy
- **Progress photos**: visible only to trainer the member shared with; encrypted at rest. First photo upload requires extra in-context confirmation. (Q22, Q25)
- **Image limits**: 10MB hard limit client-side; server downscales to 2048px long-edge, JPEG q80, strip EXIF. Profile pics ≤512px. (Q27)
- **Chat content**: only the two participants — no management view. Only metadata. (Q23)

### Gamification
- **Leaderboard opt-out**: member-controlled; opted-out users vanish from public rankings but see own private rank.
- **Weight-loss leaderboard** ranks by **% of body weight lost**, not absolute kg. Requires start+end weight in the month.
- **Trainer ratings** are private — trainer sees **aggregated only** (≥3 unique clients required to qualify). Individual ratings visible only to gym manager. Anonymized testimonials need manager approval.

### Multi-tenancy / sections
- **Gym sections** (`mens` / `womens` / `mixed`) are gym-enabled. Member section validated against enabled set. Trainer-member section compatibility enforced on booking. (Q31)
- **Seasonal hours** (e.g., Ramadan) override default hours within date range. (Q32)

### Offline mode
- Trainee/Trainer apps queue writes (workout logs, body metrics, session logs); 7-day local retention; **last-writer-wins by record ID**. Kiosk requires online. (Q26)

### Feature gating (plans)
- Free (≤25 members) / Starter / Pro / Business. Every gated handler calls `IsFeatureEnabledAsync`. UI should show upgrade prompts not blank screens.
- Examples: `data_import` Pro+, `multi_staff` Pro+, `custom_permissions` Business, `cash_reconciliation` Phase 2.

### PDPL (Egypt)
- Consent tracked at registration (data_processing, marketing). Photo upload needs `photo_sharing` consent.
- Data export ZIP (JSON + PDF + photos) within 24h via signed URL (24h TTL).

---

## 4. App map — screens & navigation

### 4.1 Trainee app (`gymapp_trainee`)

Bottom nav (shell): Home · Workouts · Progress · Schedule · More.

Key screens (Dart files in `apps/gymapp_trainee/lib/features/*/presentation/screens/`):
- `auth/login_screen.dart`, `register_screen.dart`, `consent_screen.dart`, `profile_setup_screen.dart`, `link_gym_screen.dart`
- `home/home_screen.dart`
- `workouts/exercise_library_screen.dart`, `ad_hoc_workout_screen.dart`, `guided_workout_screen.dart`, `log_workout_screen.dart`, `workout_history_screen.dart`
- `progress/progress_dashboard_screen.dart`, `log_weight_screen.dart`, `log_measurements_screen.dart`, `progress_photos_screen.dart`
- `scheduling/upcoming_sessions_screen.dart`, `book_session_screen.dart`, `session_detail_screen.dart`, `class_schedule_screen.dart`, `session_feedback_screen.dart`, `book_first_session_gate.dart`
- `nutrition/nutrition_log_screen.dart`, `meal_plan_screen.dart`, `water_tracker_screen.dart`
- `communication/chat_screen.dart`, `announcements_screen.dart`
- `gamification/leaderboard_screen.dart`, `rate_trainer_screen.dart`, `trainer_public_profile_screen.dart`
- `checkin/checkin_screen.dart`
- `membership/membership_screen.dart`, `settings/settings_screen.dart`

### 4.2 Trainer app (`gymapp_trainer`)

Bottom nav: Home · Clients · Schedule · Workouts · More.

Key screens:
- `auth/login_screen.dart`, `register_screen.dart`, `pending_approval_screen.dart`
- `home/trainer_home_screen.dart`
- `clients/client_roster_screen.dart`, `client_detail_screen.dart` (5 tabs: Overview/Progress/Sessions/Workouts/Notes), `client_notes_screen.dart`, `onboarding_form_screen.dart`, `assign_workout_screen.dart`
- `scheduling/calendar_screen.dart`, `availability_screen.dart`, `booking_requests_screen.dart`, `recurring_session_screen.dart`, `leave_management_screen.dart`
- `workouts/exercise_manager_screen.dart`, `workout_builder_screen.dart`, `program_builder_screen.dart`, `template_library_screen.dart`
- `sessions/live_session_screen.dart`, `post_session_log_screen.dart`
- `nutrition/meal_plan_builder_screen.dart`, `client_nutrition_view_screen.dart`
- `communication/chat_list_screen.dart`, `chat_screen.dart`, `broadcast_screen.dart`, `form_check_screen.dart`
- `gamification/my_ratings_dashboard_screen.dart`
- `revenue/earnings_dashboard_screen.dart`, `client_packages_screen.dart`
- `settings/profile_screen.dart`, `settings_screen.dart`

### 4.3 Gym Staff app (`gymapp_gym`)

Bottom nav: Dashboard · Members · Classes · Check-In · More.

Key screens:
- `auth/login_screen.dart`
- `dashboard/gym_dashboard_screen.dart`
- `members/member_list_screen.dart`, `add_member_screen.dart`, `member_detail_screen.dart`, `edit_member_screen.dart`
- `checkin/check_in_kiosk_screen.dart`, `manual_check_in_screen.dart`, `check_in_log_screen.dart`
- `classes/class_list_screen.dart`, `create_class_screen.dart`, `class_detail_screen.dart`
- `billing/package_templates_screen.dart`, `assign_package_screen.dart`, `active_packages_screen.dart`, `record_payment_screen.dart`, `revenue_dashboard_screen.dart`
- `trainers/trainer_list_screen.dart`, `trainer_detail_screen.dart`, `pending_trainer_approval_screen.dart`
- `staff/staff_list_screen.dart`, `add_staff_screen.dart`, `permission_matrix_screen.dart`
- `announcements/announcement_list_screen.dart`, `create_announcement_screen.dart`
- `exercises/exercise_library_screen.dart`, `custom_exercise_screen.dart`
- `gamification/coach_leaderboard_screen.dart`, `member_leaderboard_screen.dart`, `trainer_ratings_detail_screen.dart`, `moderate_testimonials_screen.dart`
- `settings/gym_profile_screen.dart`, `gym_sections_screen.dart`, `seasonal_hours_screen.dart`, `session_policies_screen.dart`, `amenities_screen.dart`, `kiosk_pin_screen.dart`, `notification_settings_screen.dart`, `branding_screen.dart`

---

## 5. Maestro target conventions

- **Element identification preference order:** semantic `text`, then visible `text:` substring, then `id` (Flutter `Semantics` label or `Key`). The apps were not built with Maestro in mind, so most flows must lean on visible text. When a screen has duplicate text, scope with `containerId` or `tapOn: { text: "X", index: 0 }`.
- **Arabic locale**: many strings differ in Arabic. Pin English first (`Settings → Language → English`) at flow start, OR provide Arabic variants.
- **Network**: backend must be reachable from the device/emulator. iOS sim → `localhost:8080`. Android emulator → `10.0.2.2:8080`. Real device → host LAN IP. The trainer/trainee mobile apps point at the configured API host in `lib/core/network/api_client.dart`.
- **Reset state between flows**: `clearState` at start; for kiosk flows, also `clearKeychain` to drop kiosk PIN. Long-running suites should re-seed via `scripts/reset_demo.sh` between runs.
- **Test directory layout**: `tests/maestro/{trainee,trainer,staff}/NN_*.yaml` + `tests/maestro/config.yaml` for `appId`/env vars.

### Common flow primitives

```yaml
# Login (trainee/trainer/staff identical structure)
- launchApp
- assertVisible: "Login"
- tapOn: "Email"
- inputText: ${EMAIL}
- tapOn: "Password"
- inputText: "123456"
- tapOn: "Login"
- assertVisible:
    text: "Home"
    timeout: 8000
```

```yaml
# Force English locale (idempotent)
- tapOn: "More"
- tapOn: "Settings"
- tapOn: "Language"
- tapOn: "English"
- back
- back
```

---

## 6. Edge-case taxonomy (use this when expanding to 100+ cases)

1. **Boundary inputs** — empty, max length, leading/trailing spaces, unicode, RTL chars, emoji, zero, negative, max numeric, decimal precision.
2. **State transitions** — pending → active, active → frozen → active, soft-deleted → restore.
3. **Concurrency** — same action twice fast (debounce), two devices same account, offline queue then sync.
4. **Network failure** — offline launch, mid-flow disconnect, slow 3G, server 500, 401 mid-session (token expired).
5. **Time-based** — DST boundary, cross-midnight booking, Ramadan-hour activation day, JWT expiry mid-flow, OTP expiry, session policy window edge.
6. **Multi-tenant** — gym A user trying gym B route, owner switching gyms mid-flow, section mismatch.
7. **Authorization** — receptionist vs manager vs owner permission denial paths; trainer attempting cross-trainer data.
8. **Feature-gating** — Free-plan blocked feature → upgrade prompt; Pro feature on Starter denied.
9. **PDPL / consent** — consent not granted, consent withdrawn mid-session, photo without `photo_sharing` consent.
10. **Localization** — language toggle preserves state; RTL layout no overflow; numeric input in Arabic-Indic digits.
11. **Push & background** — notification taps deep-link; app launched cold from FCM payload.
12. **Resource limits** — 10MB image, 100-character names, very long chat threads, leaderboard with 0 entries.

Every test case in `edge_cases_catalog.md` is tagged with one or more of these.

---

## 7. How to ask Claude (project) for more

Drop this entire file plus `edge_cases_catalog.md` into your Claude project. Then:

- "Generate 30 more edge cases targeting the Trainer app's recurring-session screen, focusing on time-based and concurrency edges."
- "Convert these YAML flows to use Maestro's `runFlow:` shared sub-flows for login. Output the refactored files."
- "I changed the trainee booking screen — re-validate cases 9-13 against new copy: [paste new strings]."

Claude has full context: domain rules (§3), screen map (§4), conventions (§5), taxonomy (§6).
