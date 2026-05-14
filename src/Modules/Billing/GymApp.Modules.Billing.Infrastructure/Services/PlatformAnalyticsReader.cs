using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Billing.Infrastructure.Services;

// Builds admin-dashboard analytics from the Billing DbContext plus the lightweight gym summary
// read model. Deliberately conservative: we compute what we have authoritative data for
// (gym counts by status/city, plan distribution, MRR from active subscriptions) and return
// empty arrays for series we cannot derive yet (per-month member growth, feature usage,
// cohort retention). The frontend handles empty arrays gracefully.
internal sealed class PlatformAnalyticsReader : IPlatformAnalyticsReader
{
    private readonly BillingDbContext _billing;

    public PlatformAnalyticsReader(BillingDbContext billing)
    {
        _billing = billing;
    }

    public async Task<PlatformAnalyticsDto> GetPlatformAnalyticsAsync(CancellationToken cancellationToken)
    {
        var gyms = await _billing.GymSummaries.AsNoTracking().ToListAsync(cancellationToken);
        var activeGyms = gyms.Where(g => string.Equals(g.StatusText, "Active", StringComparison.OrdinalIgnoreCase)).ToList();

        var activeSubs = await _billing.GymSubscriptions
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => new { s.GymId, s.PlanId })
            .ToListAsync(cancellationToken);

        var plans = await _billing.SubscriptionPlans
            .AsNoTracking()
            .Select(p => new { p.Id, p.Code, p.Name, p.MonthlyPriceEgp })
            .ToListAsync(cancellationToken);

        var priceByPlan = plans.ToDictionary(p => p.Id, p => p.MonthlyPriceEgp);
        var mrr = activeSubs.Sum(s => priceByPlan.TryGetValue(s.PlanId, out var price) ? price : 0m);

        var subsByPlan = activeSubs
            .GroupBy(s => s.PlanId)
            .ToDictionary(g => g.Key, g => g.Count());

        int totalSubs = Math.Max(1, activeSubs.Count);

        var planDistribution = plans
            .Select(p => new PlanDistributionDto(
                Plan: p.Name,
                Count: subsByPlan.TryGetValue(p.Id, out var c) ? c : 0,
                Percentage: subsByPlan.TryGetValue(p.Id, out var c2) ? Math.Round(100m * c2 / totalSubs, 2) : 0m,
                Color: ColorFor(p.Code)))
            .ToList();

        var cityRevenue = activeGyms
            .Where(g => !string.IsNullOrWhiteSpace(g.City))
            .GroupBy(g => g.City!)
            .Select(g => new CityRevenueDto(
                City: g.Key,
                Revenue: 0m,
                GymCount: g.Count()))
            .OrderByDescending(c => c.GymCount)
            .Take(10)
            .ToList();

        var kpis = new PlatformKpisDto(
            TotalGyms: gyms.Count,
            TotalMembers: 0,
            TotalTrainers: 0,
            Mrr: mrr,
            MrrChange: 0m,
            ActiveGymsChange: 0m,
            MemberGrowthRate: 0m,
            TrainerGrowthRate: 0m);

        // Build a rough 6-month gym growth series from creation timestamps so the chart has
        // something meaningful. Revenue/member series remain empty until Payments history
        // aggregation lands.
        var now = DateTime.UtcNow;
        var gymGrowth = Enumerable.Range(0, 6)
            .Select(offset =>
            {
                var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5 + offset);
                var monthEnd = monthStart.AddMonths(1);
                var count = gyms.Count(g => g.CreatedAt >= monthStart && g.CreatedAt < monthEnd);
                return new TimeSeriesPointDto(monthStart.ToString("yyyy-MM-dd"), count);
            })
            .ToList();

        return new PlatformAnalyticsDto(
            Kpis: kpis,
            RevenueTrend: new List<TimeSeriesPointDto>(),
            GymGrowth: gymGrowth,
            MemberGrowth: new List<TimeSeriesPointDto>(),
            PlanDistribution: planDistribution,
            CityRevenue: cityRevenue,
            FeatureUsage: new List<FeatureUsageDto>(),
            AtRiskGyms: new List<AtRiskGymDto>());
    }

    public async Task<ChurnAnalyticsDto> GetChurnAnalyticsAsync(CancellationToken cancellationToken)
    {
        var totalSubs = await _billing.GymSubscriptions.AsNoTracking().CountAsync(cancellationToken);
        var inactiveSubs = await _billing.GymSubscriptions.AsNoTracking().CountAsync(s => !s.IsActive, cancellationToken);

        var churnRate = totalSubs == 0 ? 0m : Math.Round(100m * inactiveSubs / totalSubs, 2);

        var metrics = new ChurnMetricsDto(
            ChurnRate: churnRate,
            ChurnRateChange: 0m,
            AtRiskCount: 0,
            AtRiskChange: 0,
            AvgLifetimeValue: 0m,
            LifetimeValueChange: 0m);

        return new ChurnAnalyticsDto(
            Metrics: metrics,
            ChurnByPlan: new List<ChurnByPlanDto>(),
            ChurnOverTime: new List<TimeSeriesPointDto>(),
            AtRiskGyms: new List<AtRiskGymDto>(),
            CohortData: new List<CohortRowDto>());
    }

    private static string ColorFor(string code) => code?.ToLowerInvariant() switch
    {
        var c when c != null && c.Contains("free") => "#6b7280",
        var c when c != null && c.Contains("starter") => "#10b981",
        var c when c != null && c.Contains("pro") => "#3b82f6",
        var c when c != null && (c.Contains("business") || c.Contains("enterprise")) => "#8b5cf6",
        _ => "#94a3b8",
    };
}
