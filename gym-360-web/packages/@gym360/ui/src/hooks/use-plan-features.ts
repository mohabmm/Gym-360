'use client';

import { useQuery, useQueryClient } from '@tanstack/react-query';
import { apiGet } from '../lib/api-client';
import { BILLING } from '../lib/api-endpoints';
import type { PlanFeature } from '../types/subscription';

interface PlanFeaturesResult {
  features: PlanFeature[];
  isFeatureEnabled: (featureCode: string) => boolean;
  isLoading: boolean;
  error: Error | null;
  invalidate: () => void;
}

export function usePlanFeatures(): PlanFeaturesResult {
  const queryClient = useQueryClient();

  const { data, isLoading, error } = useQuery<PlanFeature[]>({
    queryKey: ['plan-features'],
    queryFn: async () => {
      // BE returns { gymId, planCode, planName, ...limits, features: PlanFeature[] };
      // the effective feature list is nested under `features`.
      const response = await apiGet<{ features?: PlanFeature[] }>(BILLING.MY_FEATURES);
      return response.data?.features ?? [];
    },
    staleTime: Infinity, // Only refetch on SignalR signal
    gcTime: 1000 * 60 * 60, // Keep in cache for 1 hour
  });

  const features = data ?? [];

  const isFeatureEnabled = (featureCode: string): boolean => {
    const feature = features.find((f) => f.featureCode === featureCode);
    return feature?.isEnabled ?? false;
  };

  const invalidate = (): void => {
    void queryClient.invalidateQueries({ queryKey: ['plan-features'] });
  };

  return {
    features,
    isFeatureEnabled,
    isLoading,
    error: error as Error | null,
    invalidate,
  };
}

// Feature codes matching backend
export const FEATURE_CODES = {
  WORKOUT_BUILDER: 'workout_builder',
  NUTRITION_TRACKING: 'nutrition_tracking',
  PROGRESS_PHOTOS: 'progress_photos',
  REMOTE_TRAINING: 'remote_training',
  CUSTOM_EXERCISE_LIBRARY: 'custom_exercise_library',
  SESSION_PACKAGES: 'session_packages',
  GYM_BRANDING: 'gym_branding',
  ADVANCED_ANALYTICS: 'advanced_analytics',
  MEMBER_LEADERBOARD: 'member_leaderboard',
  TRAINER_RATINGS: 'trainer_ratings',
  ANNOUNCEMENTS: 'announcements',
  MULTI_STAFF: 'multi_staff',
  CLASS_MANAGEMENT: 'class_management',
  QR_CHECK_IN: 'qr_check_in',
  MEAL_PLANS: 'meal_plans',
  BODY_METRICS: 'body_metrics',
  CHAT: 'chat',
  GAMIFICATION: 'gamification',
  TESTIMONIALS: 'testimonials',
  SEASONAL_HOURS: 'seasonal_hours',
  GYM_SECTIONS: 'gym_sections',
  EXPORT_DATA: 'export_data',
  BULK_ACTIONS: 'bulk_actions',
  NOTIFICATION_PREFERENCES: 'notification_preferences',
  PAYMENT_TRACKING: 'payment_tracking',
  STORAGE_UPGRADE: 'storage_upgrade',
  TRAINER_MULTI_GYM: 'trainer_multi_gym',
  LIVE_SESSION_MODE: 'live_session_mode',
  WORKOUT_ASSIGNMENT: 'workout_assignment',
  STREAKS: 'streaks',
  WATER_TRACKING: 'water_tracking',
  DATA_IMPORT: 'data_import',
} as const;
