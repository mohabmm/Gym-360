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
const jsonError = (error: string, status: number) => NextResponse.json({ error }, { status });

function decodeJwtPayload(token: string) {
  return JSON.parse(Buffer.from(token.split('.')[1], 'base64').toString('utf-8'));
}

async function exchangeGymToken(accessToken: string, gymId: string) {
  const apiBase = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:8080/api/v1';
  return fetch(`${apiBase}/identity/select-gym`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify({ gymId }),
  });
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function toSessionUpdate(data: any, gymId: string) {
  const { accessToken, refreshToken, user } = data.data ?? {};
  if (!accessToken || !user) {
    return jsonError('Malformed response', 502);
  }

  const payload = decodeJwtPayload(accessToken);

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

export async function POST(request: Request) {
  const session = await getServerSession(authOptions);
  if (!session?.accessToken) {
    return jsonError('Unauthenticated', 401);
  }

  const { gymId } = (await request.json().catch(() => ({}))) as { gymId?: string };
  if (!gymId) {
    return jsonError('gymId is required', 400);
  }

  const res = await exchangeGymToken(session.accessToken, gymId);
  if (!res.ok) {
    const text = await res.text();
    return jsonError(text || 'select-gym failed', res.status);
  }

  return toSessionUpdate(await res.json(), gymId);
}
