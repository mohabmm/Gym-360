namespace GymApp.Modules.Billing.Application.DTOs;

// Shape mirrors the frontend PlatformAnalytics interface (apps/admin/lib/hooks/use-analytics.ts).
// All field names are camelCased by the API layer's JsonNamingPolicy.
public sealed record PlatformAnalyticsDto(
    PlatformKpisDto Kpis,
    List<TimeSeriesPointDto> RevenueTrend,
    List<TimeSeriesPointDto> GymGrowth,
    List<TimeSeriesPointDto> MemberGrowth,
    List<PlanDistributionDto> PlanDistribution,
    List<CityRevenueDto> CityRevenue,
    List<FeatureUsageDto> FeatureUsage,
    List<AtRiskGymDto> AtRiskGyms);

public sealed record PlatformKpisDto(
    int TotalGyms,
    int TotalMembers,
    int TotalTrainers,
    decimal Mrr,
    decimal MrrChange,
    decimal ActiveGymsChange,
    decimal MemberGrowthRate,
    decimal TrainerGrowthRate);

public sealed record TimeSeriesPointDto(string Date, decimal Value);

public sealed record PlanDistributionDto(string Plan, int Count, decimal Percentage, string Color);

public sealed record CityRevenueDto(string City, decimal Revenue, int GymCount);

public sealed record FeatureUsageDto(string FeatureCode, string FeatureName, int UsageCount, decimal AdoptionRate);

public sealed record AtRiskGymDto(
    Guid Id,
    string Name,
    string PlanName,
    decimal RiskScore,
    List<string> RiskFactors,
    string LastActivity,
    int MemberCount,
    decimal MonthlyRevenue);

public sealed record ChurnAnalyticsDto(
    ChurnMetricsDto Metrics,
    List<ChurnByPlanDto> ChurnByPlan,
    List<TimeSeriesPointDto> ChurnOverTime,
    List<AtRiskGymDto> AtRiskGyms,
    List<CohortRowDto> CohortData);

public sealed record ChurnMetricsDto(
    decimal ChurnRate,
    decimal ChurnRateChange,
    int AtRiskCount,
    int AtRiskChange,
    decimal AvgLifetimeValue,
    decimal LifetimeValueChange);

public sealed record ChurnByPlanDto(string Plan, decimal Rate);

public sealed record CohortRowDto(string Month, int Retained, int Churned);
