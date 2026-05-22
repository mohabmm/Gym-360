'use client';

import { useState, useEffect, useRef, type ReactNode } from 'react';
import { QueryClient, QueryClientProvider, useQueryClient } from '@tanstack/react-query';
import { SessionProvider, useSession, signOut } from 'next-auth/react';
import { useSignalR, useInvalidateQueries, configureApiClient } from '@gym360/ui';

/**
 * Registers the access token with the shared axios client. The token is read through
 * a ref so every request gets the latest value even if React hasn't re-rendered, which
 * avoids a race where queries fire before next-auth has hydrated the session and get
 * a spurious 401 → forced sign-out.
 */
function ApiClientConfigurator({ children }: { children: ReactNode }) {
  const { data: session, status } = useSession();
  const tokenRef = useRef<string | null>(null);
  const sessionReadyRef = useRef(false);
  const queryClient = useQueryClient();

  tokenRef.current = session?.accessToken ?? null;
  if (status !== 'loading') sessionReadyRef.current = true;

  useEffect(() => {
    configureApiClient({
      getAccessToken: () => tokenRef.current,
      onUnauthorized: () => {
        // Only bounce to /login if we actually had a session to begin with. If the
        // session is still loading (or never existed), the request races ahead of
        // hydration; swallow the 401 so next-auth middleware handles auth instead.
        if (sessionReadyRef.current && tokenRef.current) {
          void signOut({ callbackUrl: '/login' });
        }
      },
    });
  }, []);

  // Once the session finishes loading with a valid token, refetch any queries that
  // fired during the hydration gap so the UI doesn't stay stuck on an empty state.
  useEffect(() => {
    if (status === 'authenticated' && session?.accessToken) {
      void queryClient.invalidateQueries();
    }
  }, [status, session?.accessToken, queryClient]);

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
    hubUrl: `${(process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:8080/api/v1').replace(/\/api\/v\d+$/, '')}/hubs/config`,
    accessToken: session?.accessToken,
    autoReconnect: true,
  });

  useEffect(() => {
    const handleConfigUpdate = () => {
      invalidate(['plan-features']);
      invalidate(['plans']);
      invalidate(['features']);
      invalidate(['gyms']);
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

  return <>{children}</>;
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
      </QueryClientProvider>
    </SessionProvider>
  );
}
