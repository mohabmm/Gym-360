'use client';

import { useEffect, type ReactNode } from 'react';
import { usePathname, useRouter } from 'next/navigation';
import { isPathEnabled } from '@gym360/ui';
import { STAFF_ROUTE_FLAGS, useStaffFlags } from '@/lib/feature-flags';

/**
 * Sends a disabled route (typed URL, bookmark, old link) back to the dashboard. Waits for
 * the cached or fetched flags so a business gym's deep link is not bounced by the defaults.
 */
export function FeatureRouteGuard({ children }: { children: ReactNode }) {
  const pathname = usePathname() ?? '/';
  const router = useRouter();
  const { isEnabled, isSettled } = useStaffFlags();
  const allowed = isPathEnabled(pathname, STAFF_ROUTE_FLAGS, isEnabled);

  useEffect(() => {
    if (isSettled && !allowed) router.replace('/');
  }, [isSettled, allowed, router]);

  if (!allowed) return null;
  return <>{children}</>;
}
