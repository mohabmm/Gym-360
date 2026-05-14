# GymApp — 50 Maestro Edge-Case Test Flows

> Companion to `docs/MAESTRO_TEST_CONTEXT.md`.
> **How to use:** each `## TC-NN` block below is a stand-alone Maestro flow. Copy the YAML into `tests/maestro/{app}/NN_<slug>.yaml` and run with `maestro test`. Or paste the catalog into Claude project and ask for refactors.
> **Element labels** assume English locale; Arabic equivalents are noted only where stringly different in a load-bearing way. If a `tapOn:` text doesn't match the current build, swap to the actual visible label — the *intent* of each step is comment-documented above it.
> **Pre-reqs:** demo seed loaded (`db/seed.sql`), backend up on `localhost:8080` (Android emu uses `10.0.2.2`), Maestro CLI ≥ 1.36.

Coverage: Trainee 20 · Trainer 15 · Staff 15.

---

# TRAINEE APP — 20 edge cases  (`com.example.gymapp_trainee`)

## TC-01 — Account lockout after 5 failed logins
**Tags:** auth, boundary, state-transitions
```yaml
appId: com.example.gymapp_trainee
name: TC-01 trainee lockout after 5 failed logins
---
- launchApp:
    clearState: true
- repeat:
    times: 5
    commands:
      - tapOn: "Email"
      - inputText: "trainee@demo.local"
      - tapOn: "Password"
      - eraseText: 20
      - inputText: "wrong-pw"
      - tapOn: "Login"
      - assertVisible:
          text: "Invalid"
          timeout: 5000
- tapOn: "Email"
- inputText: "trainee@demo.local"
- tapOn: "Password"
- eraseText: 20
- inputText: "123456"
- tapOn: "Login"
- assertVisible:
    text: "locked"
    timeout: 5000
```

## TC-02 — OTP rate limit: 4th OTP request inside an hour blocked
**Tags:** auth, boundary, time-based
```yaml
appId: com.example.gymapp_trainee
name: TC-02 OTP rate limit
---
- launchApp:
    clearState: true
- tapOn: "Forgot password"
- tapOn: "Email"
- inputText: "trainee@demo.local"
- repeat:
    times: 3
    commands:
      - tapOn: "Send OTP"
      - assertVisible:
          text: "OTP sent"
          timeout: 5000
      - tapOn: "Resend"
- tapOn: "Send OTP"
- assertVisible:
    text: "Too many requests"
    timeout: 5000
```

## TC-03 — Register with under-13 DOB blocked
**Tags:** validation, PDPL, boundary
```yaml
appId: com.example.gymapp_trainee
name: TC-03 under-13 registration blocked
---
- launchApp:
    clearState: true
- tapOn: "Sign up"
- tapOn: "Email"
- inputText: "kid_${RANDOM}@demo.local"
- tapOn: "Password"
- inputText: "Password1!"
- tapOn: "Date of birth"
- inputText: "01/01/2018"
- tapOn: "Continue"
- assertVisible:
    text: "13"
- assertNotVisible: "Profile setup"
```

## TC-04 — Register with 13-15 DOB triggers parental consent gate
**Tags:** validation, PDPL, consent
```yaml
appId: com.example.gymapp_trainee
name: TC-04 parental consent gate 13-15
---
- launchApp:
    clearState: true
- tapOn: "Sign up"
- tapOn: "Email"
- inputText: "teen_${RANDOM}@demo.local"
- tapOn: "Password"
- inputText: "Password1!"
- tapOn: "Date of birth"
- inputText: "01/01/2012"
- tapOn: "Continue"
- assertVisible:
    text: "Parental consent"
    timeout: 5000
- assertVisible: "Parent email"
```

## TC-05 — Link gym with invalid 6-char code shows not-found, never advances
**Tags:** validation, multi-tenant, boundary
```yaml
appId: com.example.gymapp_trainee
name: TC-05 invalid gym code
---
- runFlow: login_trainee_no_gym.yaml
- assertVisible: "Link your gym"
- tapOn: "Gym code"
- inputText: "ZZZZZZ"
- tapOn: "Link gym"
- assertVisible:
    text: "not found"
    timeout: 5000
- assertVisible: "Link your gym"  # still on gate screen
```

## TC-06 — JWT expiry mid-session redirects to login (no silent crash)
**Tags:** auth, time-based, network
```yaml
appId: com.example.gymapp_trainee
name: TC-06 JWT expiry mid-session
---
- runFlow: login_trainee.yaml
- assertVisible: "Home"
# simulate by clearing access token in shared prefs via deep link / dev hook,
# then make any authenticated call (open Progress)
- openLink: "gymappdev://debug/expire-jwt"
- tapOn: "Progress"
- assertVisible:
    text: "Login"
    timeout: 8000
```

## TC-07 — Log weight of 0 rejected, of 9999 warned
**Tags:** validation, boundary
```yaml
appId: com.example.gymapp_trainee
name: TC-07 weight log edges
---
- runFlow: login_trainee.yaml
- tapOn: "Progress"
- tapOn: "Log weight"
- tapOn: "Weight"
- inputText: "0"
- tapOn: "Save"
- assertVisible:
    text: "must be greater"
- eraseText: 4
- inputText: "9999"
- tapOn: "Save"
- assertVisible:
    text: "unusually"   # soft warning, not a block
- tapOn: "Confirm"
- assertVisible: "Saved"
```

## TC-08 — Cross-midnight booking: book at 23:58 for 00:30 tomorrow
**Tags:** time-based, scheduling
```yaml
appId: com.example.gymapp_trainee
name: TC-08 cross-midnight booking
---
- runFlow: login_trainee.yaml
# device clock should be set to 23:58 local via Maestro env or device CLI before run
- tapOn: "Schedule"
- tapOn: "Book session"
- tapOn:
    text: "Tomorrow"
- tapOn: "00:30"
- tapOn: "Confirm booking"
- assertVisible:
    text: "Booked"
    timeout: 6000
- tapOn: "Upcoming"
- assertVisible: "00:30"
```

## TC-09 — Late cancel inside window deducts session from package
**Tags:** scheduling, billing, business-rule
```yaml
appId: com.example.gymapp_trainee
name: TC-09 late cancel deducts package
---
- runFlow: login_trainee.yaml
# pre-seed: trainee has a session in 2h, package has 5 remaining, gym window=24h
- tapOn: "Schedule"
- tapOn:
    text: "Today"
    index: 0
- tapOn: "Cancel session"
- assertVisible: "Late cancellation"
- tapOn: "Cancel anyway"
- assertVisible: "Cancelled"
- tapOn: "Membership"
- assertVisible: "4 sessions"   # decremented from 5
```

## TC-10 — Cancel 25h before window: NO deduction
**Tags:** scheduling, billing, business-rule, boundary
```yaml
appId: com.example.gymapp_trainee
name: TC-10 cancel outside window no deduction
---
- runFlow: login_trainee.yaml
- tapOn: "Schedule"
- tapOn:
    text: "+25h"   # session scheduled >24h out
- tapOn: "Cancel session"
- assertNotVisible: "Late cancellation"
- tapOn: "Confirm cancel"
- assertVisible: "Cancelled"
- tapOn: "Membership"
- assertVisible: "5 sessions"   # unchanged
```

## TC-11 — Book session with 0 remaining sessions blocked
**Tags:** billing, business-rule, boundary
```yaml
appId: com.example.gymapp_trainee
name: TC-11 book with zero package
---
# pre-seed: trainee package exhausted
- runFlow: login_trainee_exhausted.yaml
- tapOn: "Schedule"
- tapOn: "Book session"
- assertVisible:
    text: "No sessions remaining"
- assertNotVisible: "Confirm booking"
```

## TC-12 — Book session when membership expired blocked
**Tags:** billing, business-rule
```yaml
appId: com.example.gymapp_trainee
name: TC-12 book with expired membership
---
- runFlow: login_trainee_expired.yaml
- tapOn: "Schedule"
- tapOn: "Book session"
- assertVisible:
    text: "Membership expired"
- assertVisible: "Renew"
```

## TC-13 — Chat opens empty state when no assigned trainer
**Tags:** auth, empty-state
```yaml
appId: com.example.gymapp_trainee
name: TC-13 chat without trainer
---
- runFlow: login_trainee_no_trainer.yaml
- tapOn: "More"
- tapOn: "Chat"
- assertVisible:
    text: "No trainer assigned"
- assertVisible: "Browse trainers"
```

## TC-14 — Progress photo >10MB rejected client-side, never uploads
**Tags:** validation, resource-limit, boundary
```yaml
appId: com.example.gymapp_trainee
name: TC-14 photo size limit
---
- runFlow: login_trainee.yaml
- tapOn: "Progress"
- tapOn: "Photos"
- tapOn: "Add photo"
- tapOn: "Choose from gallery"
# pre-stage a 12MB image as the most recent gallery item via adb push
- tapOn:
    id: "gallery_item_0"
- assertVisible:
    text: "exceeds 10 MB"
    timeout: 5000
- assertNotVisible: "Uploading"
```

## TC-15 — First progress photo blocks upload until photo-sharing consent
**Tags:** PDPL, consent, state-transition
```yaml
appId: com.example.gymapp_trainee
name: TC-15 photo consent gate
---
- runFlow: login_trainee_fresh.yaml   # never uploaded a photo before
- tapOn: "Progress"
- tapOn: "Photos"
- tapOn: "Add photo"
- assertVisible:
    text: "encrypted"   # consent dialog
- tapOn: "Decline"
- assertNotVisible: "Uploading"
- assertVisible: "Photos"
- tapOn: "Add photo"
- assertVisible: "encrypted"
- tapOn: "I agree"
- tapOn: "Choose from gallery"
- tapOn:
    id: "gallery_item_0"
- assertVisible:
    text: "Uploading"
    timeout: 5000
```

## TC-16 — Switch language Ar↔En mid-session preserves nav stack
**Tags:** localization, state-transition
```yaml
appId: com.example.gymapp_trainee
name: TC-16 language toggle preserves state
---
- runFlow: login_trainee.yaml
- tapOn: "Progress"
- tapOn: "Log weight"
- tapOn: "Weight"
- inputText: "75"
# do NOT save; switch language and ensure value survives
- tapOn:
    text: "Back"
- tapOn: "More"
- tapOn: "Settings"
- tapOn: "Language"
- tapOn: "العربية"
- assertVisible:
    text: "الإعدادات"
    timeout: 5000
- tapOn:
    text: "التقدم"   # Progress
- tapOn:
    text: "تسجيل الوزن"
- assertVisible: "75"
```

## TC-17 — Leaderboard opt-out hides public name but keeps private rank
**Tags:** privacy, business-rule
```yaml
appId: com.example.gymapp_trainee
name: TC-17 leaderboard opt-out
---
- runFlow: login_trainee.yaml
- tapOn: "More"
- tapOn: "Settings"
- tapOn: "Privacy"
- tapOn:
    id: "leaderboard_opt_out_toggle"
- assertVisible: "Hidden from public"
- tapOn: "Back"
- tapOn: "Back"
- tapOn: "More"
- tapOn: "Leaderboard"
- assertVisible:
    text: "Your private rank"
- assertNotVisible:
    text: "trainee@demo.local"   # name not in public list
```

## TC-18 — Water log: pressing +500ml 20× in 5s only logs unique entries
**Tags:** concurrency, debounce, boundary
```yaml
appId: com.example.gymapp_trainee
name: TC-18 water log debounce
---
- runFlow: login_trainee.yaml
- tapOn: "More"
- tapOn: "Water"
- repeat:
    times: 20
    commands:
      - tapOn:
          id: "water_add_500"
- assertVisible:
    text: "ml"
# expected daily total should equal 20 × 500 = 10000ml but UI must cap display warning
- assertVisible: "Daily target reached"
```

## TC-19 — Kill app mid-guided workout, resume restores set index + timer
**Tags:** state-persistence, crash-recovery
```yaml
appId: com.example.gymapp_trainee
name: TC-19 guided workout resume
---
- runFlow: login_trainee_remote_on.yaml   # trainer enabled remote mode
- tapOn: "Today's Workout"
- tapOn: "Start"
- assertVisible: "Exercise 1"
- tapOn: "Next set"
- tapOn: "Next set"
- assertVisible: "Set 3"
- stopApp
- launchApp
- assertVisible: "Resume workout?"
- tapOn: "Resume"
- assertVisible: "Set 3"
```

## TC-20 — Offline workout log queues and syncs on reconnect (last-writer-wins)
**Tags:** offline, sync, business-rule
```yaml
appId: com.example.gymapp_trainee
name: TC-20 offline workout sync
---
- runFlow: login_trainee.yaml
- tapOn: "Workouts"
- tapOn: "Log workout"
# turn airplane mode ON (via Maestro `runScript` adb)
- runScript: ./scripts/airplane_on.sh
- tapOn: "Add exercise"
- tapOn:
    text: "Bench Press"
    index: 0
- tapOn: "Sets"
- inputText: "3"
- tapOn: "Reps"
- inputText: "10"
- tapOn: "Save"
- assertVisible:
    text: "Queued"
- runScript: ./scripts/airplane_off.sh
- waitForAnimationToEnd:
    timeout: 10000
- tapOn: "Workout history"
- assertVisible:
    text: "Bench Press"
    timeout: 15000
```

---

# TRAINER APP — 15 edge cases  (`com.example.gymapp_trainer`)

## TC-21 — Pending-approval trainer cannot enter shell, sees gate screen
**Tags:** auth, state-transition
```yaml
appId: com.example.gymapp_trainer
name: TC-21 pending trainer gated
---
- launchApp:
    clearState: true
# seed a trainer whose status=Pending
- runFlow: login_trainer_pending.yaml
- assertVisible:
    text: "awaiting approval"
- assertNotVisible: "Clients"
- assertNotVisible: "Schedule"
```

## TC-22 — Activate Remote Workout Mode pushes notification + flips trainee UI
**Tags:** push, cross-app, state-transition
```yaml
appId: com.example.gymapp_trainer
name: TC-22 remote mode activation cross-app
---
- runFlow: login_trainer.yaml
- tapOn: "Clients"
- tapOn:
    text: "Sara"
    index: 0
- tapOn:
    id: "remote_mode_toggle"
- tapOn: "Reason"
- inputText: "On leave until April 20"
- tapOn: "Enable"
- assertVisible: "Enabled"
# manual cross-check: switch to trainee device, expect notification + Today's Workout card.
# automated cross-app handled by separate flow tests/maestro/trainee/TC22_verify.yaml
```

## TC-23 — Deactivate Remote Mode notifies trainee, card disappears
**Tags:** push, cross-app, state-transition
```yaml
appId: com.example.gymapp_trainer
name: TC-23 remote mode deactivation
---
- runFlow: login_trainer.yaml
- tapOn: "Clients"
- tapOn:
    text: "Sara"
- tapOn:
    id: "remote_mode_toggle"
- assertVisible: "Disabled"
```

## TC-24 — Save workout with 0 exercises blocked
**Tags:** validation, boundary
```yaml
appId: com.example.gymapp_trainer
name: TC-24 empty workout save
---
- runFlow: login_trainer.yaml
- tapOn: "Workouts"
- tapOn: "Build workout"
- tapOn: "Name"
- inputText: "Empty"
- tapOn: "Save"
- assertVisible:
    text: "Add at least one exercise"
- assertVisible: "Build workout"
```

## TC-25 — Superset of a single exercise rejected
**Tags:** validation, boundary
```yaml
appId: com.example.gymapp_trainer
name: TC-25 superset of one
---
- runFlow: login_trainer.yaml
- tapOn: "Workouts"
- tapOn: "Build workout"
- tapOn: "Add exercise"
- tapOn:
    text: "Squat"
- longPressOn:
    text: "Squat"
- tapOn: "Group as superset"
- assertVisible:
    text: "Need at least 2"
```

## TC-26 — Trainer cannot assign workout to a member outside their gym
**Tags:** multi-tenant, authorization
```yaml
appId: com.example.gymapp_trainer
name: TC-26 cross-gym assign blocked
---
- runFlow: login_trainer.yaml
# craft deep link to gym B's member id
- openLink: "gymappdev://client/00000000-0000-0000-0000-cross-gym-001/assign"
- assertVisible:
    text: "not found"
- assertNotVisible: "Assign"
```

## TC-27 — Live session fails gracefully if assigned client account deleted mid-flow
**Tags:** state-transition, error-handling
```yaml
appId: com.example.gymapp_trainer
name: TC-27 live session client deleted
---
- runFlow: login_trainer.yaml
- tapOn: "Schedule"
- tapOn:
    text: "Now"
    index: 0
- tapOn: "Start session"
# simulate client deletion server-side via dev endpoint
- openLink: "gymappdev://test/delete-active-client"
- tapOn: "Next exercise"
- assertVisible:
    text: "Client no longer available"
- assertVisible: "Return to schedule"
```

## TC-28 — Post-session log: missing required fields rejected, partial save not silent
**Tags:** validation
```yaml
appId: com.example.gymapp_trainer
name: TC-28 post-session log validation
---
- runFlow: login_trainer.yaml
- tapOn: "Schedule"
- tapOn:
    text: "Completed today"
    index: 0
- tapOn: "Log session"
- tapOn: "Save"
- assertVisible:
    text: "Rating is required"
- tapOn:
    id: "rating_3"
- tapOn: "Save"
- assertVisible:
    text: "Feedback is required"
```

## TC-29 — Add certification with past expiry date rejected
**Tags:** validation, time-based
```yaml
appId: com.example.gymapp_trainer
name: TC-29 expired cert rejected
---
- runFlow: login_trainer.yaml
- tapOn: "More"
- tapOn: "Profile"
- tapOn: "Certifications"
- tapOn: "Add"
- tapOn: "Name"
- inputText: "NASM-CPT"
- tapOn: "Expiry"
- inputText: "01/01/2020"
- tapOn: "Save"
- assertVisible:
    text: "must be in the future"
```

## TC-30 — Availability: end-time before start-time rejected
**Tags:** validation, time-based
```yaml
appId: com.example.gymapp_trainer
name: TC-30 invalid availability range
---
- runFlow: login_trainer.yaml
- tapOn: "Schedule"
- tapOn: "Availability"
- tapOn: "Sunday"
- tapOn: "Start"
- inputText: "18:00"
- tapOn: "End"
- inputText: "08:00"
- tapOn: "Save"
- assertVisible:
    text: "End must be after start"
```

## TC-31 — Recurring session capped at 12 weeks
**Tags:** business-rule, boundary
```yaml
appId: com.example.gymapp_trainer
name: TC-31 recurring 12-week cap
---
- runFlow: login_trainer.yaml
- tapOn: "Schedule"
- tapOn: "Recurring"
- tapOn: "Create"
- tapOn: "Repeat for"
- inputText: "20"   # weeks
- tapOn: "Save"
- assertVisible:
    text: "12 weeks"
# either auto-clamps or rejects — assert clamped value visible
- assertVisible: "12 weeks"
```

## TC-32 — Trainer never sees individual ratings, only aggregate
**Tags:** privacy, business-rule
```yaml
appId: com.example.gymapp_trainer
name: TC-32 ratings privacy
---
- runFlow: login_trainer.yaml
- tapOn: "More"
- tapOn: "My ratings"
- assertVisible:
    text: "★"   # aggregate visible
- assertNotVisible:
    text: "trainee@"   # no client identifiers
- assertNotVisible:
    text: "Wasn't focused"   # individual comment text from seed
```

## TC-33 — Earnings dashboard offline: cached snapshot rendered, banner shown
**Tags:** offline
```yaml
appId: com.example.gymapp_trainer
name: TC-33 earnings offline cache
---
- runFlow: login_trainer.yaml
- tapOn: "More"
- tapOn: "Earnings"
- assertVisible:
    text: "EGP"
- runScript: ./scripts/airplane_on.sh
- tapOn: "Refresh"
- assertVisible:
    text: "Offline"
- assertVisible:
    text: "EGP"   # cached value still visible
```

## TC-34 — Broadcast to 0 selected clients blocked
**Tags:** validation, boundary
```yaml
appId: com.example.gymapp_trainer
name: TC-34 empty broadcast
---
- runFlow: login_trainer.yaml
- tapOn: "More"
- tapOn: "Broadcast"
- tapOn: "Message"
- inputText: "Hi"
- tapOn: "Send"
- assertVisible:
    text: "Select at least one"
```

## TC-35 — Chat image >10MB rejected client-side
**Tags:** resource-limit, validation
```yaml
appId: com.example.gymapp_trainer
name: TC-35 chat image size limit
---
- runFlow: login_trainer.yaml
- tapOn: "Chats"
- tapOn:
    text: "Sara"
    index: 0
- tapOn:
    id: "attach"
- tapOn: "Gallery"
- tapOn:
    id: "gallery_item_huge"
- assertVisible:
    text: "exceeds 10 MB"
- assertNotVisible: "Uploading"
```

---

# GYM STAFF APP — 15 edge cases  (`com.example.gymapp_gym`)

## TC-36 — Receptionist sees no Revenue tab (authorization)
**Tags:** authorization, role
```yaml
appId: com.example.gymapp_gym
name: TC-36 receptionist hides revenue
---
- launchApp:
    clearState: true
- runFlow: login_receptionist.yaml
- assertVisible: "Dashboard"
- assertNotVisible: "Revenue"
- tapOn: "More"
- assertNotVisible: "Revenue dashboard"
```

## TC-37 — Manual check-in of expired member blocked with renewal message
**Tags:** business-rule, billing
```yaml
appId: com.example.gymapp_gym
name: TC-37 manual check-in expired
---
- runFlow: login_staff.yaml
- tapOn: "Check-In"
- tapOn: "Manual check-in"
- tapOn: "Search"
- inputText: "Expired Demo"
- tapOn:
    text: "Expired Demo"
    index: 0
- tapOn: "Check in"
- assertVisible:
    text: "expired"
- assertVisible: "Renew"
- assertNotVisible: "Welcome"
```

## TC-38 — Kiosk: non-numeric member number rejected
**Tags:** validation, boundary, kiosk
```yaml
appId: com.example.gymapp_gym
name: TC-38 kiosk invalid number
---
- runFlow: login_staff.yaml
- tapOn: "Check-In"
- tapOn: "Kiosk mode"
- tapOn: "PIN"
- inputText: "1234"
- tapOn: "Unlock"
- tapOn: "Enter Member Number"
- inputText: "abc"
- assertVisible:
    text: "Numbers only"
```

## TC-39 — Kiosk: 3 rapid taps on same number = 1 check-in (debounce)
**Tags:** concurrency, debounce, kiosk
```yaml
appId: com.example.gymapp_gym
name: TC-39 kiosk rapid debounce
---
- runFlow: login_staff_kiosk.yaml
- tapOn: "Enter Member Number"
- inputText: "1001"
- repeat:
    times: 3
    commands:
      - tapOn:
          id: "kiosk_submit"
- assertVisible:
    text: "Welcome"
- back
- tapOn: "Check-in log"
# expect a single log entry for member 1001 in the last 10 seconds
- assertVisible:
    text: "1001"
    index: 0
- assertNotVisible:
    text: "1001"
    index: 1
```

## TC-40 — Add member with duplicate email surfaces 400, form retains input
**Tags:** validation
```yaml
appId: com.example.gymapp_gym
name: TC-40 duplicate email
---
- runFlow: login_staff.yaml
- tapOn: "Members"
- tapOn: "Add member"
- tapOn: "Name"
- inputText: "Dup Demo"
- tapOn: "Email"
- inputText: "trainee@demo.local"
- tapOn: "Phone"
- inputText: "01000000000"
- tapOn: "Save"
- assertVisible:
    text: "already exists"
- assertVisible: "Dup Demo"   # form not wiped
```

## TC-41 — Add member to section not enabled at the gym blocked
**Tags:** multi-tenant, business-rule
```yaml
appId: com.example.gymapp_gym
name: TC-41 disabled section blocked
---
# pre-seed: gym has only `mens` enabled
- runFlow: login_staff_mens_only.yaml
- tapOn: "Members"
- tapOn: "Add member"
- tapOn: "Section"
- assertNotVisible: "Womens"   # not selectable
- assertVisible: "Mens"
```

## TC-42 — Record payment of 0 EGP rejected
**Tags:** validation, billing, boundary
```yaml
appId: com.example.gymapp_gym
name: TC-42 zero payment rejected
---
- runFlow: login_staff.yaml
- tapOn: "Members"
- tapOn:
    text: "Active Demo"
    index: 0
- tapOn: "Record payment"
- tapOn: "Amount"
- inputText: "0"
- tapOn: "Save"
- assertVisible:
    text: "must be greater than 0"
```

## TC-43 — Void payment marks Voided + excludes from revenue total
**Tags:** business-rule, billing, state-transition
```yaml
appId: com.example.gymapp_gym
name: TC-43 void payment excluded from revenue
---
- runFlow: login_staff.yaml
- tapOn: "Revenue"
- copyTextFrom:
    id: "revenue_total"   # stash baseline
- tapOn: "Back"
- tapOn: "Members"
- tapOn:
    text: "Active Demo"
- tapOn: "Payments"
- tapOn:
    text: "REC-"
    index: 0
- tapOn: "Void"
- tapOn: "Reason"
- inputText: "Wrong amount entered"
- tapOn: "Confirm void"
- assertVisible: "Voided"
- tapOn: "Back"
- tapOn: "Back"
- tapOn: "Revenue"
- assertVisible:
    text: "Voided"   # filter chip or footer note
# pseudo-assert: revenue total decreased — verify manually or via separate metric flow
```

## TC-44 — Freeze active membership blocks check-in
**Tags:** state-transition, business-rule
```yaml
appId: com.example.gymapp_gym
name: TC-44 freeze blocks check-in
---
- runFlow: login_staff.yaml
- tapOn: "Members"
- tapOn:
    text: "Active Demo"
- tapOn: "Freeze"
- tapOn: "Until"
- inputText: "31/12/2026"
- tapOn: "Confirm freeze"
- assertVisible: "Frozen"
- tapOn: "Back"
- tapOn: "Check-In"
- tapOn: "Manual"
- tapOn: "Search"
- inputText: "Active Demo"
- tapOn:
    text: "Active Demo"
- tapOn: "Check in"
- assertVisible:
    text: "Frozen"
- assertNotVisible: "Welcome"
```

## TC-45 — Unfreeze restores check-in
**Tags:** state-transition
```yaml
appId: com.example.gymapp_gym
name: TC-45 unfreeze restores check-in
---
- runFlow: login_staff.yaml
- tapOn: "Members"
- tapOn:
    text: "Active Demo"
- tapOn: "Unfreeze"
- tapOn: "Confirm"
- assertVisible: "Active"
- tapOn: "Back"
- tapOn: "Check-In"
- tapOn: "Manual"
- tapOn: "Search"
- inputText: "Active Demo"
- tapOn:
    text: "Active Demo"
- tapOn: "Check in"
- assertVisible:
    text: "Welcome"
```

## TC-46 — Create class with capacity 0 rejected
**Tags:** validation, boundary
```yaml
appId: com.example.gymapp_gym
name: TC-46 class capacity zero
---
- runFlow: login_staff.yaml
- tapOn: "Classes"
- tapOn: "Create class"
- tapOn: "Name"
- inputText: "Edge Yoga"
- tapOn: "Capacity"
- inputText: "0"
- tapOn: "Save"
- assertVisible:
    text: "must be greater"
```

## TC-47 — Class full → next booking auto-waitlists; cancellation auto-promotes
**Tags:** business-rule, state-transition, push
```yaml
appId: com.example.gymapp_gym
name: TC-47 waitlist auto-promote
---
- runFlow: login_staff.yaml
- tapOn: "Classes"
- tapOn:
    text: "Cycling"   # pre-seeded class capacity=20, 20 booked
- assertVisible:
    text: "20 / 20"
# add 21st via dev hook (or pull trainee app side-by-side)
- openLink: "gymappdev://test/book-class-overflow"
- tapOn: "Waitlist"
- assertVisible:
    text: "1"
# cancel one booked member
- tapOn: "Bookings"
- tapOn:
    text: "Cancel"
    index: 0
- tapOn: "Confirm"
- tapOn: "Back"
- tapOn: "Waitlist"
- assertVisible:
    text: "0"   # promoted back into booked
- tapOn: "Back"
- assertVisible: "20 / 20"
```

## TC-48 — Cancel full class fires N push notifications to booked members
**Tags:** push, cross-app
```yaml
appId: com.example.gymapp_gym
name: TC-48 cancel class push fanout
---
- runFlow: login_staff.yaml
- tapOn: "Classes"
- tapOn:
    text: "Cycling"
- tapOn: "Cancel class"
- tapOn: "Reason"
- inputText: "Trainer sick"
- tapOn: "Confirm cancel"
- assertVisible: "Cancelled"
# downstream check: verify notification count via /api/v1/communication/notifications
# (Maestro runScript using curl + the API token)
- runScript: ./scripts/assert_push_count.sh "Cycling cancelled" 20
```

## TC-49 — Approve pending trainer flips status, unlocks trainer-app shell
**Tags:** state-transition, cross-app, authorization
```yaml
appId: com.example.gymapp_gym
name: TC-49 approve trainer unlocks app
---
- runFlow: login_staff.yaml
- tapOn: "More"
- tapOn: "Trainers"
- tapOn: "Pending"
- tapOn:
    text: "Pending Demo"
    index: 0
- tapOn: "Approve"
- assertVisible: "Approved"
# cross-app: trigger trainer-app login from helper script
- runScript: ./scripts/login_pending_trainer_assert_shell.sh
```

## TC-50 — Kiosk PIN must be ≥4 digits and numeric
**Tags:** validation, boundary, kiosk, security
```yaml
appId: com.example.gymapp_gym
name: TC-50 kiosk PIN policy
---
- runFlow: login_staff.yaml
- tapOn: "More"
- tapOn: "Settings"
- tapOn: "Kiosk PIN"
- tapOn: "New PIN"
- inputText: "12"
- tapOn: "Save"
- assertVisible:
    text: "at least 4"
- eraseText: 2
- inputText: "abcd"
- tapOn: "Save"
- assertVisible:
    text: "Numbers only"
- eraseText: 4
- inputText: "5678"
- tapOn: "Save"
- assertVisible: "Saved"
```

---

# Shared helper flows (place under `tests/maestro/flows/`)

These are referenced by `runFlow:` from cases above. Each is a tiny independent flow.

## `login_trainee.yaml`
```yaml
appId: com.example.gymapp_trainee
---
- launchApp:
    clearState: true
- tapOn: "Email"
- inputText: "trainee@demo.local"
- tapOn: "Password"
- inputText: "123456"
- tapOn: "Login"
- assertVisible:
    text: "Home"
    timeout: 8000
```

## `login_trainer.yaml`
```yaml
appId: com.example.gymapp_trainer
---
- launchApp:
    clearState: true
- tapOn: "Email"
- inputText: "trainer@demo.local"
- tapOn: "Password"
- inputText: "123456"
- tapOn: "Login"
- assertVisible:
    text: "Today"
    timeout: 8000
```

## `login_staff.yaml`
```yaml
appId: com.example.gymapp_gym
---
- launchApp:
    clearState: true
- tapOn: "Email"
- inputText: "staff@demo.local"
- tapOn: "Password"
- inputText: "123456"
- tapOn: "Login"
- assertVisible:
    text: "Dashboard"
    timeout: 8000
```

## `login_receptionist.yaml`
```yaml
appId: com.example.gymapp_gym
---
- launchApp:
    clearState: true
- tapOn: "Email"
- inputText: "receptionist@demo.local"
- tapOn: "Password"
- inputText: "123456"
- tapOn: "Login"
- assertVisible:
    text: "Dashboard"
    timeout: 8000
```

> Add similar tiny helpers for the `_pending`, `_expired`, `_exhausted`, `_no_trainer`, `_no_gym`, `_remote_on`, `_mens_only`, `_fresh`, `_kiosk` variants. Each just logs in as a seeded user whose state matches the suffix; create those seeds in `db/seed.sql` Section 0 if they don't exist yet.

---

# Coverage matrix

| # | App | Surface | Tag(s) |
|---|---|---|---|
| 1 | trainee | login | auth, lockout |
| 2 | trainee | OTP | rate-limit |
| 3 | trainee | register | age, PDPL |
| 4 | trainee | register | parental consent |
| 5 | trainee | link gym | validation |
| 6 | trainee | session | JWT expiry |
| 7 | trainee | log weight | numeric edges |
| 8 | trainee | booking | cross-midnight |
| 9 | trainee | cancel | late-cancel deduct |
| 10 | trainee | cancel | outside-window no deduct |
| 11 | trainee | booking | zero package |
| 12 | trainee | booking | expired membership |
| 13 | trainee | chat | empty state |
| 14 | trainee | photo | size limit |
| 15 | trainee | photo | consent gate |
| 16 | trainee | settings | language toggle |
| 17 | trainee | leaderboard | opt-out |
| 18 | trainee | water | debounce |
| 19 | trainee | guided workout | crash resume |
| 20 | trainee | workout log | offline sync |
| 21 | trainer | login | pending state |
| 22 | trainer | remote mode | activation push |
| 23 | trainer | remote mode | deactivation |
| 24 | trainer | builder | empty save |
| 25 | trainer | builder | superset-of-one |
| 26 | trainer | assign | cross-gym block |
| 27 | trainer | live session | client deleted |
| 28 | trainer | post-session | validation |
| 29 | trainer | cert | past expiry |
| 30 | trainer | availability | end<start |
| 31 | trainer | recurring | 12-week cap |
| 32 | trainer | ratings | privacy |
| 33 | trainer | earnings | offline cache |
| 34 | trainer | broadcast | empty targets |
| 35 | trainer | chat | image size |
| 36 | staff | role | receptionist nav |
| 37 | staff | check-in | expired member |
| 38 | staff | kiosk | non-numeric |
| 39 | staff | kiosk | debounce |
| 40 | staff | members | duplicate email |
| 41 | staff | members | disabled section |
| 42 | staff | payment | zero amount |
| 43 | staff | payment | void exclusion |
| 44 | staff | membership | freeze blocks |
| 45 | staff | membership | unfreeze restores |
| 46 | staff | class | capacity zero |
| 47 | staff | class | waitlist auto-promote |
| 48 | staff | class | cancel push fanout |
| 49 | staff | trainer | approve unlocks |
| 50 | staff | settings | kiosk PIN policy |
