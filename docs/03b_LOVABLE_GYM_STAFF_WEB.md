# LOVABLE PROMPT — Gym Staff Web App (Separate Lovable Project)

> **DEMO VERSION**: This prototype uses real data from **First Gym, New Cairo** — our first target customer. All gym details, branding, member names, trainer names, classes, and data are tailored to First Gym so we can demo this directly to the owner.
> **Target Persona**: GYM OWNER — may NOT be tech-savvy. They need things simple, clear, and fast. Arabic/English bilingual. Used to paper/Excel/WhatsApp.
> **UX Philosophy**: Warm, friendly, approachable. Light theme, big touch targets, clear labels, friendly empty states. NOT a dark corporate dashboard.
> **Output**: UI/visual design only. Backend integration done by Claude agents later.

---

Build a **Gym Staff Management Web App** — a desktop web application used by gym owners and managers to manage their gym's daily operations. This demo uses **First Gym, New Cairo** as the gym.

## FIRST GYM DETAILS (Use throughout all mock data)
- **Gym Name**: First Gym (فيرست جيم)
- **Location**: El Banafseg 1, in front of Gate 12 Al-Rehab City, New Cairo, Cairo, Egypt
- **Branches**: 2 — Men's gym (1st floor) + Ladies-only (2nd floor) — same building
- **Operating Hours**: 24/7
- **Phone**: 01272463444
- **Alt Phones**: 01023032345, 01064089529
- **Facebook**: facebook.com/First.Gym.EG (23,500+ followers) + facebook.com/firstgymladies (4,400+)
- **Estimated Members**: ~280 across both floors
- **Brand Colors**: Primary: Deep Red (#C41E3A), Secondary: Black (#1A1A1A), Accent: Gold (#D4AF37)
- **Amenities**: Jacuzzi, Steam Room, Sauna, Separate Ladies Floor, Free Parking, Lockers, Showers, WiFi
- **Classes offered**: CrossFit, HIIT, Yoga, Cardio Kickboxing, Functional Training, Spinning, Pilates
- **Owner name for demo**: Mostafa (use as the logged-in user)

## Tech Stack
React + TypeScript + Tailwind CSS + shadcn/ui + Recharts + React Router + Lucide React + Tanstack Table. All data is mock/hardcoded JSON.

## Design Direction — FRIENDLY, NOT INTIMIDATING
- **Light theme default**. White backgrounds, soft grays. Light sidebar with subtle gray borders.
- **Deep Red (#C41E3A)** for primary actions (matching First Gym's brand). Gold (#D4AF37) for highlights and achievements. Slate (#475569) for text.
- **Large text** — body 14-16px, headings 20-28px.
- **Big action buttons** — prominent, not hidden in dropdowns.
- **Friendly empty states** with warm illustrations + encouraging text + CTA button.
- **Confirmation on everything** — "Are you sure?" modals for any action that changes data.
- Typography: "Plus Jakarta Sans" headings, "Inter" body. Rounded corners. Soft shadows.

## SIDEBAR NAVIGATION
```
[SVG Logo: Bold "FIRST" in deep red + "GYM" below in black, athletic font + "First Gym"]
📊 Dashboard        → /
👥 Members           → /members
🏋 Trainers          → /trainers
📅 Classes           → /classes
💪 Exercises         → /exercises
💳 Billing           → /billing
🏆 Gamification      → /gamification
📢 Announcements     → /announcements
📊 Analytics         → /analytics
🚪 Check-Ins         → /check-ins
👤 Staff             → /staff
⚙️ Settings          → /settings
[Bottom: User avatar + "Mostafa, Owner" + logout]
```
Active state: Deep Red left border + light red background (#FEF2F2). Icons + text always visible.

## PAGES

### Dashboard (/)
**Welcome header**: "Good morning, Mostafa! ☀️" + today's date + "First Gym" name. Feels personal.

4 KPI cards: Active Members 283 (+22 this month ↑), Revenue This Month EGP 245,800 (+15.1%↑), Retention Rate 89.3%, Today's Check-ins 67.

Row 2: Revenue Trend area chart (last 6 months, deep red gradient: 180K → 245K) | Peak Hours heatmap (24/7 gym — show full 24 hours, highlight 6-8AM, 5-9PM as hottest, 1-5AM as coldest).

Row 3: **Today's Schedule** (next 5 sessions: "6:00 PM — Coach Karim × Ahmed R.", "6:30 PM — Coach Dina × Nour H.", "7:00 PM — Coach Tarek × Omar S.", "7:30 PM — Coach Karim × Sara M.", "8:00 PM — CrossFit Class (18/25)") | **Quick Actions** (4 large cards: "➕ Add Member", "💰 Record Payment", "📅 Create Class", "📢 Send Announcement").

Row 4: **Top Classes** ("CrossFit WOD 23/25", "HIIT Burn 20/20 FULL", "Yoga Flow 15/20", "Cardio Kickboxing 18/25", "Spinning 12/15") | **Needs Attention** ("Youssef Adel — membership expires tomorrow", "Hana Mostafa — hasn't visited in 14 days ⚠️", "3 pending trainer approvals", "Package low: Sara's 10-session package has 2 left").

Row 5: **Coach of the Month** spotlight — "🏆 Coach Karim Mahmoud — Score 4.8 — 48 sessions this month". Gold accent border, trophy icon. Feature gated: `coach_of_the_month`.

Row 6: **Recent Activity** feed: "Ahmed Ragab checked in (3 min ago)", "Payment EGP 800 recorded for Nour Hassan (12 min ago)", "CrossFit class booked by 4 members (30 min ago)", "Coach Dina completed session with Hana (1 hr ago)", "New member registered: Yara Khaled (2 hrs ago)".

### Members (/members)
**Page header**: "Your Members (283)" + large "➕ Add Member" button (deep red).

Search bar: "Search by name, phone, or email...". Filters: Status (All/Active/Expired/Frozen), Plan (Basic/Premium/VIP/Student/Quarterly/Annual), Floor (All/Men's Floor/Ladies Floor), "Clear filters".

Data table: Photo + Name + **Member # (e.g., #7824)**, Phone, Plan badge, Floor (🔵 Men's / 🩷 Ladies), Joined, Last Visit, Status badge, Actions (View, Edit, More → Freeze, Deactivate, Send Reminder).

**Bulk actions**: Checkbox + bulk bar (Send Announcement, Freeze, Export CSV).

Mock data — 30 First Gym members with realistic names:
**Men's Floor (18)**: Ahmed Ragab (Premium, Active, 2 days ago), Omar Sayed (VIP, Active, today), Youssef Adel (Basic, Active, ⚠️ 12 days), Karim Nabil (Annual, Active, 1 day), Mohamed Tarek (Premium, Expired ❌), Hassan Fathy (Student, Active, today), Mahmoud Essam (Basic, Frozen 🔵), Ali Mostafa (Quarterly, Active, 3 days), Tamer Galal (Premium, Active, today), Ibrahim Lotfy (VIP, Active, yesterday), Sherif Hamed (Basic, Expired), Amr Wael (Premium, Active, 5 days), Khaled Samir (Annual, Active, today), Ramy Ashraf (Student, Active, 1 day), Walid Nasser (Basic, Active, 4 days), Hossam Reda (Premium, Frozen), Seif ElDin (VIP, Active, today), Bassem Fouad (Quarterly, Active, 2 days).
**Ladies Floor (12)**: Sara Mohamed (Premium, Active, today), Nour Ibrahim (VIP, Active, 1 day), Hana Mostafa (Basic, Active, ⚠️ 14 days), Dina Saleh (Annual, Active, today), Yasmine Khaled (Premium, Active, 2 days), Mariam Hassan (Student, Active, yesterday), Farida Amr (Quarterly, Active, 3 days), Rana Tarek (Premium, Expired), Salma Wael (Basic, Active, today), Nada Sherif (VIP, Active, 1 day), Yara Khaled (Premium, Active, today — NEW), Laila Sami (Basic, Frozen).

### Member Detail (/members/:id)
Breadcrumb: Members > Ahmed Ragab. Header: avatar + "Ahmed Ragab" + **#7824** + 01012345678 + ahmed.ragab@gmail.com + Premium badge + Active status + Floor: Men's + "Edit Profile".

4 tabs:
- **Profile**: Name, phone, email, DOB: 15 Mar 1992, Gender: Male, Floor: Men's 1st Floor, Health conditions: "None reported", Goals: "Build muscle, lose belly fat", Membership: Premium Monthly, Start: 1 Jan 2026, Expiry: 1 May 2026, Assigned Trainer: Coach Karim, Gym Code: FG-AHR-2026
- **Payments**: 4 rows — EGP 800 (Card, Membership, REC-FG-20260401-012), EGP 5,000 (Cash, 10-Session Package, REC-FG-20260315-008), EGP 800 (Card, Membership, REC-FG-20260301-003), EGP 800 (Online, Membership, REC-FG-20260201-001)
- **Sessions**: 8 rows with Coach Karim — Chest & Triceps, Back & Biceps, Legs, etc.
- **Progress**: Weight chart (95kg → 88kg over 3 months), last check-in: today 5:45 PM

### Trainers (/trainers)
Card grid — 6 First Gym trainers:
1. **Coach Karim Mahmoud** — Strength, Bodybuilding — 14 clients — 48 sessions/mo — ⭐4.8 — 🏆 Coach of the Month
2. **Coach Dina Hassan** — Yoga, Pilates, Ladies Fitness — 12 clients — 40 sessions/mo — ⭐4.7 (Ladies floor)
3. **Coach Tarek Sayed** — CrossFit, Functional — 10 clients — 35 sessions/mo — ⭐4.5
4. **Coach Amira Nabil** — Cardio, HIIT, Weight Loss — 8 clients — 30 sessions/mo — ⭐4.6 (Ladies floor)
5. **Coach Hassan Ali** — Boxing, Kickboxing — 6 clients — 22 sessions/mo — ⭐4.4
6. **Coach Mahmoud Essam** — Spinning, Cardio — 5 clients — 18 sessions/mo — ⭐4.3

**Pending Trainer Requests**: "2 trainers waiting for approval" — "Ramy Adel (Strength, submitted 2 days ago)" + "Salma Nour (Yoga, submitted today)". Approve ✅ / Reject ❌.

### Classes (/classes)
Weekly calendar showing First Gym's actual class types:
- **Mon/Wed/Fri 7AM**: CrossFit WOD (Coach Tarek, 25 cap)
- **Mon/Wed 6PM**: HIIT Burn (Coach Amira, 20 cap — Ladies)
- **Tue/Thu 8AM**: Yoga Flow (Coach Dina, 20 cap — Ladies)
- **Tue/Thu 7PM**: Cardio Kickboxing (Coach Hassan, 25 cap)
- **Sat 10AM**: Functional Training (Coach Tarek, 20 cap)
- **Daily 6AM**: Spinning (Coach Mahmoud, 15 cap)
- **Fri 5PM**: Pilates Core (Coach Dina, 15 cap — Ladies)

### Exercises (/exercises)
**Page header**: "Exercise Library" + search bar ("Search by name or muscle group...").

Two sections:

**Global Exercises** (available to all gyms, read-only): Filterable table of 200+ exercises. Columns: Name, Muscle Groups (tags: Chest, Back, Shoulders, Arms, Legs, Core, Cardio), Equipment (Barbell, Dumbbell, Machine, Cable, Bodyweight), Category (Strength/Cardio/Flexibility), Demo (video link icon if available). Filter bar: Muscle Group dropdown, Equipment dropdown, Category dropdown. Gym staff can browse and trainers reference these when building workouts. **Cannot edit or delete global exercises** — those are managed by the platform admin.

Mock: 15 visible exercises — Barbell Bench Press (Chest, Barbell), Lat Pulldown (Back, Machine), Shoulder Press (Shoulders, Dumbbell), Barbell Squat (Legs, Barbell), Plank (Core, Bodyweight), Deadlift (Back/Legs, Barbell), Cable Fly (Chest, Cable), Leg Press (Legs, Machine), Bicep Curl (Arms, Dumbbell), Tricep Pushdown (Arms, Cable), Running (Cardio, Bodyweight), Kettlebell Swing (Core, Kettlebell), Romanian Deadlift (Legs, Barbell), Pull-Up (Back, Bodyweight), Push-Up (Chest, Bodyweight).

**Custom Exercises** (feature gated: `custom_exercise_library` — Business plan only): If gym is on Business plan, show a second section below: "Your Custom Exercises" + **"➕ Add Exercise"** button → modal: Name, Muscle Groups (multi-select), Equipment, Description, Demo Video URL (optional), Demo Image upload (optional). These exercises are private to this gym — only their trainers see them. Useful for gyms with specialty equipment or proprietary exercises. Mock: 2 custom exercises — "First Gym Cable Rotation" (Core, Cable), "Rehab Shoulder Mobility" (Shoulders, Bodyweight). If NOT on Business plan, show locked section: "Upgrade to Business to add your own custom exercises" + upgrade prompt.

### Billing (/billing)
3 tabs: **Payments** | **Plans** | **Packages**.

**Payments**: Revenue This Month EGP 245,800 (↑15.1% vs last month), Pending EGP 8,500, Overdue EGP 3,200. Table with First Gym receipts: REC-FG-20260408-xxx format. "💰 Record Payment" modal.

**Revenue Breakdown**: Memberships 72% vs PT Packages 28%. Revenue by Trainer: Coach Karim EGP 45,000, Coach Dina EGP 32,000, Coach Tarek EGP 28,000, Coach Amira EGP 22,000, Coach Hassan EGP 15,000.

**Plans** (First Gym's own membership plans — what they charge members):
- Basic Monthly: EGP 500/mo (gym access only)
- Premium Monthly: EGP 800/mo (gym + classes + sauna/steam)
- VIP Monthly: EGP 1,200/mo (everything + 2 PT sessions/mo)
- Student Monthly: EGP 350/mo (gym access, valid student ID required)
- Quarterly: EGP 2,000/3mo (Premium equivalent, 17% discount)
- Annual: EGP 6,500/yr (Premium equivalent, 32% discount)

**Packages**: Templates: "10 Sessions - EGP 5,000 (EGP 500/session)", "20 Sessions - EGP 8,000 (EGP 400/session)", "5 Sessions Trial - EGP 2,000 (EGP 400/session)". Assigned packages: Ahmed Ragab × Coach Karim (10-session, 7 remaining), Sara Mohamed × Coach Dina (20-session, 15 remaining), Omar Sayed × Coach Karim (10-session, 2 remaining ⚠️ LOW).

### Gamification (/gamification)
3 tabs:

**Member Leaderboards**: Attendance: 1. Seif ElDin (26 visits), 2. Omar Sayed (24), 3. Sara Mohamed (23), 4. Karim Nabil (22), 5. Dina Saleh (21). Weight Loss %: 1. Ahmed Ragab (-7.4%), 2. Hana Mostafa (-5.2%), 3. Tamer Galal (-4.8%). Consistency: 1. Nour Ibrahim (96%), 2. Sara Mohamed (94%), 3. Karim Nabil (91%). "8 members opted out" note.

**Coach Rankings**: 1. 🏆 Karim Mahmoud (Score 4.8, 48 sessions, 14 clients), 2. Dina Hassan (4.7, 40 sessions, 12 clients), 3. Amira Nabil (4.6, 30 sessions, 8 clients), 4. Tarek Sayed (4.5, 35 sessions, 10 clients), 5. Hassan Ali (4.4, 22 sessions, 6 clients). Click Karim → individual ratings: "Ahmed Ragab ⭐5 — Best trainer I've ever had", "Omar Sayed ⭐5 — Pushes me hard", "Youssef Adel ⭐4 — Good but sometimes late". **MANAGER EYES ONLY**.

**Testimonials**: 3 pending: "This gym changed my life — lost 15kg in 4 months" (from Ahmed R. about Coach Karim), "Coach Dina is incredibly motivating" (from Nour I.), "The ladies floor is so comfortable" (from Yasmine K.). Approve/Edit/Reject.

### Announcements (/announcements)
Mock announcements:
- "🌙 Ramadan Hours Update" (Published, General, All Members)
- "💪 New CrossFit Schedule Starting May" (Scheduled May 1, Schedule, All)
- "🎉 First Gym 5th Anniversary — 25% Off Annual Plans!" (Draft, Offer, All)
- "🔧 Steam Room Maintenance — Closed April 12-13" (Published, Maintenance, All)

### Analytics (/analytics)
KPIs: Active Members 283 (+22), Revenue EGP 245,800 (+15.1%), Retention 89.3%, Churn 4.2%.
Member Growth: 12-month chart showing 180 → 283 growth.
New vs Churned: stacked monthly bar.
Peak Hours: 24-hour heatmap (key insight: 6-8AM rush, 5-9PM rush, 11PM-5AM very low but not zero — it's 24/7).
Top Classes: CrossFit #1, HIIT #2, Yoga #3.
Trainer Utilization: Coach Karim 96%, Coach Dina 83%, Coach Tarek 73%.
Check-in Frequency: "5+/week" 15%, "3-4x/week" 38%, "1-2x/week" 32%, "Not this month" 15%.

### Check-Ins (/check-ins)
Today's Stats: Total 67, Peak Hour 6-7PM (12), Unique Members 61.
Live log: "5:45 PM — Ahmed Ragab #7824 (QR)", "5:43 PM — Sara Mohamed #3201 (Member Number)", "5:40 PM — Omar Sayed #4455 (QR)", "5:38 PM — Karim Nabil #1089 (Manual by Rania)", "3:12 AM — Seif ElDin #6612 (Member Number)" — shows all 3 check-in methods working, including member number at 3AM (no staff needed).
Check-in trends: 30-day bar chart.
**"Manual Check-In" button** → search by name OR type member number → confirm → toast "Welcome to First Gym, Ahmed #7824! ✅".
Note: Member number is shown next to each name in the log. Method badge shows how they checked in (QR / Member Number / Manual).

### Staff (/staff)
**Page header**: "Your Staff (4/10)" — on Pro plan.

Table:
- Mostafa (Owner, amber badge, since Jan 2022, Active)
- Ayman Saber (Manager, blue, since Mar 2023, Active)
- Heba Farouk (Receptionist, gray, since Sep 2024, Active — Ladies floor desk)
- Rania Khaled (Receptionist, gray, since Jan 2025, Active — Main floor desk)

"Add Staff Member" modal: role dropdown (Owner/Manager/Receptionist), preset permissions.

**Permission Matrix** (read-only on Pro — shows defaults): Owner=all ✅, Manager=all except billing settings, Receptionist=check-in+payments+view members only. Note: "Upgrade to Business to customize permissions".

### Settings (/settings)
Pre-filled with First Gym data:
- **Gym Profile**: Name: "First Gym", Description: "Modern 24/7 gym for men and ladies with professional trainers, group classes, and spa facilities. Separate ladies floor for complete privacy.", Phone: 01272463444, Alt: 01023032345, Email: info@firstgym.eg, Address: El Banafseg 1, in front of Gate 12 Al-Rehab City, New Cairo, Map placeholder with pin.
- **Operating Hours**: All days show "24/7" — the "24/7 Gym" toggle is ON.
- **Branding**: Logo: Generate an SVG logo inline — bold athletic text "FIRST" on top line + "GYM" below in slightly smaller text, deep red (#C41E3A) on white, with a subtle gold (#D4AF37) underline accent. Use a bold sans-serif font (like Impact or Oswald style). This matches their actual branding style. Primary color: #C41E3A (deep red), Secondary: #1A1A1A (black), Accent: #D4AF37 (gold). **Live preview card** — phone mockup showing "First Gym" with red theme.
- **Amenities**: ✅ Parking, ✅ Showers, ✅ Lockers, ✅ WiFi, ✅ Sauna, ✅ Steam Room, ✅ Jacuzzi, ❌ Pool, ❌ Juice Bar, ❌ Pro Shop. Custom added: "Separate Ladies Floor".
- **Social Media**: Facebook: facebook.com/First.Gym.EG, Instagram: (empty), TikTok: (empty).
- **Notifications**: Session reminders ✅, Expiry warnings ✅, Announcements ✅, Check-in confirmation ❌.

## GLOBAL UI REQUIREMENTS
1. **ALL mock data uses First Gym's real details** — name, location, brand colors, realistic member/trainer names.
2. Receipt numbers use **REC-FG-** prefix (FG = First Gym code).
3. Responsive: desktop (1440px+) primary, tablet (768px+).
4. **Light theme with First Gym's deep red (#C41E3A) as primary action color.** Gold (#D4AF37) for achievements/highlights.
5. Skeleton loading, friendly empty states with CTA.
6. Toast with emoji: "✅ Member added!", "💰 Payment recorded!".
7. Confirmation modal for ALL destructive actions.
8. Charts: animate, deep red palette, large labels.
9. Tables: sortable, searchable, paginated 20/page.
10. Breadcrumbs on detail pages.
11. **Floor filter** on Members page — "Men's Floor" / "Ladies Floor" — unique to First Gym's 2-floor setup.
12. **No jargon** — "Members" not "Users", "Revenue" not "MRR".
13. Build ALL pages fully with mock data. No placeholders. This is a live demo for First Gym's owner.
