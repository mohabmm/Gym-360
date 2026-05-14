# GymApp Platform — Complete Features Guide

> **Audience**: Manual QA Testers & Investors
> **Purpose**: Understand what the entire system does, who uses it, and how every feature works — in plain language, no code.

---

# WHAT IS GYMAPP?

GymApp is a platform that helps local gyms run their entire business digitally. Instead of paper records, WhatsApp groups, and Excel sheets, gym owners get a branded app ecosystem that connects their members, trainers, and staff — all in one place.

**The business model**: We sell the platform to gym owners on a monthly subscription. They get branded apps with their gym logo and colors. Their members and trainers download the app and use it for free.

**What makes us different**: Most gym software handles either gym management (billing, check-ins) OR personal training (workout plans, progress tracking) — but never both together. GymApp combines them. Our unique feature is "dual-session logging" where both the trainer AND the member log their perspective of the same training session, creating a complete picture that no competitor offers.

---

# WHO USES THE SYSTEM?

| User | What They Use | What They Do |
|------|-------------|-------------|
| **Gym Member (Trainee)** | Mobile app (iOS/Android) | Track workouts, log body progress, book sessions with trainer, follow meal plans, chat with trainer |
| **Personal Trainer** | Mobile app (iOS/Android) | Manage clients, build workout plans, run live sessions, provide feedback, track earnings |
| **Gym Staff** (owner, manager, receptionist) | Mobile app (on the floor) + Website (at the desk) | Manage members, record payments, schedule classes, handle check-ins, view analytics |
| **Platform Admin** (our team) | Website | Onboard new gyms, manage subscriptions, monitor platform health, handle support tickets |

---

# APP 1: TRAINEE APP (Mobile — iOS & Android)

*The gym member downloads this app when they join a gym that uses GymApp.*

## 1.1 Registration & Onboarding

**Sign Up**: Member creates an account with email or phone number. They receive a verification code (OTP) via SMS or email. They can also sign up instantly with Google or Apple account.

**Profile Setup**: After signing up, the member fills in their fitness profile — name, photo, date of birth, gender, height, current weight, fitness level (beginner/intermediate/advanced), any health conditions or injuries, and their fitness goals (weight loss, muscle gain, general fitness, endurance). This information helps their trainer understand their starting point.

**Join a Gym**: The member enters a 6-character gym code (given to them at the front desk) or scans a QR code posted at the gym. This links their account to that specific gym. They can be a member of multiple gyms.

## 1.2 Home Screen

When the member opens the app, they see a personalized dashboard showing:
- Today's assigned workout — **this card only appears if the trainer has activated "Remote Workout Mode" for this member** (see section 1.3 below). In normal day-to-day operation, the member trains in person with their trainer and this card is hidden.
- Their next scheduled session with their trainer (date, time, trainer name)
- Quick stats: current workout streak (consecutive days), latest weight, today's water intake
- A mini chart showing how many workouts they did this week

## 1.3 Workout Tracking

**How training normally works**: In the standard workflow, the member trains **in person** with their trainer at the gym. The trainer coaches them through exercises face-to-face. The member does NOT need to follow a guided workout on their phone — the trainer is right there. After the session, both the trainer and member log the session (see section 1.5).

**Remote Workout Mode (Optional — Trainer Activated)**: Sometimes the trainer wants the member to follow a structured workout independently. This can happen for many reasons — the trainer is on leave, the member is traveling, the trainer wants to assign supplementary home workouts, or any other reason the trainer decides. When the trainer activates "Remote Workout Mode" for a specific client:
- The "Today's Workout" card appears on the member's home screen
- The member can tap "Start Workout" and get a **guided experience** — exercise by exercise, with demo images/videos uploaded by the trainer, target sets/reps/weight, rest timer, and fields to log actual performance
- The trainer can upload exercise images and videos so the member knows exactly how to perform each movement
- The member is notified when remote mode is activated ("Your trainer has enabled remote workouts for you") and when it's deactivated

**Important**: Remote Workout Mode is **off by default** for all members. It is entirely the trainer's decision to activate it, and they can turn it on or off for each client independently at any time.

**Log Own Workout (Always Available)**: Regardless of whether Remote Workout Mode is on or off, the member can always log a workout on their own by browsing the exercise library (200+ exercises searchable by muscle group or equipment), picking exercises, and entering their sets, reps, and weights. This is always available.

**Workout History**: A calendar view and list showing every workout they've completed — both guided (remote) workouts and ad-hoc self-logged workouts, with date, duration, and muscle groups trained. They can tap any past workout to see the full details.

## 1.4 Progress Tracking

**Weight Logging**: The member enters their weight at any time. The app shows a line chart of their weight trend over 30, 90, or 365 days. They can toggle between kilograms and pounds.

**Body Measurements**: The member (or their trainer) can log body measurements — chest, waist, hips, arms, thighs, calves, neck. Each measurement has its own trend chart showing how it changes over time. Units toggle between centimeters and inches.

**Body Fat Percentage**: Can be entered manually or calculated using the Navy method (from waist and neck measurements). Shown with a visual indicator of the healthy range.

**Progress Photos**: The member takes front, side, and back photos at different dates. The app has a side-by-side comparison slider so they can visually see their transformation. Photos are private by default — the member chooses whether to share them with their trainer.

**Progress Dashboard**: A single screen showing all progress at a glance — weight trend chart, body fat change, key measurements with +/- deltas, latest progress photo, workouts this month, and current streak.

## 1.5 Trainer Sessions

**View Schedule**: The member sees a list of their upcoming sessions with their trainer — date, time, duration, and what the session will focus on (e.g., "Upper Body Strength").

**Book a Session**: The member can see their trainer's available time slots and book a new session. They receive a confirmation notification and can add it to their phone calendar.

**Cancel a Session**: The member can cancel a session. If they cancel less than 24 hours before, it's flagged as a "late cancellation" and counts against them.

**Post-Session Feedback**: After a session, the member gets a notification prompting them to log their experience — they rate the session 1-5 stars, log their perceived effort (scale 1-10), write personal notes about how they felt, and report any discomfort or pain. This feedback is visible to the trainer alongside the trainer's own session notes — this is the "dual-logging" that makes GymApp unique.

## 1.6 Nutrition

**Daily Food Log**: The member logs their meals (breakfast, lunch, dinner, snacks) by searching a food database or entering manually. Each food item shows calories, protein, carbs, and fat. The app shows daily totals vs their target.

**Water Tracking**: Quick-add buttons (250ml, 500ml, custom) to log water intake throughout the day. A visual ring shows progress toward their daily target (default 2 liters, adjustable).

**View Trainer's Meal Plan**: If the trainer has created a meal plan, the member sees it here — daily meals with portions, calories, and macros. They can mark meals as completed and generate a grocery list.

## 1.7 Gym Features

**Check-In System**: Members check in using either their QR code from the app OR their Member Number (e.g., "7824") on a numpad at the kiosk. Both methods show the member's photo, name, and a "Welcome!" message. The member number is assigned automatically when they join the gym — useful for members who don't have smartphones or forgot their phone. Staff can also do manual check-in by searching the member's name or number.

**Class Schedule**: The member views the gym's weekly group class schedule (e.g., CrossFit at 7 AM, Yoga at 6 PM), sees how many spots are left, and can book or cancel a spot. If a class is full, they join the waitlist and get automatically moved in when a spot opens.

**Membership Status**: The member sees their current plan, expiry date, and a renewal reminder 7 days before expiry.

**Gym Announcements**: A feed of news from the gym — schedule changes, holiday hours, special offers, new classes. Push notifications alert the member.

## 1.8 Communication

**Chat with Trainer**: One-on-one messaging with their assigned trainer. They can send text, photos (e.g., a form check video), and voice notes. Messages appear in real-time.

**Push Notifications**: The member receives alerts for: session reminders (24h and 1h before), trainer feedback posted, membership expiring, gym announcements, workout streak milestones, new workout assigned.

## 1.9 Settings

Language toggle (Arabic/English with full right-to-left support), unit preferences (kg/lbs, cm/inches), notification preferences (toggle each type on/off), privacy settings (who sees progress photos), dark/light mode, account deletion.

---

# APP 2: TRAINER APP (Mobile — iOS & Android)

*Personal trainers download this to manage their clients and run sessions professionally.*

## 2.1 Registration & Profile

The trainer registers with email/phone. Their account must be **approved by the gym owner** before they can use it — this prevents unauthorized people from acting as trainers. Once approved, they build their professional profile: bio, photo, specializations (e.g., strength training, weight loss, rehabilitation), certifications with expiry dates, years of experience, languages spoken. This profile is visible to gym members.

## 2.2 Home Dashboard

When the trainer opens the app, they see:
- Today's schedule — a list of all sessions today with time, client name, and session focus
- Quick stats: pending booking requests, unread messages, total active clients
- Quick action buttons: Start Session, Add Client Note, Build Workout
- Monthly summary: total sessions completed and estimated earnings this month

## 2.3 Client Management

**Client List**: All assigned clients in a searchable, filterable list. Each client shows their photo, name, last session date, next session date, and fitness goal. The trainer can tag clients (e.g., "weight loss", "muscle gain", "rehab") for organization.

**Client Profile**: Tapping a client opens a detailed view with 5 tabs:
- **Overview**: Goal, health conditions, fitness level, quick stats (weight, body fat, sessions this month, streak), current program, next session
- **Progress**: Weight and body fat trend charts, body measurement history, progress photos gallery (if the member shared them)
- **Sessions**: History of all past sessions with both the trainer's notes and the member's feedback side by side
- **Workouts**: Currently assigned workout/program and history of past assignments
- **Notes**: Private notes only the trainer can see (e.g., "Tends to skip leg day — needs motivation", "Bad knee — avoid deep squats")

**Client Onboarding Form**: When a new client is assigned, the trainer sends them an intake questionnaire covering: goals, injuries, medical conditions, exercise experience, availability, dietary restrictions, and a Physical Activity Readiness Questionnaire (PAR-Q).

## 2.4 Scheduling

**Set Availability**: The trainer defines which hours they're available each day of the week (e.g., Sunday-Thursday 8 AM-6 PM, Friday off). They set a default session duration (30/45/60 minutes) and buffer time between sessions (e.g., 15 minutes).

**Manage Bookings**: When a member requests a session, the trainer receives a notification and can accept, reject, or suggest a different time. Confirmed sessions appear on the calendar.

**Calendar View**: A daily or weekly calendar showing all sessions color-coded by type (personal training, assessment, follow-up). Tapping a session shows details and options.

**Recurring Sessions**: For regular clients, the trainer sets up a weekly recurring session (e.g., "Every Monday and Wednesday at 10 AM") that auto-creates sessions for up to 12 weeks.

**No-Show Tracking**: If a client doesn't show up, the trainer marks it as a no-show. The app tracks no-show counts per client.

**Leave Management**: The trainer can block dates for vacation or sick days. All affected clients are automatically notified and their sessions are flagged for rescheduling.

## 2.5 Workout Programming

**Exercise Library**: The trainer browses 200+ exercises with demo images, searchable by muscle group and equipment. They can also add their own custom exercises with their own demo videos — these are visible only to their gym's members.

**Workout Builder**: The trainer creates a workout by dragging exercises into a list, setting the number of sets, target reps, suggested weight, and rest time for each. They can group exercises into supersets (two exercises done back-to-back) or circuits. The workout shows an estimated duration.

**Templates**: Any workout can be saved as a reusable template. The trainer builds a library of templates (e.g., "Push Day", "Leg Day", "HIIT Cardio") and quickly assigns them to different clients with minor customizations.

**Multi-Week Programs**: The trainer maps workouts across weeks to create a structured program (e.g., "8-Week Hypertrophy Program"). Each week can have a theme (e.g., Week 1-4: Volume, Week 5-8: Intensity). The program auto-delivers the right workout to the client each day — but only if Remote Workout Mode is active for that client.

**Assign to Client**: The trainer assigns a workout or program to a client for their **own reference and session planning**. In normal operation (in-person training), the client does NOT see the assigned workout in their app — the trainer uses it as their session plan and coaches the client through it face-to-face.

**Remote Workout Mode (Per-Client Toggle)**: This is the key feature that bridges in-person and remote training. On any client's profile, the trainer sees a toggle: **"Enable Remote Workouts"**. When the trainer activates it:
- They provide an optional reason (free text — e.g., "I'm on leave until April 20", "Client traveling to Alexandria", "Supplementary home program on off-days")
- The client's app immediately shows the "Today's Workout" card and gains access to the guided workout experience with exercise images/videos
- The client receives a notification: "Your trainer has enabled remote workouts for you"
- The trainer can upload exercise demo images and videos so the client knows exactly how to perform each movement independently

When the trainer deactivates it:
- The guided workout features hide from the client's app
- The client is notified: "Remote workout mode has been turned off"
- The client can still log ad-hoc workouts from the exercise library as always

**Important for testers**: Remote Workout Mode is OFF by default for every client. The trainer activates it independently per client, for any reason, at any time. It is NOT tied to the trainer's leave status — though going on leave is one common reason to use it. Each client's remote mode status is independent of other clients.

## 2.6 Live Session Mode

**During a Session**: The trainer opens "Live Session" mode which shows the planned workout step by step. For each exercise, they quickly enter the client's actual reps and weight. A rest timer helps pace the session. If equipment is unavailable or an exercise doesn't work, the trainer can swap it for an alternative mid-session. The entire session duration is timed.

**Post-Session Log**: After the session ends, the trainer fills in their professional feedback:
- Client effort rating (1-5)
- Professional feedback on performance and technique
- Technique notes (what to improve)
- Areas to focus on next
- Suggested focus for the next session
- Attendance confirmation

This log is automatically synced to the client's account. The client can then add their own perspective (rating, how they felt, any discomfort). **Together, these two logs create a complete session record — this dual-logging is what no competitor offers.**

## 2.7 Client Progress Monitoring

**View Client Progress**: The trainer sees each client's weight, body fat, and measurement trends on charts. They can view progress photos side by side.

**Log Measurements for Client**: During assessments, the trainer takes measurements (e.g., with calipers or InBody scanner) and enters them directly into the client's profile. These entries are tagged as "trainer-measured" to distinguish from self-reported data.

**Monthly Report**: The trainer can generate a PDF progress report for a client showing their month's data — metrics, workout summary, session count, key achievements. Branded with the gym's logo.

## 2.8 Nutrition

**Create Meal Plans**: The trainer builds a 7-day meal plan with meals, portions, calories, and macros. They can create templates and assign them to clients.

**View Client's Food Log**: The trainer sees what the client is eating and can provide dietary feedback.

## 2.9 Communication

**Chat**: Direct messaging with each client — text, photos, voice notes, videos. Useful for form checks between sessions.

**Broadcast**: Send a message to all clients or a tagged group at once (e.g., "Holiday schedule change" to all clients).

**Form Check**: Client sends a video of an exercise. Trainer responds with feedback.

## 2.10 Revenue & Business

**Session Packages**: Gym owner creates package templates (e.g., "10 Sessions for EGP 5,000"). Gym staff assigns a package to a member and links it to their trainer when the member pays at the desk. Sessions auto-decrement when completed. Alert when running low (3 left, 1 left). Trainers can ONLY view their clients' remaining sessions — they cannot create or price packages.

**Earnings Dashboard**: Total sessions this month, calculated revenue, month-over-month comparison, breakdown by client. Export as CSV or PDF.

## 2.11 Certification Tracker

Trainers add their certifications with expiry dates. The app sends reminders 30 and 7 days before expiry so they stay compliant.

## 2.12 My Ratings

The trainer can see their **aggregated** star rating (e.g., "4.7 ★ from 24 reviews this month") and how it trends over time. They can see their Coach of the Month badges and any anonymized testimonials that the gym manager has approved for their public profile. However, the trainer **cannot** see individual ratings — they don't know which client gave which score or which comments came from whom. This protects honest client feedback.

---

# GAMIFICATION & RATINGS (Shared Feature Across Apps)

*This feature spans the Trainee App, Trainer App, and Gym Staff apps.*

## Member Leaderboards

Each gym has 3 leaderboards that update monthly (with live daily snapshots):

**Attendance Leaderboard**: Who attended the most sessions this month? Members are ranked by total sessions attended. Ties are broken by streak length. This encourages members to show up consistently.

**Weight Loss Leaderboard**: Who lost the most weight this month? Ranked by **percentage of body weight lost** (not absolute kg) — so it's fair whether you're 60kg or 120kg. For example, losing 2kg from 80kg = 2.5% scores higher than losing 2kg from 100kg = 2.0%. Only members who logged their weight at both the start and end of the month qualify.

**Consistency Leaderboard**: Who hit their training targets? If a trainer assigned a member 4 sessions per week and the member completed all 4, they score 100%. A member with 10 out of 12 assigned sessions scores 83%. This rewards members who follow their program.

**Privacy / Opt-Out**: Members can choose to hide themselves from all leaderboards in their privacy settings. If they opt out, their name disappears from the public rankings — but they can still see their own position privately. This is important because not every member wants their weight or attendance visible to others.

## Trainer Ratings (Private Feedback System)

**How it works**: After each training session, the member receives a notification asking them to rate their trainer. They give 1-5 stars and can write an optional comment. This feedback is **completely private** — it is visible ONLY to the gym owner and managers. The trainer does NOT see individual ratings. Other members do NOT see other members' ratings. This ensures members give honest feedback without worrying about awkwardness.

**What the gym manager sees**: A detailed view of every rating — which client gave what score, what they wrote, for which session. This is a management tool to monitor trainer quality, identify issues early, and reward top performers.

**Monthly Coach Score**: At the end of each month, a normalized score is calculated for each trainer. It's not a simple average — it uses a weighted formula that accounts for:
- Average star rating (60% weight)
- Session consistency — did the trainer complete all scheduled sessions on time? (20% weight)
- Client retention — are the trainer's clients staying or leaving? (20% weight)

This normalization prevents unfair comparisons — a trainer with 5 clients isn't compared the same way as one with 30 clients. A minimum of 3 unique client ratings is required to qualify.

**Coach of the Month**: The trainer with the highest normalized monthly score is crowned "Coach of the Month." This is announced to all gym members via notification and displayed prominently in the app. It motivates trainers and helps members choose a good trainer.

**Public Trainer Profile**: When a member browses trainers (e.g., to request assignment), they see the trainer's **aggregated** star rating (e.g., "4.7 ★") and anonymized testimonials — short excerpts from client feedback with names removed, curated by the gym manager. The manager reviews submitted comments, approves the good ones (anonymized), and rejects inappropriate ones.

**Coach Leaderboard**: The gym owner/manager sees a ranked list of all trainers by monthly score, along with session counts, unique client counts, and detailed score breakdowns. This helps with performance reviews, bonus decisions, and identifying trainers who need support.

---

# APP 3: GYM STAFF MOBILE APP (iOS, Android & Tablet)

*Used by gym owners, managers, and receptionists on the gym floor for quick tasks.*

## 3.1 Dashboard

A quick overview: active member count, new members this month, retention rate, revenue this month, peak hours chart, top classes, and a list of at-risk members (haven't visited in 10+ days).

## 3.2 Member Management

Search and browse all gym members. View their profile, membership status, payment history, and assigned trainer. Add new members, edit details, freeze or deactivate memberships.

## 3.3 QR Check-In Kiosk

**This is the star feature of the mobile app.** A tablet at the gym entrance runs in kiosk mode — full-screen with the gym's logo. **Two check-in options side by side**: (1) "Scan Your QR Code" with camera viewfinder, and (2) "Enter Your Member Number" with a large numpad. When a member scans QR or types their number (e.g., "7824"), the screen shows their photo, name, and a "Welcome!" message, then auto-resets after 5 seconds. If their membership is expired, the check-in is blocked with a renewal message. The member number option is critical for Egyptian gyms where many members don't have smartphones or prefer not to use the app.

## 3.4 Class Management

Create and manage group classes (name, trainer, schedule, capacity). View bookings per class. Manage the waitlist. Cancel a class with automatic notification to all booked members.

## 3.5 Announcements

Create and publish announcements to all members or specific groups. Attach images. Schedule for future dates. Push notifications sent automatically.

## 3.6 Trainer Management

View all trainers, approve new trainer registrations, assign trainers to members, view each trainer's schedule and performance (sessions completed, client count, average rating).

## 3.7 Staff Management

Add staff accounts with roles (owner, manager, receptionist). Each role has preset permissions — receptionist can check in members and record payments but cannot see financial reports or manage trainers. Manager has full access except billing/subscription settings. Pro plan allows up to 10 staff accounts, Business is unlimited. On Business plan, the gym owner can customize the permission matrix to change what each role can access (feature: `custom_permissions`).

---

# APP 4: GYM STAFF WEB APP (Desktop Browser)

*Used by gym owners and managers sitting at a desk. Same features as the mobile app but optimized for heavy data work — large tables, detailed reports, bulk operations.*

## 4.1 Dashboard

Same KPIs as mobile but with richer charts: revenue trend over 6 months, peak hours heatmap (hour × day of week), class popularity ranking, at-risk member alerts, and a recent activity feed showing the last 10 events at the gym.

## 4.2 Member Management (Full Desktop Experience)

A powerful data table with columns for name, email, phone, plan, joined date, last check-in, status — all sortable and filterable. Search by name, filter by plan or status, export the full list as CSV. Click any member to see their complete profile, payment history, session history, and assigned trainer. Record payments, freeze memberships, or deactivate accounts.

## 4.3 Billing & Payments

**Record Payments**: When a member pays at the desk (cash or card), staff records it here. A receipt is auto-generated (e.g., REC-GLD-20260408-001) that can be printed or sent to the member.

**Membership Plans**: Create and manage plans (e.g., Basic EGP 300/mo, Premium EGP 800/mo) with descriptions and included features.

**Session Packages**: View all PT session packages — who bought what, how many sessions remain, which packages are about to run out.

**Revenue Dashboard**: Total revenue this month, broken down by memberships vs PT packages. Comparison with last month. Revenue by trainer.

## 4.4 Analytics

The desktop analytics page is richer than mobile: member growth over 12 months, revenue trends, retention rate trends, peak hours analysis, most popular classes, trainer utilization rates (how busy each trainer is), member churn rate, and check-in frequency distribution.

## 4.5 Classes, Announcements, Staff, Check-ins, Settings

Same features as the mobile app but presented in spacious desktop layouts with full data tables, bulk actions, and detailed views.

## 4.6 Gym Branding & Settings

The gym owner uploads their logo, sets primary and secondary brand colors, and sees a live preview of how the member app will look. They also set operating hours, contact info, address (with map), amenities, and social media links.

---

# APP 5: ADMIN DASHBOARD (Desktop Browser — Internal)

*This is OUR platform management tool. Gym owners never see this — only our team uses it.*

## 5.1 Platform Dashboard

A bird's-eye view of the entire platform: total gyms (142), total members across all gyms (8,420), total trainers (286), monthly recurring revenue (EGP 284,000), growth trends, new gym registrations this month, at-risk gyms that might churn.

## 5.2 Gym Onboarding

When a new gym owner signs up, their application appears here in a pending queue. Our team reviews it, approves or rejects it. On approval, the gym gets assigned a subscription plan, a unique gym code is generated, and a welcome email is sent automatically.

## 5.3 All Gyms Management

A master list of every gym on the platform. For each gym we can see: name, location, owner, member count, trainer count, monthly revenue, subscription plan, status. We can drill into any gym to see detailed metrics, change their plan, suspend their account, or configure their feature access.

## 5.4 User Management

Search any user across the platform (members, trainers, staff, owners). View their profile, suspend accounts, handle data deletion requests. For debugging, we can impersonate a user to see what they see.

## 5.5 Subscription Management

Manage the subscription tiers we offer to gyms: Free (≤25 members), Starter (≤75, EGP 499/mo), Pro (≤200, EGP 999/mo), Business (≤500, EGP 1,999/mo). Create custom promotional plans (e.g., "Ramadan Special" with a time window). Hide or show plans on the signup page instantly — gyms already on a hidden plan keep their subscription. Track which gyms are on which plan, payment status, overdue accounts.

## 5.6 Platform Analytics

Aggregate analytics across ALL gyms: member growth platform-wide, gym registration trends, revenue by city (Cairo, Alexandria, Giza), top-performing gyms, subscription plan distribution, daily active users, and which features are most used (workout logging, check-in, progress tracking, etc.).

## 5.7 Churn Analytics

Identify gyms at risk of cancelling their subscription. Risk indicators: gym hasn't logged in for 14+ days, member count declining, revenue dropping. We can trigger re-engagement emails or assign an account manager to reach out.

## 5.8 Support Tickets

A ticket system for handling gym owner issues — billing problems, feature requests, bug reports, onboarding help. Each ticket tracks the conversation, priority, assignment, and resolution status.

## 5.9 Content Management

**Exercise Library**: Manage the global database of 200+ exercises that all gyms see. Add new exercises with names, descriptions, muscle group tags, equipment, and demo images.

**Feature Flags**: Control which features are available on each subscription tier. For example, "Nutrition Tracking" is only available on Pro and Business plans. We toggle features on/off per tier using a simple switch matrix.

**App Notices**: Send platform-wide announcements to all gym owners (e.g., maintenance windows, new features, pricing changes).

## 5.10 Platform Settings

Configure the platform: default language, currency (EGP), payment gateway keys (Stripe/Fawry), email/SMS provider settings, admin security settings (two-factor authentication, session timeout, IP whitelist).

---

# CROSS-CUTTING FEATURES (All Apps)

## Authentication & Security
- Email/phone registration with OTP verification
- Google and Apple sign-in
- JWT-based authentication with token refresh
- Role-based access control (each role sees only what they're allowed to)
- Multi-gym support (one user can belong to multiple gyms)
- Soft account deletion with 30-day data retention

## Notifications
- Push notifications on iOS and Android for: session reminders, trainer feedback, membership expiry, gym announcements, workout streak milestones, new workout assigned, booking confirmations
- In-app notification center with read/unread status and badge count
- User-configurable notification preferences with quiet hours

## Localization
- Arabic (primary) and English
- Full right-to-left (RTL) layout support for Arabic
- Egyptian cultural context: EGP currency, local food database for nutrition tracking, Arabic names throughout

## White-Label Branding
- Each gym gets their own branded experience — their logo, colors, and name appear throughout the member and trainer apps
- The gym's branding is set by the owner in the Gym Staff Web settings page
- Members feel like they're using "their gym's app", not a generic platform

---

# FEATURE SUMMARY BY NUMBERS

| Category | Count |
|----------|-------|
| Total features across all apps | 88 |
| Trainee app features | 28 |
| Trainer app features | 33 |
| Gym staff features | 15 |
| Admin dashboard features | 12 |
| API endpoints | 80+ |
| Pre-seeded exercises | 200+ |
| Supported languages | 2 (Arabic + English) |

---

# FOR TESTERS: KEY SCENARIOS TO VALIDATE

1. **Member Onboarding Flow**: Register → OTP verify → profile setup → link gym code → see home screen with gym branding
2. **Normal Training Flow (No Remote Mode)**: Trainer assigns workout (for own reference) → client's app does NOT show "Today's Workout" card → trainer and client meet in person → trainer uses Live Session mode → both log session → dual logs visible
3. **Remote Workout Mode — Activation**: Trainer goes to client profile → toggles "Enable Remote Workouts" ON → enters reason ("On leave until April 20") → client receives notification "Remote workouts enabled" → client's home screen now shows "Today's Workout" card → client can follow guided workout with trainer's exercise images/videos → client logs performance → workout appears in history
4. **Remote Workout Mode — Deactivation**: Trainer returns → toggles OFF for client → client receives notification "Remote workout mode turned off" → "Today's Workout" card disappears from home screen → client can still log ad-hoc workouts from exercise library
5. **Remote Workout Mode — Multiple Clients**: Trainer activates remote mode for Client A (reason: "traveling") and Client B (reason: "home supplement") but NOT Client C → verify Client A and B see guided workouts, Client C does not → trainer deactivates for Client A only → verify Client A loses guided mode, Client B still has it
6. **Session Dual-Logging**: Trainer books session → member confirms → session happens → trainer logs feedback → member logs their perspective → both logs visible on session detail
7. **Payment & Package Flow**: Staff creates membership plan → records payment → member status becomes active → check-in allowed → membership approaches expiry → notification sent → renewal
8. **Session Package Decrement**: Gym owner creates 10-session package template → staff assigns to client + links trainer → each completed session decrements count → alert at 3 remaining → alert at 1 remaining → package exhausted
9. **Class Booking & Waitlist**: Staff creates class with 20 capacity → 20 members book → 21st member goes to waitlist → one member cancels → waitlisted member auto-promoted → notification sent
10. **Progress Photo Comparison**: Member uploads front photo in January → uploads another in March → opens comparison slider → sees side-by-side transformation
11. **Check-In Kiosk**: Tablet in kiosk mode → member scans QR → photo + name shown → "Welcome!" → auto-reset → member types number instead → same result → try with expired membership → blocked with renewal message
12. **Gym Onboarding (Admin)**: New gym registers → appears in admin pending queue → admin approves → gym code generated → gym owner receives welcome email → gym appears as active
13. **Churn Detection**: Gym stops logging in → 14 days pass → appears in admin churn analytics → admin sends re-engagement email
14. **Member Leaderboard — Attendance**: 3 members attend different session counts in a month → leaderboard ranks them correctly → member with most sessions is #1
15. **Member Leaderboard — Opt Out**: Member toggles "Show on Leaderboard" OFF in settings → their name disappears from public leaderboard → they can still see their own position privately → they toggle back ON → name reappears
16. **Weight Loss Leaderboard — Fair Ranking**: Member A (80kg → 78kg = 2.5% lost) should rank above Member B (100kg → 98kg = 2.0% lost) despite both losing 2kg → verify percentage-based ranking
17. **Trainer Rating — Privacy**: Member rates trainer 3 stars with comment "Wasn't focused today" → trainer opens their app → trainer sees aggregated rating updated but does NOT see this individual review → gym manager opens trainer ratings page → sees the full review with client name, stars, comment, date
18. **Coach of the Month**: Month ends → system calculates normalized scores for all trainers → trainer with highest score gets Coach of the Month badge → all gym members receive notification → badge appears on trainer's public profile
19. **Testimonial Moderation**: Client submits comment "Amazing trainer, changed my life!" → appears in gym manager's moderation queue → manager approves it → comment appears (anonymized, no client name) on trainer's public profile → another client submits inappropriate comment → manager rejects → it never appears publicly
20. **Trainer Rating Normalization**: Trainer A has 30 clients, avg 4.2 stars. Trainer B has 5 clients, avg 4.8 stars → normalized score should account for this difference → verify the formula doesn't let a trainer with very few clients dominate unfairly
21. **Data Import from Odoo/Daftra/Excel (Pro+)**: On a Free or Starter plan, the gym owner opens `/data-import` → sees an upgrade prompt → upgrades to Pro → the page loads → downloads the template → fills the `Members`, `Payments`, `Sessions`, `InBody` tabs (all keyed by the gym's existing member number) → uploads the `.xlsx` → the page polls job status → members appear in the member list, payments in the billing list, session packages are assigned to trainers, InBody entries show on each member's progress timeline → rows with invalid phone numbers are skipped and appear in the downloadable `errors.csv`

---

# FOR INVESTORS: MARKET OPPORTUNITY

**The problem**: Local gyms in Egypt and the MENA region manage their operations with paper records, WhatsApp, and Excel spreadsheets. Western gym software (Mindbody at $149-699/mo, Glofox at $110-600/mo) is too expensive and not culturally adapted.

**Our solution**: An affordable, Arabic-first, all-in-one platform at $0-99/mo that combines gym management + personal training tools — something no competitor does in one product.

**Market size**: The Middle East fitness app market was valued at ~$400M in 2024 and is projected to reach $1.1B+ by 2033 (11.9% CAGR). Egypt specifically has a rapidly growing gym sector driven by a young, health-conscious population and expanding middle class.

**Revenue model**: Monthly SaaS subscription from gym owners — Free (≤25 members), Starter EGP 499/mo (≤75), Pro EGP 999/mo (≤200), Business EGP 1,999/mo (≤500).

**Competitive advantage**: Only platform that combines gym operations + deep personal training tools (dual-session logging, workout programming, progress tracking) + white-label branding + Arabic-first — at 5-10× lower price than Western competitors.
