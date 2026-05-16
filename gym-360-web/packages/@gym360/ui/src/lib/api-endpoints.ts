// Identity Module
export const IDENTITY = {
  LOGIN: '/identity/login',
  REGISTER: '/identity/register',
  REFRESH: '/identity/refresh',
  PROFILE: '/identity/profile',
  MY_DATA: '/identity/my-data',
  DELETE_ACCOUNT: '/identity/account',
  CONSENT: '/identity/consent',
  USERS: '/identity/users',
  USER: (id: string) => `/identity/users/${id}`,
  ADMIN_USERS: '/admin/users',
  ADMIN_USER: (id: string) => `/admin/users/${id}`,
  ADMIN_OWNERS: '/admin/owners',
} as const;

// Gyms Module
export const GYMS = {
  LIST: '/gyms',
  DETAIL: (id: string) => `/gyms/${id}`,
  CREATE: '/gyms',
  UPDATE: (id: string) => `/gyms/${id}`,
  // Admin-only lifecycle endpoints are under /admin/gyms/:id
  DELETE: (id: string) => `/admin/gyms/${id}`,
  SUSPEND: (id: string) => `/admin/gyms/${id}/suspend`,
  REACTIVATE: (id: string) => `/admin/gyms/${id}/reactivate`,
  TERMINATE: (id: string) => `/admin/gyms/${id}/terminate`,
  APPROVE: (id: string) => `/admin/gyms/${id}/approve`,
  REJECT: (id: string) => `/admin/gyms/${id}/reject`,
  PENDING: '/gyms/pending',
  MEMBERS: (id: string) => `/gyms/${id}/members`,
  TRAINERS: (id: string) => `/gyms/${id}/trainers`,
  CLASSES: (id: string) => `/gyms/${id}/classes`,
  STAFF: (id: string) => `/gyms/${id}/staff`,
  ANNOUNCEMENTS: (id: string) => `/gyms/${id}/announcements`,
  SETTINGS: (id: string) => `/gyms/${id}/settings`,
  OPERATING_HOURS: (id: string) => `/gyms/${id}/operating-hours`,
  SEASONAL_HOURS: (id: string) => `/gyms/${id}/seasonal-hours`,
  SESSION_POLICY: (id: string) => `/gyms/${id}/session-policy`,
  SECTIONS: (id: string) => `/gyms/${id}/sections`,
  BRANDING: (id: string) => `/gyms/${id}/branding`,
  CHECK_INS: (id: string) => `/gyms/${id}/check-ins`,
  ANALYTICS: (id: string) => `/gyms/${id}/analytics`,
  ADMIN_CREATE_STAFF: (id: string) => `/admin/gyms/${id}/staff/create`,
} as const;

// Training Module
export const TRAINING = {
  EXERCISES: '/training/exercises',
  EXERCISE: (id: string) => `/training/exercises/${id}`,
  WORKOUTS: '/training/workouts',
  WORKOUT: (id: string) => `/training/workouts/${id}`,
  PROGRAMS: '/training/programs',
  PROGRAM: (id: string) => `/training/programs/${id}`,
  WORKOUT_LOGS: '/training/workout-logs',
  ASSIGNMENTS: '/training/assignments',
} as const;

// Scheduling Module
export const SCHEDULING = {
  SESSIONS: '/scheduling/sessions',
  SESSION: (id: string) => `/scheduling/sessions/${id}`,
  CANCEL: (id: string) => `/scheduling/sessions/${id}/cancel`,
  COMPLETE: (id: string) => `/scheduling/sessions/${id}/complete`,
  AVAILABILITY: '/scheduling/availability',
  CLASSES: '/scheduling/classes',
  CLASS: (id: string) => `/scheduling/classes/${id}`,
  ENROLLMENTS: (id: string) => `/scheduling/classes/${id}/enrollments`,
} as const;

// Billing Module
// Admin-only routes (plans, features, matrix, overrides, change log) live under /admin/*.
// Tenant-scoped routes (payments, storage, my-features) stay under /billing/*.
export const BILLING = {
  PAYMENTS: '/billing/payments',
  PAYMENT: (id: string) => `/billing/payments/${id}`,
  VOID: (id: string) => `/billing/payments/${id}/void`,
  PLANS: '/admin/plans',
  PLAN: (id: string) => `/admin/plans/${id}`,
  FEATURES: '/admin/features',
  FEATURE: (id: string) => `/admin/features/${id}`,
  PLAN_FEATURES: (planId: string, featureId: string) =>
    `/admin/plans/${planId}/features/${featureId}`,
  PLAN_FEATURE_MATRIX: '/admin/plans/feature-matrix',
  GYM_OVERRIDES: (gymId: string) => `/admin/gyms/${gymId}/feature-overrides`,
  GYM_OVERRIDE: (gymId: string, featureId: string) =>
    `/admin/gyms/${gymId}/feature-overrides/${featureId}`,
  GYM_SUBSCRIPTION: (gymId: string) => `/admin/gyms/${gymId}/subscription`,
  CHANGE_PLAN: (gymId: string) => `/admin/gyms/${gymId}/subscription/plan`,
  STORAGE_USAGE: (gymId: string) => `/billing/gyms/${gymId}/storage`,
  STORAGE_LIMITS: '/billing/storage-limits',
  SESSION_PACKAGES: '/billing/session-packages',
  SESSION_PACKAGE: (id: string) => `/billing/session-packages/${id}`,
  SESSION_PACKAGE_TEMPLATES: '/billing/session-package-templates',
  PLAN_CHANGE_LOG: '/admin/plan-change-log',
  MY_FEATURES: '/billing/my-features',
} as const;

// Communication Module
export const COMMUNICATION = {
  NOTIFICATIONS: '/communication/notifications',
  NOTIFICATION: (id: string) => `/communication/notifications/${id}`,
  MARK_READ: (id: string) => `/communication/notifications/${id}/read`,
  PREFERENCES: '/communication/preferences',
  ANNOUNCEMENTS: '/communication/announcements',
  ANNOUNCEMENT: (id: string) => `/communication/announcements/${id}`,
} as const;

// Gamification Module
export const GAMIFICATION = {
  LEADERBOARD: '/gamification/leaderboard',
  TRAINER_RANKINGS: '/gamification/trainer-rankings',
  TESTIMONIALS: '/gamification/testimonials',
  TESTIMONIAL: (id: string) => `/gamification/testimonials/${id}`,
  MODERATE: (id: string) => `/gamification/testimonials/${id}/moderate`,
  OPT_OUT: '/gamification/opt-out',
} as const;

// Progress Module
export const PROGRESS = {
  METRICS: '/progress/metrics',
  PHOTOS: '/progress/photos',
  STREAKS: '/progress/streaks',
} as const;

// Nutrition Module
export const NUTRITION = {
  FOOD_LOG: '/nutrition/food-log',
  WATER_LOG: '/nutrition/water-log',
  MEAL_PLANS: '/nutrition/meal-plans',
  FOODS: '/nutrition/foods',
} as const;

// Admin Module
export const ADMIN = {
  DEPLOY: (scope: string, id?: string) =>
    id ? `/admin/deploy/${scope}/${id}` : `/admin/deploy/${scope}`,
  AUDIT_LOG: '/admin/audit-log',
  PLATFORM_ANALYTICS: '/admin/analytics',
  CHURN_ANALYTICS: '/admin/analytics/churn',
  GYM_ANALYTICS: (gymId: string) => `/admin/analytics/gyms/${gymId}`,
  PLATFORM_CONFIG: '/admin/platform-config',
  SECURITY_SETTINGS: '/admin/security-settings',
} as const;
