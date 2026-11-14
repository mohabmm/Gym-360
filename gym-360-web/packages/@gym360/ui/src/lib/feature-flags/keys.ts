/** Feature-flag keys shared with the backend (`FeatureKeys` in .NET). Never rename a key. */
export const FEATURE_FLAG_KEYS = {
  // PT module — ON in the pt_module preset.
  PT_TRIAL_SESSIONS: 'pt.trial_sessions',
  PT_PROGRESS_MEASUREMENTS: 'pt.progress_measurements',
  PT_TRIAL_END_ALERTS: 'pt.trial_end_alerts',
  PT_PACKAGES: 'pt.packages',
  PT_DUAL_SESSION_LOGGING: 'pt.dual_session_logging',
  PT_RENEWAL_REMINDERS: 'pt.renewal_reminders',
  PT_OWNER_DASHBOARD: 'pt.owner_dashboard',
  PT_TRIAL_RECAP: 'pt.trial_recap',
  PT_PACKAGE_INTEREST: 'pt.package_interest',
  PT_TRIAL_FOLLOWUPS: 'pt.trial_followups',
  // PT later phase — OFF in pt_module.
  PT_MONTHLY_REPORT: 'pt.monthly_report',
  PT_COACH_RATINGS: 'pt.coach_ratings',
  PT_COACH_PERFORMANCE: 'pt.coach_performance',
  // Setting.
  PT_AUTO_EXPIRY: 'pt.auto_expiry',
  // Every other module — OFF in pt_module.
  GYM_MEMBERSHIPS: 'gym.memberships',
  GYM_ATTENDANCE: 'gym.attendance',
  GYM_FINANCE: 'gym.finance',
  PAYMENTS_IN_APP: 'payments.in_app',
  CLASSES: 'classes',
  SCHEDULING_BOOKING: 'scheduling.booking',
  TRAINING_WORKOUTS: 'training.workouts',
  TRAINING_PROGRAMS: 'training.programs',
  TRAINING_REMOTE_WORKOUT: 'training.remote_workout',
  NUTRITION: 'nutrition',
  PROGRESS_PHOTOS: 'progress.photos',
  PROGRESS_INBODY: 'progress.inbody',
  GAMIFICATION_STREAKS: 'gamification.streaks',
  COMMUNICATION_CHAT: 'communication.chat',
  COMMUNICATION_ANNOUNCEMENTS: 'communication.announcements',
  GAMIFICATION_MEMBER_LEADERBOARD: 'gamification.member_leaderboard',
  COACH_LEADERBOARD: 'coach.leaderboard',
  COACH_TESTIMONIALS: 'coach.testimonials',
  ANALYTICS: 'analytics',
  GYM_MEMBER_RECORDS: 'gym.member_records',
  GYM_BRANDING: 'gym.branding',
  GYM_DATA_IMPORT: 'gym.data_import',
  STAFF_CUSTOM_PERMISSIONS: 'staff.custom_permissions',
  INTEGRATIONS_API_ACCESS: 'integrations.api_access',
} as const;

export type FeatureFlagKey = (typeof FEATURE_FLAG_KEYS)[keyof typeof FEATURE_FLAG_KEYS];

const K = FEATURE_FLAG_KEYS;

/**
 * What the app assumes when it has never fetched flags and cannot reach the server:
 * the pt_module preset. Never "all on".
 */
export const PT_MODULE_DEFAULTS: readonly string[] = [
  K.PT_TRIAL_SESSIONS,
  K.PT_PROGRESS_MEASUREMENTS,
  K.PT_TRIAL_END_ALERTS,
  K.PT_PACKAGES,
  K.PT_DUAL_SESSION_LOGGING,
  K.PT_RENEWAL_REMINDERS,
  K.PT_OWNER_DASHBOARD,
  K.PT_TRIAL_RECAP,
  K.PT_PACKAGE_INTEREST,
  K.PT_TRIAL_FOLLOWUPS,
];
