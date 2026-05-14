import { NextResponse } from 'next/server';
import { getServerSession } from 'next-auth';
import { authOptions } from '@/lib/auth';

/**
 * Proxy for the Owner gym chooser flow. Re-exchanges the Owner-scope token
 * for a gym-scoped token via the API, then returns the new token + gym info
 * for the client to call session.update() with.
 *
 * Body: { gymId: string }
 */
export async function POST(request: Request) {
  const session = await getServerSession(authOptions);
  if (!session?.accessToken) {
    return NextResponse.json({ error: 'Unauthenticated' }, { status: 401 });
  }

  const { gymId } = (await request.json().catch(() => ({}))) as { gymId?: string };
  if (!gymId) {
    return NextResponse.json({ error: 'gymId is required' }, { status: 400 });
  }

  const apiBase = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:8080/api/v1';

  const res = await fetch(`${apiBase}/identity/select-gym`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${session.accessToken}`,
    },
    body: JSON.stringify({ gymId }),
  });

  if (!res.ok) {
    const text = await res.text();
    return NextResponse.json({ error: text || 'select-gym failed' }, { status: res.status });
  }

  const data = await res.json();
  const { accessToken, refreshToken, user } = data.data ?? {};
  if (!accessToken || !user) {
    return NextResponse.json({ error: 'Malformed response' }, { status: 502 });
  }

  const payload = JSON.parse(Buffer.from(accessToken.split('.')[1], 'base64').toString('utf-8'));

  // The client uses next-auth `useSession().update(...)` to merge these into
  // the JWT — see lib/auth.ts jwt callback (trigger === 'update').
  return NextResponse.json({
    accessToken,
    refreshToken,
    gymId: user.gymId ?? payload.gymId ?? gymId,
    gymName: user.gymName ?? '',
    schemaPrefix: payload.schemaPrefix ?? '',
  });
}
