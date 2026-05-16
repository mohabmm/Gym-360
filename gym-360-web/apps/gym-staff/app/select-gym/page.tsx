'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { signOut, useSession } from 'next-auth/react';

export default function SelectGymPage() {
  const router = useRouter();
  const { data: session, status, update } = useSession();
  const [submitting, setSubmitting] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (status === 'unauthenticated') router.replace('/login');
  }, [status, router]);

  // If a gym is already active, skip the chooser.
  useEffect(() => {
    if (session?.user?.gymId) router.replace('/');
  }, [session?.user?.gymId, router]);

  const ownedGyms = session?.user?.ownedGyms ?? [];

  // Owner with exactly one gym shouldn't see the chooser — auto-select.
  useEffect(() => {
    if (status !== 'authenticated') return;
    if (session?.user?.gymId) return;
    if (ownedGyms.length === 1 && submitting === null) {
      void selectGym(ownedGyms[0].gymId);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [status, session?.user?.gymId, ownedGyms.length]);

  async function selectGym(gymId: string) {
    setSubmitting(gymId);
    setError(null);
    try {
      const res = await fetch('/api/select-gym', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ gymId }),
      });

      if (!res.ok) {
        const data = await res.json().catch(() => ({}));
        throw new Error(data.error ?? `Failed (${res.status})`);
      }

      const { accessToken, gymId: newGymId, gymName, schemaPrefix } = await res.json();
      await update({ accessToken, gymId: newGymId, gymName, schemaPrefix });
      router.replace('/');
    } catch (e: any) {
      setError(e.message ?? 'Selection failed');
      setSubmitting(null);
    }
  }

  if (status === 'loading') {
    return <div className="flex min-h-screen items-center justify-center">Loading…</div>;
  }

  if (ownedGyms.length === 0) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-center">
          <p className="mb-4">No owned gyms found for this account.</p>
          <button
            onClick={() => signOut({ callbackUrl: '/login' })}
            className="rounded bg-[#C41E3A] px-4 py-2 text-white"
          >
            Sign out
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4">
      <div className="w-full max-w-md rounded-2xl border border-border bg-card p-6 shadow">
        <h1 className="mb-1 text-2xl font-bold">Select Gym</h1>
        <p className="mb-6 text-sm text-muted-foreground">
          You manage multiple gyms. Pick one to continue. You can switch from the header anytime.
        </p>

        {error && (
          <div className="mb-4 rounded bg-red-50 p-3 text-sm text-red-700" role="alert">
            {error}
          </div>
        )}

        <ul className="space-y-2">
          {ownedGyms.map((gym) => (
            <li key={gym.gymId}>
              <button
                onClick={() => selectGym(gym.gymId)}
                disabled={submitting !== null}
                className="flex w-full items-center justify-between rounded-lg border border-border bg-background px-4 py-3 text-left transition hover:border-[#C41E3A] hover:bg-[#C41E3A]/5 disabled:opacity-50"
              >
                <span>
                  <span className="block font-semibold">{gym.name}</span>
                  <span className="block text-xs text-muted-foreground">{gym.schemaPrefix}</span>
                </span>
                {submitting === gym.gymId ? (
                  <span className="text-xs">…</span>
                ) : (
                  <span className="text-xs text-muted-foreground">Open →</span>
                )}
              </button>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
