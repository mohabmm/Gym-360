# Gym 360 Member (trainee app): Google Play release guide

Shared Console steps (create app, Internal testing, rollout) are the same as in
[`../trainer/PLAY_RELEASE_GUIDE.md`](../trainer/PLAY_RELEASE_GUIDE.md). This file lists only what differs.

| | |
|---|---|
| App source | `gymapp-mobile/apps/gymapp_trainee` |
| Package name | `com.gym360.trainee` (permanent once uploaded) |
| Bundle | `gym360-member-1.0.0-1.aab` (repo root, gitignored). Version 1.0.0, versionCode 1 |
| Signing | Upload key `android/gym360-trainee-upload.jks` (CN=Gym 360 Member) + `android/key.properties`. **Back both up.** |
| Backend | `https://staging-api.gym-365.com`. HTTPS only in release |
| Push (FCM) | **Off** (no `google-services.json` yet) |
| Permissions | Internet, notifications, **camera** (scanning the gym QR to link a gym; optional, `required=false`) |

Rebuild: `cd gymapp-mobile/apps/gymapp_trainee && flutter build appbundle --release`.

## Create app
- App name: `Gym 360 Member` · Default language **Arabic** (+ English) · App · Free

## Internal testing release notes
```
<ar>الإصدار الأول من تطبيق Gym 360 للأعضاء.</ar>
<en-US>First release of the Gym 360 Member app.</en-US>
```

## App content
| Section | Answer |
|---|---|
| Privacy policy | Host `PRIVACY_POLICY.md` (fill in the placeholders) and paste the URL |
| App access | **Restricted.** Give a staging member login that is already linked to the demo gym. Note: "Log in with these credentials; the account is already a member of the demo gym." |
| Ads | No |
| Content rating | Category **Utility, Productivity, Communication, or Other**. Answer **Yes** to "users can interact" (messages, leaderboards) and No to everything else. Expect Everyone / 3+ |
| Target audience | **18 and over** (recommended). If gyms will sign up members under 18, PDPL requires guardian consent and Play's Families policy applies, so decide that before choosing a younger age group |
| Health apps | **Fitness / Activity tracking** and **Nutrition / weight management**. Not a medical device |
| Financial features | None (the app shows membership status; payments are taken at the gym) |

### Data safety
- Collects data: **Yes** · Encrypted in transit: **Yes** · Shared with third parties: **No**
- Deletion: **Yes**. In-app via Settings → Delete Account, plus a web URL (e.g. `https://gym-365.com/delete-account`)

| Data type | Purpose | Required? |
|---|---|---|
| Personal info → Name, Email, Phone, User IDs | App functionality, Account management | Required |
| Personal info → Other info (date of birth, gender) | App functionality | Optional |
| Health and fitness → Health info (weight, body measurements) | App functionality | Optional |
| Health and fitness → Fitness info (workouts, nutrition logs) | App functionality | Optional |
| Photos and videos → Photos (progress photos) | App functionality | Optional |
| Messages → Other in-app messages | App functionality | Optional |
| Financial info → Purchase history (membership / packages) | App functionality | Required |
| App activity → Other user-generated content (points, achievements, leaderboards) | App functionality | Optional |
| App info and performance → Crash logs, Diagnostics | Analytics | Required |
| Device or other IDs (FCM token) | **Add when push is enabled** | |

Camera frames used for QR scanning are processed on the device and never uploaded, so the camera is not a collected data type.

## Store listing

**Arabic (default)**
- Name: `Gym 360 للأعضاء`
- Short (≤80): `تمارينك، حجوزاتك، وتقدمك في صالتك الرياضية، كلها في تطبيق واحد.`
- Full:
  ```
  Gym 360 للأعضاء هو تطبيقك داخل صالتك الرياضية المشتركة في منصة Gym 360.

  • الدخول السريع: امسح رمز الصالة واربط حسابك، واستخدم رمز QR الخاص بك لتسجيل الحضور.
  • الحصص: احجز حصصك مع المدرب أو الكلاسات وتابع مواعيدك.
  • التمارين: تابع برنامجك وسجّل تمارينك.
  • التغذية: تابع خطتك الغذائية.
  • تقدمك: سجّل وزنك وقياساتك وصور التقدم وشاهد الرسوم البيانية.
  • العضوية: اعرف حالة اشتراكك وموعد التجديد.
  • التحفيز: نقاط وإنجازات ولوحة المتصدرين.
  • الرسائل والإعلانات من صالتك ومدربك.

  يدعم العربية والإنجليزية بالكامل. يتطلب عضوية في صالة رياضية مشتركة في Gym 360.
  ```

**English (en-US)**
- Name: `Gym 360 Member`
- Short: `Your workouts, bookings and progress at your gym, all in one app.`
- Full:
  ```
  Gym 360 Member is your app at any gym on the Gym 360 platform.

  • Quick access: scan your gym's code to link your account, and check in with your personal QR code.
  • Sessions: book personal-training sessions and classes, and keep track of your schedule.
  • Workouts: follow your program and log every session.
  • Nutrition: follow your meal plan.
  • Progress: log weight, body measurements and progress photos, and see your charts.
  • Membership: see your plan status and renewal date.
  • Motivation: points, achievements and leaderboards.
  • Messages and announcements from your gym and trainer.

  Fully bilingual (Arabic / English). Requires a membership at a Gym 360 gym.
  ```

- Category: **Health & Fitness**
- Graphics: 512×512 icon (from `ios/Runner/Assets.xcassets/AppIcon.appiconset/Icon-App-1024x1024@1x.png`), 1024×500 feature graphic, 2+ phone screenshots (Arabic, logged in to staging)
