export interface PlatformKpis {
  totalGyms: number;
  totalMembers: number;
  totalTrainers: number;
  mrr: number;
  mrrChange: number;
  activeGymsChange: number;
  memberGrowthRate: number;
  trainerGrowthRate: number;
}

export interface GymKpis {
  activeMembers: number;
  activeMembersChange: number;
  monthlyRevenue: number;
  revenueChange: number;
  todayCheckIns: number;
  checkInsChange: number;
  activeTrainers: number;
  trainersChange: number;
}

export interface ChurnMetrics {
  churnRate: number;
  churnRateChange: number;
  atRiskCount: number;
  atRiskChange: number;
  avgLifetimeValue: number;
  lifetimeValueChange: number;
}

export interface AtRiskGym {
  id: string;
  name: string;
  planName: string;
  riskScore: number;
  riskFactors: string[];
  lastActivity: string;
  memberCount: number;
  monthlyRevenue: number;
}

export interface ChurnFunnelStep {
  stage: string;
  count: number;
  percentage: number;
}

export interface TimeSeriesPoint {
  date: string;
  value: number;
}

export interface CityRevenue {
  city: string;
  revenue: number;
  gymCount: number;
}

export interface PlanDistribution {
  plan: string;
  count: number;
  percentage: number;
  color: string;
}

export interface PeakHoursData {
  hour: number;
  monday: number;
  tuesday: number;
  wednesday: number;
  thursday: number;
  friday: number;
  saturday: number;
  sunday: number;
}

export interface FeatureUsage {
  featureCode: string;
  featureName: string;
  usageCount: number;
  adoptionRate: number;
}

export interface CheckInTrend {
  date: string;
  total: number;
  qr: number;
  manual: number;
  numpad: number;
}
