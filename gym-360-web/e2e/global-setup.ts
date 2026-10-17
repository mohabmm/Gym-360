/**
 * Playwright globalSetup: ensure the staff app's E2E gym has minimum demo data
 * (members, payments) so suites that assert on table rows / KPIs have something
 * to render. Uses real BE APIs only — no SQL/seed hacks.
 */
import { request } from '@playwright/test';

const API = process.env.API_URL ?? 'http://localhost:8080/api/v1';
const STAFF = { email: 'staff@demo.local', password: '123456' };
const TARGET_MEMBERS = 5;

async function login(ctx: ReturnType<typeof request.newContext> extends Promise<infer T> ? T : never): Promise<{ token: string; gymId: string }> {
  const res = await ctx.post(`${API}/identity/login`, {
    headers: { 'Content-Type': 'application/json' },
    data: { email: STAFF.email, password: STAFF.password },
  });
  if (!res.ok()) throw new Error(`login failed: ${res.status()}`);
  const body = await res.json();
  const token = body?.data?.accessToken as string;
  const payload = JSON.parse(Buffer.from(token.split('.')[1], 'base64').toString('utf-8'));
  return { token, gymId: payload.gymId };
}

async function listMembers(ctx: any, token: string, gymId: string): Promise<any[]> {
  const res = await ctx.get(`${API}/gyms/${gymId}/members?page=1&pageSize=100`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok()) return [];
  const body = await res.json();
  return body?.data?.items ?? body?.data ?? [];
}

function jsonAuthHeaders(token: string) {
  return { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' };
}

function isCreated(res: any): boolean {
  return res.ok() || res.status() === 201;
}

function extractUserId(body: any): string | undefined {
  const data = body?.data;
  return data?.userId ?? data?.id ?? data;
}

async function ensureMember(ctx: any, token: string, gymId: string, idx: number): Promise<void> {
  const phone = `+2010000000${(50 + idx).toString().padStart(2, '0')}`;
  const email = `seedmember${idx}@demo.local`;
  const userRes = await ctx.post(`${API}/identity/users/staff-create`, {
    headers: jsonAuthHeaders(token),
    data: { firstName: `Seed${idx}`, lastName: 'Demo', phone, email },
  });
  if (!isCreated(userRes)) {
    console.warn(`[global-setup] staff-create ${idx} → ${userRes.status()}`);
    return;
  }
  const userBody = await userRes.json().catch(() => ({}));
  const userId = extractUserId(userBody);
  if (!userId) return;
  await ctx.post(`${API}/gyms/${gymId}/members`, {
    headers: jsonAuthHeaders(token),
    data: { userId },
  });
}

export default async function globalSetup() {
  const ctx = await request.newContext();
  try {
    const { token, gymId } = await login(ctx);
    const existing = await listMembers(ctx, token, gymId);
    const need = Math.max(0, TARGET_MEMBERS - existing.length);
    if (need > 0) {
      console.log(`[global-setup] gym ${gymId} has ${existing.length} members, adding ${need}`);
      for (let i = 0; i < need; i++) {
        await ensureMember(ctx, token, gymId, i);
      }
    } else {
      console.log(`[global-setup] gym ${gymId} already has ${existing.length} members; skip seed`);
    }
  } catch (err) {
    console.warn('[global-setup] populate skipped:', (err as Error).message);
  } finally {
    await ctx.dispose();
  }
}
