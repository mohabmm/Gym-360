# Gym 360 Trainer — Google Play release guide

| | |
|---|---|
| Package name | `com.gym360.trainer` (permanent once uploaded) |
| Bundle | `gym360-trainer-1.0.0-2.aab` (repo root, gitignored). Version 1.0.0, versionCode 2 |
| Signing | Upload key `android/gym360-trainer-upload.jks` (CN=Gym 360 Trainer). **Back it up and keep `key.properties` passwords safe.** |
| Backend | `https://staging-api.gym-365.com` (default `ApiBaseUrl`). HTTPS only in release |
| Push (FCM) | **Off.** No `google-services.json` yet, so the app runs without notifications |
| Target track | Internal testing |

Rebuild: `cd gymapp-mobile/apps/gymapp_trainer && flutter build appbundle --release`.
For production later, add `--dart-define=API_BASE_URL=https://<prod-api>` and bump `version:` in `pubspec.yaml` (versionCode must increase on every upload).

---

## 1. Create the app
Play Console → **Create app**
- App name: `Gym 360 Trainer`
- Default language: **Arabic – ar** (add English – en-US as a translation)
- App or game: **App** · Free or paid: **Free**
- Accept the declarations → **Create app**

## 2. Upload to Internal testing
**Test and release → Testing → Internal testing**
1. **Testers** tab → create an email list (your team) → save.
2. **Create new release** → accept **Play App Signing** (Google-managed app signing key) when asked.
3. Upload `gym360-trainer-1.0.0-2.aab`.
4. Release name: `1.0.0 (2)` · Release notes:
   ```
   <ar>الإصدار الأول من تطبيق Gym 360 للمدربين.</ar>
   <en-US>First release of the Gym 360 Trainer app.</en-US>
   ```
5. **Next → Save**. Rollout stays blocked until step 3 is done.

## 3. App content (Policy → App content). All required
| Section | Answer |
|---|---|
| Privacy policy | Host `PRIVACY_POLICY.md` (fill in the placeholders), then paste the URL |
| App access | **All or some functionality is restricted.** Add a working staging trainer login (email + password) with instructions: "Log in with these credentials. The account is already approved by the demo gym." New trainer sign-ups wait for gym approval, so reviewers need a ready account |
| Ads | **No, my app does not contain ads** |
| Content rating | Questionnaire → category **Utility, Productivity, Communication, or Other**. Answer No to violence, sexual content, drugs and gambling. Answer **Yes** to "users can interact/exchange content" (in-app messages). Expect a rating of Everyone / 3+ |
| Target audience | **18 and over** only. The app is not designed for children |
| News app | No |
| COVID-19 tracing | No |
| Data safety | See below |
| Government app | No |
| Financial features | None |
| Health apps | Select **Fitness / Activity tracking** (trainers manage client workouts and nutrition). It is not a medical device |

### Data safety answers
- Does the app collect or share user data? **Yes**
- Is all data encrypted in transit? **Yes** (release builds are HTTPS-only)
- Can users request data deletion? **Yes**. In-app: Settings → Privacy & Data → Delete Account. Play also asks for a **web URL** for deletion requests. Use a page or email form such as `https://gym-365.com/delete-account`.
- Shared with third parties? **No** (hosting and Firebase are service providers, not "sharing").

| Data type | Collected | Purpose | Optional? |
|---|---|---|---|
| Personal info → Name | ✓ | App functionality, Account management | Required |
| Personal info → Email address | ✓ | App functionality, Account management | Required |
| Personal info → Phone number | ✓ | App functionality, Account management | Required |
| Personal info → User IDs | ✓ | App functionality, Account management | Required |
| Health and fitness → Fitness info | ✓ | App functionality | Required |
| Health and fitness → Health info (client weight / body metrics) | ✓ | App functionality | Required |
| Messages → Other in-app messages | ✓ | App functionality | Required |
| Financial info → Purchase history (client packages / revenue) | ✓ | App functionality | Required |
| App info and performance → Crash logs | ✓ | Analytics (diagnostics) | Required |
| App info and performance → Diagnostics (device model, OS version) | ✓ | Analytics (diagnostics) | Required |
| Device or other IDs (FCM token) | Add **when push is enabled** | App functionality | Required |

Not collected: location, contacts, photos/videos, audio, files, calendar, web history.

## 4. Store listing (Grow → Store presence → Main store listing)

**Arabic (default)**
- App name (≤30): `Gym 360 للمدربين`
- Short description (≤80): `أدر عملاءك وحصصك وبرامج التمارين والتغذية من مكان واحد.`
- Full description:
  ```
  Gym 360 للمدربين هو تطبيق المدرب الشخصي داخل منصة Gym 360 لإدارة الصالات الرياضية.

  • عملاؤك: تابع كل متدرب، تمارينه، وزنه وتقدمه.
  • الحصص: حدّد أوقات تواجدك، أكّد الحجوزات، وتابع جدولك اليومي.
  • برامج التمارين والتغذية: صمّم البرامج وأرسلها لعملائك.
  • الرسائل والإعلانات: تواصل مع عملائك وتابع إعلانات الصالة.
  • الإيرادات: تابع باقات عملائك وأرباحك.
  • التحفيز: نقاط وإنجازات تشجع عملاءك على الاستمرار.

  التطبيق يدعم العربية والإنجليزية بالكامل. يتطلب حسابًا في صالة رياضية مشتركة في Gym 360 وموافقة إدارة الصالة.
  ```

**English (en-US translation)**
- App name: `Gym 360 Trainer`
- Short description: `Manage your clients, sessions, workout and nutrition plans in one place.`
- Full description:
  ```
  Gym 360 Trainer is the personal-trainer app of the Gym 360 gym management platform.

  • Clients: follow every trainee's workouts, weight and progress.
  • Sessions: set your availability, confirm bookings and see your day at a glance.
  • Workout & nutrition plans: build programs and assign them to clients.
  • Messages & announcements: stay in touch with clients and your gym.
  • Revenue: track client packages and your earnings.
  • Motivation: points and achievements that keep clients coming back.

  Fully bilingual (Arabic / English). Requires an account at a Gym 360 member gym, approved by the gym's management.
  ```

**Graphics**
- App icon: 512×512 PNG. Export from `assets/images/logo.png` / the iOS 1024 icon (`ios/Runner/Assets.xcassets/AppIcon.appiconset/Icon-App-1024x1024@1x.png`) resized to 512.
- Feature graphic: 1024×500 PNG/JPG (required).
- Phone screenshots: at least 2 (Play recommends 4+), 9:16, min 320 px side. Take them from the app logged in to staging, in Arabic.
- Category: **Health & Fitness** · Contact email: required · Website: `https://gym-365.com`

## 5. Roll out
Internal testing → **Review release** → fix any errors shown → **Start rollout to Internal testing**.
Copy the **opt-in link** from the Testers tab and send it to testers. They must accept it, then install from Play.

## Before Production (later)
- [ ] Production API URL via `--dart-define=API_BASE_URL=…`
- [ ] Firebase Android app `com.gym360.trainer` → `android/app/google-services.json` + `com.google.gms.google-services` Gradle plugin; then add "Device or other IDs" to Data safety
- [ ] Personal developer accounts created after Nov 2023 must run **Closed testing with 12+ testers for 14 days** before Production access
- [ ] Optional: Play service-account JSON → automate Android uploads in `codemagic.yaml` like the iOS TestFlight workflows
