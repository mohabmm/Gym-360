# Staging E2E suite

Playwright tests that drive the **deployed** admin + staff webs against
`staging-api.gym-365.com`. Fully separate from the localhost `playwright.config.ts`.

- Config: `../../playwright.staging.config.ts`
- 71 cases: 28 admin + 43 staff (navigation, lists, detail, search, auth,
  shell toggles, plus safe API-driven create+verify+cleanup write flows).

## Run

```bash
cd gym-360-web
npm run e2e:staging            # everything (admin + staff)
npm run e2e:staging:admin      # admin only
npm run e2e:staging:staff      # staff only
npm run e2e:staging:report     # open last HTML report
```

Override targets / creds via env:

```bash
STAGING_ADMIN=… STAGING_STAFF=… STAGING_API=… \
ADMIN_EMAIL=… ADMIN_PASSWORD=… STAFF_EMAIL=… STAFF_PASSWORD=… \
npm run e2e:staging
```

Defaults: staging URLs + demo accounts `admin@demo.local` / `staff@demo.local`
(`123456`).

## Notes

- The staging WAF rejects non-browser User-Agents; the real Chromium browser is
  fine, and `apiContext()` sets a Chrome UA for direct API calls.
- API URLs are built as `${V1}/path` (full `/api/v1` base). Do **not** lean on
  `APIRequestContext` baseURL — a leading-slash path resets to the origin root
  and silently drops `/api/v1` (→ 404).
- Write flows use unique `e2e<...>` tags and clean up after themselves
  (member/announcement delete is best-effort; tagged rows remain if no
  hard-delete endpoint exists).
## Known bug — staff-create-trainer 500 (cross-gym One-Gym conflict)

SigNoz (staging, 2026-05-30) shows `POST /identity/users/staff-create-trainer`
returning 500. **Confirmed root cause:** a domain/DB mismatch on the One-Gym
rule (CLAUDE.md Rule 11).

- DB index `UX_user_gym_memberships_active_per_user` (migration
  `20260501185445`) is UNIQUE on `UserId WHERE is_active = 1` — one active
  membership per user.
- `User.LinkToGym` (`...Identity.Domain/Entities/User.cs:114`) only blocks
  *same gym + same role + active*; it adds a 2nd active membership in a
  different gym, so `SaveChangesAsync` violates the index →
  `DbUpdateException` → unhandled 500.

Trigger: reuse a trainer (by email/phone) who is already Active in another gym.
Not reproducible from `staff@demo.local` (same-gym reuse is idempotent 201), so
`S43` only guards the fresh-create + same-account-reuse angles. Fix belongs in
the BE (`LinkToGym` should enforce one-active-across-all-gyms and return a clean
error; wrap `SaveChangesAsync` to map the unique violation to 409).
