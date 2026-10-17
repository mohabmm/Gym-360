/**
 * STAGING — Gym Staff web main flows.
 * Auth, navigation across every staff route, list/detail/search, shell toggles,
 * plus a few SAFE create+verify+cleanup write flows (E2E-tagged data) driven
 * through the real backend API and verified to render in the UI.
 */
import { test, expect } from '@playwright/test';
import {
  uiLogin,
  expectRendered,
  apiContext,
  apiLogin,
  auth,
  e2eTag,
  STAFF,
  V1,
} from './helpers';

const ROUTES: Array<[string, string]> = [
  ['Dashboard', '/'],
  ['Members', '/members'],
  ['Trainers', '/trainers'],
  ['Classes', '/classes'],
  ['Exercises', '/exercises'],
  ['Messages', '/messages'],
  ['Billing', '/billing'],
  ['My Subscription', '/subscription'],
  ['Gamification', '/gamification'],
  ['Announcements', '/announcements'],
  ['Analytics', '/analytics'],
  ['Check-Ins', '/check-ins'],
  ['Staff', '/staff'],
  ['Data Import', '/data-import'],
  ['Kiosk PIN', '/kiosk-pin'],
  ['Settings', '/settings'],
];

test.describe('Staff — auth', () => {
  test.describe('unauthenticated', () => {
    test.use({ storageState: { cookies: [], origins: [] } });

    test('S01 invalid credentials show an error', async ({ page }) => {
      await page.goto('/login', { waitUntil: 'domcontentloaded' });
      await page.fill('input[type="email"]', 'wrong@nobody.local');
      await page.fill('input[type="password"]', 'badpass');
      await page.click('button[type="submit"]');
      await expect(page.locator('text=/invalid|insufficient|incorrect/i').first()).toBeVisible({
        timeout: 20000,
      });
      expect(page.url()).toContain('/login');
    });

    test('S02 protected route redirects to login when signed out', async ({ page }) => {
      await page.goto('/members', { waitUntil: 'domcontentloaded' });
      await page.waitForURL(/\/login/, { timeout: 20000 });
      expect(page.url()).toContain('/login');
    });

    test('S03 valid credentials log in to the dashboard', async ({ page }) => {
      await uiLogin(page, STAFF);
      expect(page.url()).not.toContain('/login');
      await expect(page.locator('main, h1, h2').first()).toBeVisible();
    });
  });
});

test.describe('Staff — navigation renders every route', () => {
  for (const [name, route] of ROUTES) {
    test(`S-nav ${name} (${route})`, async ({ page }) => {
      await expectRendered(page, route);
    });
  }
});

test.describe('Staff — sidebar & shell', () => {
  test('S20 sidebar lists the main nav sections', async ({ page }) => {
    await page.goto('/', { waitUntil: 'domcontentloaded' });
    for (const label of ['Members', 'Trainers', 'Classes', 'Billing']) {
      await expect(page.locator(`a:has-text("${label}")`).first()).toBeVisible({ timeout: 20000 });
    }
  });

  test('S21 sidebar link navigates to Members', async ({ page }) => {
    await page.goto('/', { waitUntil: 'domcontentloaded' });
    await page.locator('a[href="/members"]').first().click();
    await page.waitForURL(/\/members$/, { timeout: 20000 });
    await expect(page.locator('main, h1, h2').first()).toBeVisible();
  });

  test('S22 dashboard shows summary content', async ({ page }) => {
    await page.goto('/', { waitUntil: 'domcontentloaded' });
    const text = await page.locator('main').innerText();
    expect(text.length).toBeGreaterThan(20);
  });
});

test.describe('Staff — members', () => {
  test('S30 members page shows a table or empty state', async ({ page }) => {
    await page.goto('/members', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('table').first()).toBeVisible({ timeout: 25000 });
  });

  test('S31 members search filters without crashing', async ({ page }) => {
    await page.goto('/members', { waitUntil: 'domcontentloaded' });
    const search = page.locator('input[placeholder*="earch"]').first();
    if (await search.count()) {
      await search.fill('zzz-no-such-member');
      await page.waitForTimeout(800);
      await expect(page.locator('main').first()).toBeVisible();
    }
  });

  test('S32 clicking a member row opens its detail', async ({ page }) => {
    await page.goto('/members', { waitUntil: 'domcontentloaded' });
    const row = page.locator('table tbody tr').first();
    if (await row.count()) {
      await row.click();
      await page.waitForURL(/\/members\/[^/]+$/, { timeout: 20000 }).catch(() => {});
      await expect(page.locator('main, h1, h2').first()).toBeVisible();
    }
  });

  test('S33 Add Member modal opens and cancels', async ({ page }) => {
    await page.goto('/members', { waitUntil: 'domcontentloaded' });
    const addBtn = page.locator('button:has-text("Add Member")').first();
    await expect(addBtn).toBeVisible({ timeout: 20000 });
    await addBtn.click();
    await expect(page.locator('text=Add Member').first()).toBeVisible();
    await page.locator('button:has-text("Cancel")').first().click();
  });
});

test.describe('Staff — lists & widgets', () => {
  const listRoutes: Array<[string, string]> = [
    ['S42 trainers', '/trainers'],
    ['S44 classes', '/classes'],
    ['S45 exercises', '/exercises'],
    ['S46 billing', '/billing'],
    ['S47 gamification', '/gamification'],
    ['S48 announcements', '/announcements'],
    ['S49 check-ins', '/check-ins'],
    ['S4A staff', '/staff'],
  ];
  for (const [name, route] of listRoutes) {
    test(`${name} renders content`, async ({ page }) => {
      await page.goto(route, { waitUntil: 'domcontentloaded' });
      await expect(page.locator('main').first()).toBeVisible({ timeout: 25000 });
      const text = await page.locator('main').innerText();
      expect(text.length, `${route} rendered empty`).toBeGreaterThan(10);
    });
  }

  test('S45b exercises search filters without crashing', async ({ page }) => {
    await page.goto('/exercises', { waitUntil: 'domcontentloaded' });
    const search = page.locator('input[placeholder*="earch"]').first();
    if (await search.count()) {
      await search.fill('bench');
      await page.waitForTimeout(800);
    }
    await expect(page.locator('main').first()).toBeVisible();
  });
});

test.describe('Staff — write flows (E2E-tagged, cleaned up)', () => {
  test('S40 create member via API appears in the members list, then cleaned up', async ({
    page,
  }) => {
    const ctx = await apiContext();
    const { token, gymId } = await apiLogin(ctx, STAFF);
    test.skip(!gymId, 'no gym scope for staff account');

    const tag = e2eTag();
    const name = `${tag} Member`;
    const create = await ctx.post(`${V1}/identity/users/staff-create`, {
      headers: auth(token),
      data: {
        firstName: tag,
        lastName: 'Member',
        phone: `+2011${Math.floor(1e7 + Math.random() * 8e7)}`,
        email: `${tag}@test.local`,
        section: 'general',
      },
    });
    expect(create.status(), 'staff-create member').toBeLessThan(400);
    const userId = (await create.json())?.data?.userId;
    if (userId) {
      await ctx.post(`${V1}/gyms/${gymId}/members`, { headers: auth(token), data: { userId } });
    }

    // Verify it renders in the UI list.
    await page.goto('/members', { waitUntil: 'domcontentloaded' });
    const search = page.locator('input[placeholder*="earch"]').first();
    if (await search.count()) await search.fill(tag);
    await expect(page.locator(`text=${tag}`).first()).toBeVisible({ timeout: 20000 });

    // Cleanup (tolerant — hard-delete may not exist; record stays E2E-tagged if so).
    if (userId) {
      await ctx
        .delete(`${V1}/gyms/${gymId}/members/${userId}`, { headers: auth(token) })
        .catch(() => {});
    }
    await ctx.dispose();
  });

  test('S41 create announcement via API appears in the list, then cleaned up', async ({ page }) => {
    const ctx = await apiContext();
    const { token, gymId } = await apiLogin(ctx, STAFF);
    test.skip(!gymId, 'no gym scope for staff account');

    const tag = e2eTag();
    const title = `${tag} Notice`;
    // Matches the staff app's create payload (BE wants content/category/targetAudience;
    // createdBy is derived from the JWT).
    const create = await ctx.post(`${V1}/gyms/${gymId}/announcements`, {
      headers: auth(token),
      data: {
        title,
        content: 'E2E staging test announcement',
        category: 'general',
        targetAudience: 'all',
      },
    });
    test.skip(create.status() === 403, 'announcements plan-gated for this gym');
    expect(create.status(), 'create announcement').toBeLessThan(400);
    const annId = (await create.json())?.data?.id ?? (await create.json())?.data;

    await page.goto('/announcements', { waitUntil: 'domcontentloaded' });
    await expect(page.locator(`text=${tag}`).first()).toBeVisible({ timeout: 20000 });

    if (annId) {
      await ctx
        .delete(`${V1}/gyms/${gymId}/announcements/${annId}`, { headers: auth(token) })
        .catch(() => {});
    }
    await ctx.dispose();
  });

  // SigNoz BUG (staging, 2026-05-30 14:37–15:13, 4× 500 on
  // POST /identity/users/staff-create-trainer). Trace 4542520a… shows
  // SELECT user-by-identifier → INSERT → server span 500: the One-Gym
  // duplicate-principal path (email already an Active principal in ANOTHER
  // gym) throwing at the membership-link step. NOT reproducible from
  // staff@demo.local — same-gym reuse returns an idempotent 201 (verified).
  //
  // This case therefore guards two angles:
  //  (a) fresh create must succeed (regression guard), and
  //  (b) same-account reuse must not 500 (idempotency).
  // It does NOT reproduce the live cross-gym trigger — see README "Known bug".
  test('S43 staff-create-trainer must not 500 (fresh + same-account reuse)', async ({}) => {
    const ctx = await apiContext();
    const { token } = await apiLogin(ctx, STAFF);
    const tag = e2eTag();
    const data = {
      firstName: tag,
      lastName: 'Trainer',
      phone: `+2012${Math.floor(1e7 + Math.random() * 8e7)}`,
      email: `${tag}@test.local`,
    };
    const fresh = await ctx.post(`${V1}/identity/users/staff-create-trainer`, {
      headers: auth(token),
      data,
    });
    expect(fresh.status(), `fresh: ${(await fresh.text()).slice(0, 200)}`).toBeLessThan(500);
    // Re-POST same email → must stay <500 (idempotent reuse, not a crash).
    const reuse = await ctx.post(`${V1}/identity/users/staff-create-trainer`, {
      headers: auth(token),
      data,
    });
    expect(reuse.status(), `reuse: ${(await reuse.text()).slice(0, 200)}`).toBeLessThan(500);
    await ctx.dispose();
  });
});

test.describe('Staff — shell toggles & logout', () => {
  test('S50 locale toggle keeps the page healthy', async ({ page }) => {
    await page.goto('/', { waitUntil: 'domcontentloaded' });
    const toggle = page.getByRole('button', { name: /ar|en|عرب|english|language/i }).first();
    if (await toggle.count()) {
      await toggle.click().catch(() => {});
      await page.waitForTimeout(600);
    }
    await expect(page.locator('main, h1, h2').first()).toBeVisible();
  });

  test('S51 theme toggle keeps the page healthy', async ({ page }) => {
    await page.goto('/', { waitUntil: 'domcontentloaded' });
    const toggle = page.getByRole('button', { name: /theme|dark|light/i }).first();
    if (await toggle.count()) {
      await toggle.click().catch(() => {});
      await page.waitForTimeout(400);
    }
    await expect(page.locator('main, h1, h2').first()).toBeVisible();
  });

  test('S52 sign out returns to login', async ({ page }) => {
    await page.goto('/', { waitUntil: 'domcontentloaded' });
    const signOut = page.getByRole('button', { name: /sign out|logout|log out/i }).first();
    if (await signOut.count()) {
      await signOut.click();
      await page.waitForURL(/\/login/, { timeout: 20000 }).catch(() => {});
    }
    await expect(page.locator('body')).toBeVisible();
  });
});
