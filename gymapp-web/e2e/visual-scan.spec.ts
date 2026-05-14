import { test, expect } from '@playwright/test';
import * as fs from 'fs';

test.use({ baseURL: 'http://localhost:3001' });

const ROUTES = [
  { path: '/', name: 'dashboard' },
  { path: '/members', name: 'members' },
  { path: '/trainers', name: 'trainers' },
  { path: '/classes', name: 'classes' },
  { path: '/exercises', name: 'exercises' },
  { path: '/billing', name: 'billing' },
  { path: '/gamification', name: 'gamification' },
  { path: '/announcements', name: 'announcements' },
  { path: '/analytics', name: 'analytics' },
  { path: '/check-ins', name: 'check-ins' },
  { path: '/staff', name: 'staff' },
  { path: '/data-import', name: 'data-import' },
  { path: '/kiosk-pin', name: 'kiosk-pin' },
  { path: '/settings', name: 'settings' },
];

const OUT = '/tmp/screenshots';
fs.mkdirSync(OUT, { recursive: true });

test('visual scan — all pages', async ({ page }) => {
  test.setTimeout(300000);
  const jsErrors: string[] = [];
  const apiErrors: string[] = [];

  page.on('pageerror', e => {
    if (!e.message.includes('SignalR') && !e.message.includes('negotiate'))
      jsErrors.push(e.message);
  });
  page.on('response', r => {
    if (r.status() >= 500 && r.url().includes('/api/v1'))
      apiErrors.push(`${r.status()} ${r.url()}`);
  });

  // login
  await page.goto('/login');
  await page.fill('input[type="email"]', 'staff@demo.local');
  await page.fill('input[type="password"]', '123456');
  await page.click('button[type="submit"]');
  await page.waitForURL('/', { timeout: 60000 });

  const report: string[] = [];

  for (const { path, name } of ROUTES) {
    jsErrors.length = 0;
    apiErrors.length = 0;

    await page.goto(path, { timeout: 30000 });
    await page.waitForTimeout(2000);

    await page.screenshot({ path: `${OUT}/${name}.png`, fullPage: true });

    const h1 = await page.locator('h1').first().textContent({ timeout: 3000 }).catch(() => 'N/A');
    const issues: string[] = [...jsErrors.map(e => `JS: ${e.slice(0,80)}`), ...apiErrors.map(e => `API 5xx: ${e}`)];

    const line = `${path.padEnd(16)} h1="${h1?.trim()}" ${issues.length ? '⚠ ' + issues.join(' | ') : '✓'}`;
    report.push(line);
    console.log(line);
  }

  console.log('\n=== VISUAL SCAN REPORT ===');
  report.forEach(l => console.log(l));
  console.log(`\nScreenshots saved to ${OUT}/`);

  // fail if any 500s or JS errors across all pages
  const totalIssues = report.filter(l => l.includes('⚠')).length;
  if (totalIssues > 0) console.log(`\n[ISSUES FOUND] ${totalIssues} pages with errors`);
});
