'use client';

import { useSession } from 'next-auth/react';
import { useFeatureFlags } from '@gym360/ui';
import { STAFF_ROUTE_RULES } from './feature-flag-rules';

export const STAFF_ROUTE_FLAGS = STAFF_ROUTE_RULES;

/** Flags of the signed-in staff member's gym (shared FeatureFlagService, cached per gym). */
export function useStaffFlags() {
  const { data: session } = useSession();
  return useFeatureFlags(session?.user?.gymId);
}
