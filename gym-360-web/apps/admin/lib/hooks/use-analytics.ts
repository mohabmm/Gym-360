import { useApiQuery, ADMIN, GYMS } from '@gym360/ui';
import type {
  PlatformKpis,
  TimeSeriesPoint,
  CityRevenue,
  PlanDistribution,
  AtRiskGym,
  ChurnMetrics,
  FeatureUsage,
} from '@gym360/ui';

export interface PlatformAnalytics {
  kpis: PlatformKpis;
  revenueTrend: TimeSeriesPoint[];
  gymGrowth: TimeSeriesPoint[];
  memberGrowth: TimeSeriesPoint[];
  planDistribution: PlanDistribution[];
  cityRevenue: CityRevenue[];
  featureUsage: FeatureUsage[];
  atRiskGyms: AtRiskGym[];
}

export interface ChurnAnalytics {
  metrics: ChurnMetrics;
  churnByPlan: { plan: string; rate: number }[];
  churnOverTime: TimeSeriesPoint[];
  atRiskGyms: AtRiskGym[];
  cohortData: { month: string; retained: number; churned: number }[];
}

export function usePlatformAnalytics() {
  return useApiQuery<PlatformAnalytics>(
    ['analytics', 'platform'],
    ADMIN.PLATFORM_ANALYTICS,
  );
}

export function useChurnAnalytics() {
  return useApiQuery<ChurnAnalytics>(
    ['analytics', 'churn'],
    ADMIN.CHURN_ANALYTICS,
  );
}

export function useGymAnalytics(gymId: string) {
  return useApiQuery<PlatformAnalytics>(
    ['analytics', 'gym', gymId],
    GYMS.ANALYTICS(gymId),
    undefined,
    { enabled: !!gymId },
  );
}
