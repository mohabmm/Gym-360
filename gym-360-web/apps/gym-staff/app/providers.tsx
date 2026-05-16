'use client';

import { useState, useEffect, type ReactNode } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { SessionProvider, useSession } from 'next-auth/react';
import { useSignalR, useInvalidateQueries, SuspensionOverlay } from '@gym360/ui';
import { Toaster } from 'sonner';

function SignalRProvider({ children }: { children: ReactNode }) {
  const { invalidate } = useInvalidateQueries();
  const { data: session } = useSession();
  const [isSuspended, setIsSuspended] = useState(false);

  const { on, off } = useSignalR({
    hubUrl: `${(process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:8080/api/v1').replace(/\/api\/v1$/, '')}/hubs/config`,
    // Pass JWT so the hub's [Authorize] check succeeds. Without this,
    // SignalR negotiate returns 401 on every page load.
    accessToken: (session as unknown as { accessToken?: string } | null)?.accessToken,
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
        <SignalRProvider>{children}</SignalRProvider>
        <Toaster position="top-right" richColors />
      </QueryClientProvider>
    </SessionProvider>
  );
}
