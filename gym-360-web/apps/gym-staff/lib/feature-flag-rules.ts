import { FEATURE_FLAG_KEYS as K } from '@gym360/ui/lib/feature-flags/keys';
import type { RouteFlagRule } from '@gym360/ui/lib/feature-flags/route-guard';

/**
 * Which flag owns each staff-web route. Paths not listed are always on (dashboard, members,
 * trainers, staff, settings, subscription). The API enforces the same flags (403 FEATURE_DISABLED).
 */
export const STAFF_ROUTE_RULES: readonly RouteFlagRule[] = [
  { prefix: '/classes', flags: [K.CLASSES] },
  { prefix: '/exercises', flags: [K.TRAINING_WORKOUTS] },
  { prefix: '/messages', flags: [K.COMMUNICATION_CHAT] },
  { prefix: '/billing', flags: [K.GYM_FINANCE] },
  { prefix: '/gamification', flags: [K.GAMIFICATION_MEMBER_LEADERBOARD, K.COACH_LEADERBOARD, K.COACH_TESTIMONIALS] },
  { prefix: '/announcements', flags: [K.COMMUNICATION_ANNOUNCEMENTS] },
  { prefix: '/analytics', flags: [K.ANALYTICS] },
  { prefix: '/check-ins', flags: [K.GYM_ATTENDANCE] },
  { prefix: '/kiosk-pin', flags: [K.GYM_ATTENDANCE] },
  { prefix: '/data-import', flags: [K.GYM_DATA_IMPORT] },
  { prefix: '/pt/trials', flags: [K.PT_TRIAL_END_ALERTS] },
  { prefix: '/pt/review', flags: [K.PT_DUAL_SESSION_LOGGING] },
  { prefix: '/pt/dashboard', flags: [K.PT_OWNER_DASHBOARD] },
];
