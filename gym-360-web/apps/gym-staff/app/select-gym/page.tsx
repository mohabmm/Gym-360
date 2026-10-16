'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { signOut, useSession } from 'next-auth/react';

type OwnedGym = { gymId: string; name: string; schemaPrefix: string };

// Owner with exactly one gym and no active gym gets auto-selected (once).
function shouldAutoSelect(status: string, activeGymId: string | undefined, gymCount: number, submitting: string | null) {
  return status === 'authenticated' && !activeGymId && gymCount === 1 && submitting === null;
}

async function requestGymToken(gymId: string) {
  const res = await fetch('/api/select-gym', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ gymId }),
  });

  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    throw new Error(data.error ?? `Failed (${res.status})`);
  }

  return res.json();
}

function NoOwnedGyms() {
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

function GymOption({ gym, onSelect, disabled, pending }: { gym: OwnedGym; onSelect: (gymId: string) => void; disabled: boolean; pending: boolean }) {
  return (
    <button
      onClick={() => onSelect(gym.gymId)}
      disabled={disabled}
      className="flex w-full items-center justify-between rounded-lg border border-border bg-background px-4 py-3 text-left transition hover:border-[#C41E3A] hover:bg-[#C41E3A]/5 disabled:opacity-50"
    >
      <span>
        <span className="block font-semibold">{gym.name}</span>
        <span className="block text-xs text-muted-foreground">{gym.schemaPrefix}</span>
      </span>
      {pending ? (
        <span className="text-xs">…</span>
      ) : (
        <span className="text-xs text-muted-foreground">Open →</span>
      )}
    </button>
  );
}

// Bounce unauthenticated users to /login and owners with an active gym to the dashboard.
function useChooserRedirects(status: string, activeGymId: string | undefined) {
  const router = useRouter();
  useEffect(() => {
    if (status === 'unauthenticated') router.replace('/login');
  }, [status, router]);

  // If a gym is already active, skip the chooser.
  useEffect(() => {
    if (activeGymId) router.replace('/');
  }, [activeGymId, router]);
}

function useGymSelection() {
  const router = useRouter();
  const { update } = useSession();
  const [submitting, setSubmitting] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function selectGym(gymId: string) {
    setSubmitting(gymId);
    setError(null);
    try {
      const { accessToken, gymId: newGymId, gymName, schemaPrefix } = await requestGymToken(gymId);
      await update({ accessToken, gymId: newGymId, gymName, schemaPrefix });
      router.replace('/');
    } catch (e: any) {
      setError(e.message ?? 'Selection failed');
      setSubmitting(null);
    }
  }

  return { submitting, error, selectGym };
}

export default function SelectGymPage() {
  const { data: session, status } = useSession();
  const activeGymId = session?.user?.gymId;
  const { submitting, error, selectGym } = useGymSelection();
  useChooserRedirects(status, activeGymId);

  const ownedGyms = session?.user?.ownedGyms ?? [];

  // Owner with exactly one gym shouldn't see the chooser — auto-select.
  useEffect(() => {
    if (shouldAutoSelect(status, activeGymId, ownedGyms.length, submitting)) {
      void selectGym(ownedGyms[0].gymId);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [status, activeGymId, ownedGyms.length]);

  if (status === 'loading') {
    return <div className="flex min-h-screen items-center justify-center">Loading…</div>;
  }

  if (ownedGyms.length === 0) return <NoOwnedGyms />;

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
              <GymOption
                gym={gym}
                onSelect={selectGym}
                disabled={submitting !== null}
                pending={submitting === gym.gymId}
              />
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
