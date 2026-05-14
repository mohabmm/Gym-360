# LOVABLE PROMPT — Admin Dashboard (Separate Lovable Project)

> **Target Persona**: YOU — the GymApp platform operator. Tech-savvy, manages 100+ gyms, needs data-dense views, power-user shortcuts, and full control over plans/features/subscriptions.
> **UX Philosophy**: Data-dense, professional, fast. Think Linear meets Stripe Dashboard. Dark sidebar, light content. Every click should do something — no decorative elements.
> **This is a SEPARATE Lovable project.** Output = UI/visual design only. Backend integration done by Claude agents later.

---

Build a complete **GymApp Platform Admin Dashboard** — a web-based SaaS administration panel for managing a multi-gym fitness platform. This is the command center for platform administrators who manage gym onboarding, subscriptions, plan/feature configuration, analytics, users, content, and support.

## Tech Stack
React + TypeScript + Tailwind CSS + shadcn/ui + Recharts + React Router + Lucide React + Tanstack Table. All data is mock/hardcoded JSON.

## Design Direction
Data-dense, professional SaaS dashboard for a power user who manages 100+ gyms. Dark sidebar (#0F172A) with light content area. Accent: Emerald (#10B981) positive, Red (#EF4444) alerts, Amber (#F59E0B) warnings, Blue (#3B82F6) primary. Typography: "Plus Jakarta Sans" headings, "Inter" body. Cards: shadow, rounded-xl, hover lift. Tables: zebra, sticky headers, inline actions. Every pixel should serve a purpose — no decorative illustrations or marketing fluff. This is a control panel, not a landing page.

## SIDEBAR NAVIGATION
```
[GymApp Logo + "Admin"]
MAIN: Dashboard (/) | Gyms (/gyms + /gyms/onboarding) | Users (/users) | Subscriptions (/subscriptions) | Plan & Features (/plans)
ANALYTICS: Platform Analytics (/analytics) | Churn Analytics (/analytics/churn)
OPERATIONS: Content (/content) | Settings (/settings)
[Bottom: Admin avatar + role badge (Super Admin / Admin / Support / Analyst) + logout]
```
Active = emerald left border. Badge count on Onboarding (pending gym count).

## PAGES

### Dashboard (/)
4 KPI cards: Total Gyms 142 (+12 ↑9.2%), Total Members 8420 (+340 ↑4.2%), MRR EGP 284,000 (+18.5%↑), Active Trainers 286 (+14).

**Payment Alerts bar** (only shows if overdue gyms exist): "⚠️ 3 gyms have overdue payments — 1 will be auto-suspended in 2 days" → clickable → navigates to Subscriptions page filtered by overdue. **"Bulk Suspend Overdue" button** (red, for gyms past grace period) → confirmation → suspends all eligible.
Row 2: Revenue Trend area chart (12 months, emerald gradient 120K→284K) | Gym Growth stacked bar (Approved/Pending/Rejected monthly).
Row 3: Recent Gym Registrations table (5 rows, status badges) | At-Risk Gyms alert list (5 gyms, days inactive, health score bar).
Row 4: Top Performing Gyms leaderboard (gold/silver/bronze) | Support Ticket donut (Open/InProgress/Resolved) | Plan distribution pie chart.

### All Gyms (/gyms)
Filters: search, status dropdown (All/Active/Grace Period/Suspended/Pending/Terminated), city (Cairo/Alexandria/Giza), plan (Free/Starter/Pro/Business), payment status (All/Paid/Overdue/Grace/Suspended), date range. Table: #, Gym Name (clickable), Location, Owner, Members, Trainers, Revenue/mo, Status badge (🟢/🟡/🔴/⚫), Payment badge (✅/⚠️/🔶/🔴), Plan badge, Registered, Actions dropdown (View, Suspend, Reactivate). 25 mock Egyptian gyms — include 2 overdue, 1 in grace period, 1 suspended. Paginated 20/page.

### Gym Detail (/gyms/:id)
Header: logo + name + location + status badge (🟢 Active / 🟡 Grace Period / 🔴 Suspended / ⚫ Terminated) + plan badge + payment status badge (✅ Paid / ⚠️ Overdue / 🔶 Grace / 🔴 Suspended) + owner + **Action buttons**: Edit, **Suspend** (→ modal: reason required, warning "This will block all gym apps immediately. Members/trainers/staff will see a suspension message. Data is preserved." → Confirm), **Reactivate** (only visible when suspended → instant reactivation), **Terminate** (→ modal: type gym name to confirm, "This is permanent. Data deleted after 30 days."). **6 tabs**: Overview (4 KPIs, member growth line chart, check-in heatmap hour×day, activity feed) | Members (table) | Trainers (card grid) | Revenue (chart + payments table) | **Subscription & Features** (see below) | Settings (branding preview, danger zone).

**Subscription & Features tab**: Current Plan card (plan name badge, price, started date, next billing, member usage 180/200 with progress bar, storage usage 1.2GB/2GB with progress bar, trainer count, staff count). "Change Plan" button → modal with plan dropdown + reason field. **"Deploy to This Gym" button** (green) → toast "Config pushed to [Gym Name]". Below: **Feature Override Table** — all features listed. Each row: Feature Name | Plan Default (green check or red X) | Override column: "Default" (grey) / "Force ON" (green toggle) / "Force OFF" (red toggle). When admin changes override, show "Reason" input. Mock: 2-3 overrides. At bottom: **Override History** mini-table (Date, Feature, Change, Admin, Reason).

### Gym Onboarding (/gyms/onboarding)
Table: Gym Name, Owner, Email, Phone, Location, Submitted, Actions [Approve ✅] [Reject ❌]. **Approve modal**: plan dropdown (Free/Starter/Pro/Business — default Free), auto-generated gym code, welcome email toggle, storage allocation display. Reject modal: reason required. 5 pending gyms.

### Users (/users)
Filters: search, role (All/Trainee/Trainer/Staff/Owner), status, gym. Table: Name, Email, Role (color badge), Gym(s), Joined, Last Active, Status, Actions. 30 mock users with Arabic names.

### Subscriptions (/subscriptions)
**4 plan cards**: FREE (EGP 0, ≤25 members, 100MB storage, 10 gyms, "Edit Plan"), STARTER (EGP 499, ≤75 members, 500MB, 58 gyms, "Edit Plan"), PRO (EGP 999, ≤200 members, 2GB, 32 gyms, "Edit Plan"), BUSINESS (EGP 1,999, ≤500 members, 10GB, 10 gyms, "Edit Plan"). **"+ Create New Plan"** button.

**Edit Plan modal**: Plan Name, Monthly Price (EGP), Annual Price, Member Limit (-1=unlimited), Trainer Limit, Staff Limit, Storage Limit, Photo Limit per Member, Check-in Retention (days), API Rate Limit. **Visibility section**: "Public" toggle (visible on signup page) + "Time-Limited" toggle → if ON, shows Available From (date picker) and Available Until (date picker). Note: "Gyms already on this plan keep their subscription even if hidden." Below: **Feature Checklist** — all features with toggle switches. "Save Changes" → toast "Plan saved — click Deploy to push to gym apps" → increments pending badge.

**Subscription Table**: Gym, Plan badge, Members usage/limit progress bar, Storage usage/limit, MRR, **Payment Status badge (✅ Paid green / ⚠️ Overdue amber with "X days" / 🔶 Grace Period with countdown / 🔴 Suspended red)**, Next Payment Due, Override count badge, Actions (Change Plan, **Suspend**, **Reactivate**, Send Payment Reminder). Mock: show 2 overdue gyms (7 days, 15 days), 1 grace period gym (19 days — 2 days until suspension), 1 suspended gym.

### Plan & Features (/plans) — **MASTER CONFIGURATION**
**Sticky top bar**: Pending changes count badge + **"Deploy Changes" green button**. Deploy → confirmation "Deploy to X gyms? Push config to Y apps instantly." → toast "Deployed — X gyms updated" → badge resets.

**Section 1: Plans Management**: Plan cards row + "Create New Plan" button → modal (name, code auto-slug, description, prices, all limits including storage, is_public toggle, **Availability** section: "Always available" radio (default) OR "Limited time" radio → shows date range picker (Available From / Available Until), sort order). Each plan card shows: Plan Name, Price, Member Limit, Gym Count, Status indicators: 🟢 Active+Public / 🟡 Active+Hidden / 🔴 Inactive / ⏰ Time-Limited (shows "Available Mar 1 - Mar 31"). **"Hide from Members"** quick toggle on each card (toggles is_public) — lets admin instantly hide a plan from the signup page without deleting it. Gyms already on a hidden plan keep their subscription. Mock: 4 default plans + 1 custom "Ramadan Special" plan (EGP 699, 150 members, marked as time-limited: Available Mar 1 - Apr 15, currently showing ⏰ badge). "Delete Plan" button (disabled if gyms are on it, shows "X gyms on this plan" warning).

**Section 2: Features Management**: Table (Feature Code, Display Name, Description, Category badge, Plans Enabled count, Status, Actions). **"+ Create New Feature"** button → modal (code, name, description, category dropdown). Mock: 34 default + "inbody_integration" + "whatsapp_bot" custom features.

**Section 3: Feature Matrix**: Interactive grid. Rows = ALL features (grouped by category with collapsible sections). Columns = ALL plans. Each cell = toggle switch. Footer per column: "X gyms affected". Toggle → toast "[Feature] saved for [Plan] — click Deploy".

**Section 4: Override Audit**: Table of ALL per-gym overrides. Gym Name, Feature, Override Type (Force ON/OFF badge), Set By, Date, Reason, "Remove" action. Filters + sort.

### Platform Analytics (/analytics)
6 KPI cards. Member Growth area chart. Gym Registration bar. Revenue by City horizontal bar. Top 10 Gyms table. Plan Distribution donut. DAU line chart (30 days). Feature Usage horizontal bar (which features are most used — helps decide what to include in lower plans).

### Churn Analytics (/analytics/churn)
3 KPIs: Churn Rate 3.2%, At-Risk 8, Avg Lifetime 14.5mo. Churn Funnel. Churn by Plan table. At-Risk Gym table: last login, days inactive, member/revenue sparklines, risk score bar, actions.

### Content (/content)
Exercise Library management: table of all exercises (name, muscle groups tags, equipment, category, demo URL, status, actions). "Add Exercise" modal (name, muscle groups multi-select, equipment, description, demo video URL). This manages the global 200+ exercise library that all gyms share. Feature gate: `custom_exercise_library` for gym-specific exercises (Business only — but the global library is managed here by admin).

### Settings (/settings)
**Platform Config**: Platform name + logo + support email. Email/SMS config.

**Admin Team Management** (super_admin only): Table of all platform admins: Name, Email, Role (Super Admin gold / Admin blue / Support green / Analyst gray badge), Added By, Added Date, Status, Actions (Edit Role, Deactivate). **"Invite Admin" button** → modal: Email, Role dropdown (Admin/Support/Analyst — cannot create another Super Admin), Send Invite. Mock: 3 admins — "You (Super Admin)", "Ayman Saber (Admin, added Mar 2026)", "Sara Ahmed (Support, added Apr 2026)".

**Security**: 2FA toggle, session timeout. **PDPL Compliance**: data retention settings, breach notification log (super_admin only).

## GLOBAL UI
1. All data mock with realistic Egyptian gym names, Arabic user names, EGP currency.
2. Responsive: desktop 1440px+ and tablet 768px+. Sidebar collapses to icons.
3. Dark mode toggle in header. Sidebar always dark.
4. Skeleton loading on every data section.
5. Every table: sort, search, paginate 20/page, export CSV.
6. All actions show toast. Destructive actions need confirmation modal.
7. Charts animate on first render. Consistent emerald/blue/amber palette.
8. Breadcrumbs on every page except Dashboard.
9. Badge count on sidebar: Onboarding pending.
10. Empty states with brief text (no large illustrations — this is a power-user tool).

Build ALL pages fully with mock data. No placeholders.
