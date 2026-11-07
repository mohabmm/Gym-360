# Gym 360 Staff (gym app): Google Play release guide

Shared Console steps (create app, Internal testing, rollout) are the same as in
[`../trainer/PLAY_RELEASE_GUIDE.md`](../trainer/PLAY_RELEASE_GUIDE.md). This file lists only what differs.

| | |
|---|---|
| App source | `gymapp-mobile/apps/gymapp_gym` |
| Package name | `com.gym360.staff` (permanent once uploaded) |
| Bundle | `gym360-staff-1.0.0-1.aab` (repo root, gitignored). Version 1.0.0, versionCode 1 |
| Signing | Upload key `android/gym360-staff-upload.jks` (CN=Gym 360 Staff) + `android/key.properties`. **Back both up.** |
| Backend | `https://staging-api.gym-365.com`. HTTPS only in release |
| Push (FCM) | **Off** (no `google-services.json` yet) |
| Permissions | Internet, notifications, **camera** (kiosk check-in QR scanning; optional, `required=false`) |

Rebuild: `cd gymapp-mobile/apps/gymapp_gym && flutter build appbundle --release`.

## Create app
- App name: `Gym 360 Staff` · Default language **Arabic** (+ English) · App · Free

## Internal testing release notes
```
<ar>الإصدار الأول من تطبيق Gym 360 لإدارة الصالة.</ar>
<en-US>First release of the Gym 360 Staff app.</en-US>
```

## App content
| Section | Answer |
|---|---|
| Privacy policy | Host `PRIVACY_POLICY.md` (fill in the placeholders) and paste the URL |
| App access | **Restricted.** Give a staging **owner or manager** login for the demo gym (a receptionist login hides settings screens) and the kiosk PIN if reviewers should try kiosk mode |
| Ads | No |
| Content rating | Category **Utility, Productivity, Communication, or Other**. Answer No to all content questions. Users can't interact with each other, except gym announcements to members |
| Target audience | **18 and over** |
| Health apps | None. This is a business management tool |
| Financial features | None. The app records payments taken at the gym (cash/manual) but processes no payments itself |

### Data safety
The app collects data about **staff users** and about **gym members** entered by staff.

- Collects data: **Yes** · Encrypted in transit: **Yes** · Shared with third parties: **No**
- Deletion: **Yes**. Staff accounts are created by the gym, so there's no in-app sign-up and Play only needs a **web URL** for deletion requests (e.g. `https://gym-365.com/delete-account`). Member records are deleted by the gym owner or on the member's request.

| Data type | Purpose | Required? |
|---|---|---|
| Personal info → Name, Email, Phone, User IDs | App functionality, Account management | Required |
| Personal info → Address | App functionality | Optional |
| Personal info → Other info (**national ID**) | App functionality | Optional |
| Photos and videos → Photos (member/gym photos, branding) | App functionality | Optional |
| Financial info → Purchase history (membership payments recorded) | App functionality | Required |
| App activity → Other actions (check-ins, class attendance) | App functionality | Required |
| App info and performance → Crash logs, Diagnostics | Analytics | Required |
| Device or other IDs (FCM token) | **Add when push is enabled** | |

Camera frames used for kiosk QR scanning are processed on the device and never uploaded.

## Store listing

**Arabic (default)**
- Name: `Gym 360 لإدارة الصالة`
- Short (≤80): `أدر صالتك الرياضية: الأعضاء، الاشتراكات، الحضور، والكلاسات من موبايلك.`
- Full:
  ```
  Gym 360 لإدارة الصالة هو تطبيق المالك والمدير وموظف الاستقبال في منصة Gym 360.

  • الأعضاء: أضف الأعضاء وتابع اشتراكاتهم وتجديداتهم.
  • الحضور: وضع الكشك لتسجيل دخول الأعضاء بمسح رمز QR.
  • المدفوعات: سجّل مدفوعات الاشتراكات وتابع الإيرادات.
  • الكلاسات والمدربين: نظّم الجداول والمدربين.
  • لوحة التحكم: أرقام الصالة اليومية في مكان واحد.
  • الإعلانات: أرسل إعلانات لأعضاء صالتك.
  • الإعدادات: أقسام الصالة، مواعيد العمل الموسمية، سياسات الإلغاء، والهوية البصرية.

  يدعم العربية والإنجليزية بالكامل. يتطلب حساب موظف في صالة مشتركة في Gym 360.
  ```

**English (en-US)**
- Name: `Gym 360 Staff`
- Short: `Run your gym: members, memberships, check-ins and classes from your phone.`
- Full:
  ```
  Gym 360 Staff is the app for gym owners, managers and receptionists on the Gym 360 platform.

  • Members: add members and track their memberships and renewals.
  • Check-in: kiosk mode checks members in by scanning their QR code.
  • Payments: record membership payments and follow revenue.
  • Classes & trainers: manage schedules and trainers.
  • Dashboard: your gym's daily numbers in one place.
  • Announcements: message your members.
  • Settings: gym sections, seasonal opening hours, cancellation policies and branding.

  Fully bilingual (Arabic / English). Requires a staff account at a Gym 360 gym.
  ```

- Category: **Business**
- Graphics: 512×512 icon (from `ios/Runner/Assets.xcassets/AppIcon.appiconset/Icon-App-1024x1024@1x.png`), 1024×500 feature graphic, 2+ phone screenshots. Tablet screenshots are recommended because kiosk mode runs on tablets
