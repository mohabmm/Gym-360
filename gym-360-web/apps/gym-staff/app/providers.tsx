'use client';

import { useState, useEffect, useRef, type MutableRefObject, type ReactNode } from 'react';
import { QueryClient, QueryClientProvider, useQueryClient } from '@tanstack/react-query';
import { SessionProvider, useSession, signOut } from 'next-auth/react';
import type { Session } from 'next-auth';
import { useSignalR, useInvalidateQueries, SuspensionOverlay, configureApiClient } from '@gym360/ui';
import { Toaster } from 'sonner';

type SessionStatus = ReturnType<typeof useSession>['status'];

function accessTokenOf(session: Session | null): string | null {
  return (session as unknown as { accessToken?: string } | null)?.accessToken ?? null;
}

function useConfigureApiClient(
  tokenRef: MutableRefObject<string | null>,
  gymIdRef: MutableRefObject<string | null>,
  sessionReadyRef: MutableRefObject<boolean>,
) {
  useEffect(() => {
    configureApiClient({
      getAccessToken: () => tokenRef.current,
      getActiveGymId: () => gymIdRef.current,
      onUnauthorized: () => {
        if (sessionReadyRef.current && tokenRef.current) {
          void signOut({ callbackUrl: '/login' });
        }
      },
    });
  }, [tokenRef, gymIdRef, sessionReadyRef]);
}

// Refetch any queries that fired during the hydration gap once the session is ready.
function useRefetchWhenAuthenticated(status: SessionStatus, tokenRef: MutableRefObject<string | null>) {
  const queryClient = useQueryClient();
  useEffect(() => {
    if (status === 'authenticated' && tokenRef.current) {
      void queryClient.invalidateQueries();
    }
  }, [status, queryClient, tokenRef]);
}

// A session that is "authenticated" but carries no access token means the
// refresh token was exhausted/revoked (lib/auth.ts blanks accessToken once it
// can no longer rotate). Without this guard the cookie keeps status as
// 'authenticated', so the page never bounces to /login and Owners get stuck on
// /select-gym with an "Unauthenticated" banner. Force a clean sign-out.
function useSignOutWhenTokenLost(
  status: SessionStatus,
  session: Session | null,
  tokenRef: MutableRefObject<string | null>,
) {
  useEffect(() => {
    if (status === 'authenticated' && !tokenRef.current) {
      void signOut({ callbackUrl: '/login' });
    }
  }, [status, session, tokenRef]);
}

/**
 * Wires the shared @gym360/ui axios client (used by usePlanFeatures / FeatureGate)
 * with the next-auth access token + active gymId. Without this the shared client
 * sent no Authorization/X-Gym-Id, so /billing/plan-features returned 401 and every
 * FeatureGate stayed stuck on the upgrade prompt. Token/gym are read through refs so
 * each request gets the latest value even before React re-renders.
 */
function ApiClientConfigurator({ children }: { children: ReactNode }) {
  const { data: session, status } = useSession();
  const tokenRef = useRef<string | null>(null);
  const gymIdRef = useRef<string | null>(null);
  const sessionReadyRef = useRef(false);

  tokenRef.current = accessTokenOf(session);
  gymIdRef.current = session?.user?.gymId ?? null;
  if (status !== 'loading') sessionReadyRef.current = true;

  useConfigureApiClient(tokenRef, gymIdRef, sessionReadyRef);
  useRefetchWhenAuthenticated(status, tokenRef);
  useSignOutWhenTokenLost(status, session, tokenRef);

  if (status === 'loading') {
    return (
      <div className="min-h-screen flex items-center justify-center text-sm text-muted-foreground">
        Loading…
      </div>
    );
  }

  return <>{children}</>;
}

function SignalRProvider({ children }: { children: ReactNode }) {
  const { invalidate } = useInvalidateQueries();
  const { data: session } = useSession();
  const [isSuspended, setIsSuspended] = useState(false);

  const { on, off } = useSignalR({
    hubUrl: `${(process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:8080/api/v1').replace(/\/api\/v1$/, '')}/hubs/config`,
    // Pass JWT so the hub's [Authorize] check succeeds. Without this,
    // SignalR negotiate returns 401 on every page load.
    accessToken: accessTokenOf(session) ?? undefined,
    autoReconnect: true,
  });

  useEffect(() => {
    const handleConfigUpdate = () => {
      invalidate(['plan-features']);
    };

    const handleGymSuspended = () => {
      setIsSuspended(true);
    };

    const handleGymReactivated = () => {
      setIsSuspended(false);
    };

    on('config_updated', handleConfigUpdate);
    on('gym_suspended', handleGymSuspended);
    on('gym_reactivated', handleGymReactivated);

    return () => {
      off('config_updated', handleConfigUpdate);
      off('gym_suspended', handleGymSuspended);
      off('gym_reactivated', handleGymReactivated);
    };
  }, [on, off, invalidate]);

  return (
    <>
      {isSuspended && (
        <SuspensionOverlay
          gymName="First Gym"
          onContactSupport={() => window.open('mailto:support@gym-360.com')}
        />
      )}
      {children}
    </>
  );
}

export function Providers({ children }: { children: ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 60 * 1000,
            retry: 1,
            refetchOnWindowFocus: false,
          },
        },
      })
  );

  return (
    <SessionProvider>
      <QueryClientProvider client={queryClient}>
        <ApiClientConfigurator>
          <SignalRProvider>{children}</SignalRProvider>
        </ApiClientConfigurator>
        <Toaster position="top-right" richColors />
      </QueryClientProvider>
    </SessionProvider>
  );
}
