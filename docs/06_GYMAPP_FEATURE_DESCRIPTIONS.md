# GymApp Platform — Feature Description Report

> Every feature explained in simple language.
> For: QA Testers, Investors, and Stakeholders.
> Date: April 2026
> Total Features: 26
> Plans: Free | Starter (EGP 499) | Pro (EGP 999) | Business (EGP 1,999)

---

## How to Read This Report

Each feature is described with four fields:

- **What It Does** — A plain-language description of the feature and how it works.
- **Why It Matters** — The business value. Why the gym owner should care.
- **Who Uses It** — Which user role interacts with this feature and in which app.
- **Available On** — Which subscription plan(s) include this feature.

Features are grouped by the lowest plan that includes them.

---

## CORE FEATURES (Free Plan)

*Available to all gyms — no subscription required*

### 1. Member Management

**Category:** Core | **Feature Code:** `member_management`

| Field | Description |
|-------|-------------|
| **What It Does** | Add, edit, search, and manage all gym members in one place. Each member has a profile with their name, phone, email, photo, membership plan, join date, and status (active, expired, frozen). |
| **Why It Matters** | Replaces paper notebooks and Excel sheets. Staff can instantly find any member, see their status, and take action — no more flipping through pages. |
| **Who Uses It** | Gym staff (receptionist, manager, owner) use the Gym Staff App and Web to manage members. Members see their own profile in the Trainee App. |
| **Available On** | **All Plans** |

### 2. Check-in System (QR + Member Number)

**Category:** Core | **Feature Code:** `qr_checkin`

| Field | Description |
|-------|-------------|
| **What It Does** | Members check in at the gym entrance using TWO methods: (1) QR Code — open the app, show the QR code, kiosk tablet scans it. (2) Member Number — type their assigned number (e.g., '7824') on a numpad at the kiosk. Both methods show the member's photo, name, and 'Welcome!' message, then auto-reset after 5 seconds. A third option — Manual Check-in — lets staff search by name or number. Every check-in is logged with a timestamp and method. In kiosk mode, the tablet runs full-screen with the gym's logo — no staff needed, even at 3 AM in a 24/7 gym. |
| **Why It Matters** | Automated attendance tracking with a fallback for members who don't have smartphones or forgot their phone. The gym owner knows exactly who came, when, and how — even at off-peak hours. Also blocks entry for expired memberships. |
| **Who Uses It** | Members scan QR from the Trainee App or type their member number at the kiosk. Staff can do manual check-in from the Gym Staff App. The kiosk tablet runs the Gym Staff App in kiosk mode. |
| **Available On** | **All Plans** |

### 3. Exercise Library (Global)

**Category:** Core | **Feature Code:** `exercise_library_global`

| Field | Description |
|-------|-------------|
| **What It Does** | A pre-loaded database of 200+ exercises covering all muscle groups (chest, back, shoulders, arms, legs, core) and equipment types (barbell, dumbbell, machine, bodyweight, cable). Each exercise has a name, description, step-by-step instructions, target muscle groups, and equipment needed. |
| **Why It Matters** | Trainers don't start from scratch. Members can browse exercises and learn proper form. Standardizes exercise naming across the platform. |
| **Who Uses It** | Trainers use it to build workouts. Members browse it to log their own workouts or learn exercises. |
| **Available On** | **All Plans** |

### 4. 1 Trainer Account

**Category:** Core | **Feature Code:** `single_trainer`

| Field | Description |
|-------|-------------|
| **What It Does** | The Free plan includes one trainer account. This trainer can manage clients, build workouts, and log sessions — but the gym cannot add more trainers without upgrading. |
| **Why It Matters** | Lets small gyms or personal training studios get started with one trainer at zero cost. Encourages upgrading when the gym hires more trainers. |
| **Who Uses It** | The gym's primary trainer uses the Trainer App. |
| **Available On** | **Free only** |

## STARTER PLAN FEATURES (EGP 499/month)

*Everything in Free, plus these features*

### 5. Unlimited Trainers

**Category:** Trainers | **Feature Code:** `unlimited_trainers`

| Field | Description |
|-------|-------------|
| **What It Does** | Add as many trainer accounts as needed. Each trainer gets their own app with their own client list, schedule, workout library, and session history. Trainers can only see their own clients — not other trainers' clients. |
| **Why It Matters** | Most gyms have 3-10+ trainers. Removing the limit lets the entire training team go digital without restrictions. |
| **Who Uses It** | Gym owner adds trainers from the Staff App/Web. Each trainer downloads the Trainer App. |
| **Available On** | **Starter + Pro + Business and above** |

### 6. Workout Builder & Templates

**Category:** Training | **Feature Code:** `workout_builder`

| Field | Description |
|-------|-------------|
| **What It Does** | Trainers create workouts by picking exercises from the library, setting sets, reps, weight, and rest time for each. Exercises can be reordered by dragging. Supersets (two exercises done back-to-back) and circuits are supported. Any workout can be saved as a reusable template (e.g., 'Push Day', 'Leg Day') and assigned to clients. |
| **Why It Matters** | Trainers build their workout library once and reuse it across clients. Saves hours of repetitive planning. Makes the trainer look professional. |
| **Who Uses It** | Trainers build workouts in the Trainer App. Clients see assigned workouts in the Trainee App (only if Remote Workout Mode is activated). |
| **Available On** | **Starter + Pro + Business and above** |

### 7. Session Logging (Dual)

**Category:** Training | **Feature Code:** `session_logging`

| Field | Description |
|-------|-------------|
| **What It Does** | After each personal training session, BOTH the trainer and the member log their perspective independently. The trainer logs: exercises performed, client effort rating (1-5), professional feedback, technique notes, areas to improve, and focus for next session. The member logs: rating (1-5 stars), perceived effort (RPE 1-10), personal notes, and any discomfort. |
| **Why It Matters** | This is GymApp's key differentiator. No competitor does dual-logging. It creates a complete picture of every session from both perspectives. The gym owner can see if trainers are doing their job. Members feel heard. |
| **Who Uses It** | Trainers log from the Trainer App after each session. Members log from the Trainee App (prompted via notification). |
| **Available On** | **Starter + Pro + Business and above** |

### 8. Progress Tracking (Weight, Measurements)

**Category:** Progress | **Feature Code:** `progress_tracking`

| Field | Description |
|-------|-------------|
| **What It Does** | Members (or their trainer) log body metrics: weight, body fat percentage, and 9 body measurements (chest, waist, hips, left/right arm, left/right thigh, left/right calf, neck). Each metric has a trend chart showing change over 30, 90, or 365 days. Supports both metric (kg/cm) and imperial (lbs/inches) units. |
| **Why It Matters** | Members see their progress visually. When they see the line going down (weight) or up (muscle), they stay motivated and keep their membership. Reduces churn. |
| **Who Uses It** | Members log their own data in the Trainee App. Trainers can log measurements for clients (tagged as 'trainer-measured') in the Trainer App. |
| **Available On** | **Starter + Pro + Business and above** |

### 9. Progress Photos

**Category:** Progress | **Feature Code:** `progress_photos`

| Field | Description |
|-------|-------------|
| **What It Does** | Members take front, side, and back photos at different dates. The app provides a side-by-side comparison slider to visually show body transformation over time. Photos are private by default — the member chooses whether to share them with their trainer. |
| **Why It Matters** | Visual proof of transformation is the most powerful motivator. Members who take progress photos are significantly more likely to stay long-term. Trainers can also use (with permission) transformation photos for marketing. |
| **Who Uses It** | Members upload photos in the Trainee App. Trainers view shared photos in the Trainer App. |
| **Available On** | **Starter + Pro + Business and above** |

### 10. Class Management

**Category:** Classes | **Feature Code:** `class_management`

| Field | Description |
|-------|-------------|
| **What It Does** | Create and manage group fitness classes (e.g., CrossFit, Yoga, HIIT, Spinning). Set trainer, day/time, duration, and capacity. Members can book spots from the Trainee App. When a class is full, members join a waitlist and are automatically promoted when a spot opens. Cancelling a class sends notifications to all booked members. |
| **Why It Matters** | Automates class scheduling and booking. No more WhatsApp messages to reserve spots. The waitlist feature maximizes attendance — no empty spots because of no-shows. |
| **Who Uses It** | Gym staff creates classes. Members book from the Trainee App. Trainers see their assigned classes. |
| **Available On** | **Starter + Pro + Business and above** |

### 11. Push Notifications

**Category:** Communication | **Feature Code:** `push_notifications`

| Field | Description |
|-------|-------------|
| **What It Does** | Send automated and manual push notifications to members' phones. Automated triggers include: session reminder (24h and 1h before), membership expiring (7 days before), trainer posted session feedback, new workout assigned, gym announcement published, streak milestone reached. Members can configure which notifications they receive and set quiet hours. |
| **Why It Matters** | Keeps members engaged. The session reminder alone reduces no-shows. The expiry reminder drives renewals without staff chasing people manually. |
| **Who Uses It** | The system sends automated notifications. Gym staff can send manual announcements. Members receive them on the Trainee App. |
| **Available On** | **Starter + Pro + Business and above** |

### 12. Payment Recording & Receipts

**Category:** Billing | **Feature Code:** `payment_recording`

| Field | Description |
|-------|-------------|
| **What It Does** | Staff records every payment at the desk — cash, card, or bank transfer. Each payment logs the amount, method, purpose (membership renewal, PT package, product), and who recorded it. A unique receipt number is auto-generated (e.g., REC-GLD-20260408-001). Revenue dashboard shows total income by period, broken down by memberships vs PT packages. |
| **Why It Matters** | Eliminates billing leakage. Every pound is tracked. The gym owner sees revenue data instead of guessing. Receipts add professionalism. |
| **Who Uses It** | Gym staff records payments from the Staff App/Web. The gym owner views revenue reports. |
| **Available On** | **Starter + Pro + Business and above** |

### 13. Basic Analytics

**Category:** Analytics | **Feature Code:** `basic_analytics`

| Field | Description |
|-------|-------------|
| **What It Does** | Dashboard showing: active member count, new members this month, total revenue this month, check-in count, and basic trend charts (member growth over time, revenue over time). |
| **Why It Matters** | Gives the gym owner basic visibility into their business health. Simple enough for non-technical users. |
| **Who Uses It** | Gym owner and managers view analytics from the Staff Web App. |
| **Available On** | **Starter + Pro + Business and above** |

## PRO PLAN FEATURES (EGP 999/month)

*Everything in Starter, plus branding, nutrition, gamification, and deep analytics*

### 14. Gym-Branded Colors/Logo

**Category:** Branding | **Feature Code:** `gym_branding`

| Field | Description |
|-------|-------------|
| **What It Does** | The member-facing Trainee App and Trainer App show the gym's own logo and brand colors instead of the GymApp default. The gym owner uploads their logo and picks their primary/secondary colors from the Settings page. The branded appearance applies immediately to all members of that gym. |
| **Why It Matters** | Members feel like they're using 'their gym's app', not a generic platform. Increases perceived value and professionalism. Gym owners love seeing their brand on a mobile app. |
| **Who Uses It** | Gym owner sets branding in the Staff Web Settings. Members and trainers see the branded experience. |
| **Available On** | **Pro + Business and above** |

### 15. Nutrition Tracking

**Category:** Nutrition | **Feature Code:** `nutrition_tracking`

| Field | Description |
|-------|-------------|
| **What It Does** | Members log their daily meals (breakfast, lunch, dinner, snacks) by searching a food database or entering manually. Each food item shows calories, protein, carbs, and fat. Daily totals are calculated automatically. A water intake tracker lets members log glasses throughout the day with a visual progress ring toward their daily target (default 2 liters, adjustable). |
| **Why It Matters** | Nutrition is 70% of fitness results. Members who track food make better choices and see faster results — which means they stay longer. |
| **Who Uses It** | Members log food and water in the Trainee App. Trainers can view their client's food log in the Trainer App. |
| **Available On** | **Pro + Business and above** |

### 16. Meal Plans

**Category:** Nutrition | **Feature Code:** `meal_plans`

| Field | Description |
|-------|-------------|
| **What It Does** | Trainers create structured 7-day meal plans with specific meals, portions, calorie targets, and macronutrient breakdowns (protein/carbs/fat). Plans include alternatives for each meal so members have flexibility. Plans can be saved as templates and assigned to multiple clients. Includes a basic Egyptian food database (ful, koshari, grilled chicken, rice, etc.). |
| **Why It Matters** | Trainers can offer a complete fitness + nutrition package. Members get personalized nutrition guidance without needing a separate nutritionist app. |
| **Who Uses It** | Trainers build meal plans in the Trainer App. Members view their assigned plan in the Trainee App. |
| **Available On** | **Pro + Business and above** |

### 17. Member Leaderboards

**Category:** Gamification | **Feature Code:** `member_leaderboards`

| Field | Description |
|-------|-------------|
| **What It Does** | Three competitive leaderboards within each gym: Attendance (most sessions attended this month), Weight Loss (highest percentage of body weight lost — fair regardless of starting weight), and Consistency (highest completion rate of assigned training sessions). Leaderboards update daily. Members can opt out of appearing on the public leaderboard but can still see their own rank privately. |
| **Why It Matters** | Gamification drives engagement. Members compete with peers, which increases attendance and retention. The opt-out respects privacy for members who don't want their weight visible. |
| **Who Uses It** | Members view leaderboards in the Trainee App. Gym staff can view them in the Staff App/Web. |
| **Available On** | **Pro + Business and above** |

### 18. Trainer Ratings & Coach of the Month

**Category:** Gamification | **Feature Code:** `trainer_ratings`

| Field | Description |
|-------|-------------|
| **What It Does** | After each session, members privately rate their trainer (1-5 stars + optional comment). Individual ratings are visible ONLY to the gym owner/manager — trainers cannot see who gave which rating. At month-end, a normalized score is calculated per trainer using a weighted formula: average stars (60%) + session consistency (20%) + client retention (20%). The trainer with the highest score is crowned 'Coach of the Month'. Anonymized testimonials (client comments with names removed, curated by the gym manager) appear on the trainer's public profile. |
| **Why It Matters** | Gives gym management a data-driven way to evaluate trainer quality. Motivates trainers to perform well. Coach of the Month creates healthy competition. Members can choose trainers based on ratings. |
| **Who Uses It** | Members rate trainers in the Trainee App. Gym managers view detailed ratings in the Staff Web. Trainers see only their aggregated score in the Trainer App. |
| **Available On** | **Pro + Business and above** |

### 19. Advanced Analytics & Churn Alerts

**Category:** Analytics | **Feature Code:** `advanced_analytics`

| Field | Description |
|-------|-------------|
| **What It Does** | Deep analytics including: peak hours heatmap (hour x day of week showing traffic intensity), member retention rate trend, trainer utilization rates (how busy each trainer is), class popularity ranking, check-in frequency distribution, and churn alerts — members who haven't visited in 10+ days are flagged as 'at risk' with a notification to the gym manager. |
| **Why It Matters** | Churn alerts alone can save 2-3 members per month — each worth EGP 500-2,000. Peak hours data helps with staffing decisions. Trainer utilization shows who needs more clients. |
| **Who Uses It** | Gym owner and managers view advanced analytics in the Staff Web App. |
| **Available On** | **Pro + Business and above** |

### 20. Multi-Staff with Role Permissions

**Category:** Staff | **Feature Code:** `multi_staff`

| Field | Description |
|-------|-------------|
| **What It Does** | Add multiple staff accounts with different roles: Owner (full access), Manager (all except billing/subscription settings), Receptionist (check-in + member lookup + record payments only). Each role has a preset permission matrix that controls what they can see and do. Pro plan allows up to 10 staff, Business is unlimited. The gym owner assigns roles from the Staff Web or Mobile Settings. |
| **Why It Matters** | The receptionist doesn't need to see revenue reports. The manager doesn't need to change billing settings. Role-based access keeps data secure and the interface clean for each staff member. |
| **Who Uses It** | Gym owner assigns roles from the Staff Web or Mobile Settings. Each staff member logs in and sees only what their role allows. |
| **Available On** | **Pro + Business and above** |

### 21. Remote Workout Mode

**Category:** Training | **Feature Code:** `remote_workout_mode`

| Field | Description |
|-------|-------------|
| **What It Does** | Normally, trainers coach members in person — the member doesn't need a guided workout on their phone. But when the trainer activates 'Remote Workout Mode' for a specific client, that client's app shows a 'Today's Workout' card with a guided experience: exercise-by-exercise with demo images/videos, target sets/reps/weight, rest timer, and logging fields. The trainer can activate this for any reason — they're on leave, the client is traveling, or they want to assign supplementary home workouts. Each client's remote mode is independent. Default is OFF. |
| **Why It Matters** | Bridges in-person and remote training. The trainer doesn't lose the client when one of them can't be at the gym. Clients continue progressing even when apart. |
| **Who Uses It** | Trainers activate/deactivate per client in the Trainer App. Members see guided workouts in the Trainee App only when active. |
| **Available On** | **Pro + Business and above** |

## BUSINESS PLAN FEATURES (EGP 1,999/month)

*Everything in Pro, plus white-label, API, AI predictions, and custom permissions*

### 22. White-Label Full Branding

**Category:** Branding | **Feature Code:** `white_label_branding`

| Field | Description |
|-------|-------------|
| **What It Does** | Beyond custom colors and logo (Pro plan), the Business plan offers full white-label branding: the app splash screen shows the gym's logo, the app name can be customized, and all GymApp branding is removed. The member experience feels like a fully custom app built for that specific gym. |
| **Why It Matters** | Premium gyms want their own app identity, not a third-party platform. White-label makes GymApp invisible — members think the gym built the app themselves. |
| **Who Uses It** | Gym owner configures white-label in the Staff Web Settings. Members see the fully branded experience. |
| **Available On** | **Business and above** |

### 23. API Access

**Category:** Integration | **Feature Code:** `api_access`

| Field | Description |
|-------|-------------|
| **What It Does** | REST API access allows the gym to integrate GymApp data with other systems — their accounting software, CRM, marketing tools, or custom dashboards. API provides read/write access to members, payments, sessions, and check-ins with JWT authentication. |
| **Why It Matters** | Larger gyms often have existing systems they need to connect. API access lets them build custom workflows without manual data entry. |
| **Who Uses It** | The gym's IT team or a developer uses the API. Configured from the Staff Web Settings. |
| **Available On** | **Business and above** |

### 24. Custom Exercise Library

**Category:** Training | **Feature Code:** `custom_exercise_library`

| Field | Description |
|-------|-------------|
| **What It Does** | In addition to the 200+ global exercises, the gym can add their own custom exercises with their own demo images and videos. Custom exercises are only visible to that gym's trainers and members — not shared across the platform. |
| **Why It Matters** | Gyms with unique equipment (e.g., specialty machines, proprietary exercises, rehabilitation equipment) need exercises that don't exist in the global library. |
| **Who Uses It** | Gym staff adds custom exercises from the Staff Web. Trainers see them in the exercise picker when building workouts. |
| **Available On** | **Business and above** |

### 25. PDF Progress Reports

**Category:** Reporting | **Feature Code:** `pdf_reports`

| Field | Description |
|-------|-------------|
| **What It Does** | Generate a branded PDF progress report for any member showing their month's data: weight trend, body fat change, measurements, workout summary, session count, key achievements, and trainer notes. The PDF includes the gym's logo and colors. |
| **Why It Matters** | Trainers can give clients a professional monthly report — great for client retention and demonstrating value. Clients can share their progress with doctors or other professionals. |
| **Who Uses It** | Trainers generate reports from the Trainer App. The PDF can be sent to the member via the app or downloaded. |
| **Available On** | **Business and above** |

### 26. Churn Prediction AI

**Category:** Analytics | **Feature Code:** `churn_prediction`

| Field | Description |
|-------|-------------|
| **What It Does** | AI-powered risk scoring that analyzes member behavior patterns — declining check-in frequency, missed sessions, no progress logged, approaching membership expiry — and assigns a churn risk score (low/medium/high) to each member. High-risk members appear on a priority list with suggested re-engagement actions. |
| **Why It Matters** | Losing a member costs the gym EGP 500-2,000/month. Predicting who's about to leave — and reaching out before they do — can save significant revenue. |
| **Who Uses It** | Gym owner and managers see the churn prediction dashboard in the Staff Web App. |
| **Available On** | **Business and above** |

### 27. Data Import from Odoo / Daftra / Excel

**Category:** Integration | **Feature Code:** `data_import`

| Field | Description |
|-------|-------------|
| **What It Does** | Lets a gym owner migrate their existing data into GymApp by uploading a single Excel workbook. The workbook has four tabs — `Members`, `Payments`, `Sessions`, `InBody` — all keyed by the gym's own member number so nothing has to be re-entered. The file can be exported from Odoo or Daftra, or filled in from the downloadable template. Processing happens in the background; a CSV of any rejected rows is produced for the owner to review. |
| **Why It Matters** | Most Egyptian gyms that outgrow spreadsheets or legacy accounting systems (Odoo, Daftra) have years of member, payment, package and InBody data. Re-entering it manually would kill adoption. One Excel upload and day-one GymApp matches the gym's historical records. |
| **Who Uses It** | The gym owner opens the "Data Import" page in the Gym Staff Web App, downloads the template, pastes exported data, uploads it, and watches the job finish. The error CSV is downloadable directly from the same page. |
| **Available On** | **Pro and above** |

