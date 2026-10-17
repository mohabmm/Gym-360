using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Billing.Infrastructure.Seed;

public static class BillingSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var sp = services;
        var ctx = sp.GetRequiredService<BillingDbContext>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("BillingSeeder");

        // Per-row upsert by stable key. The previous all-or-nothing gate skipped
        // any catalog change made in BillingSeedData.cs (new plan, new feature,
        // toggled plan_feature, edited storage limit) once the tables had rows.

        var newPlanCount = await InsertNewPlansAsync(ctx, logger, ct);
        var newFeatureCount = await InsertNewFeaturesAsync(ctx, logger, ct);

        // Save plans + features first so plan_features FKs resolve.
        if (newPlanCount > 0 || newFeatureCount > 0)
            await ctx.SaveChangesAsync(ct);

        var planFeatures = await SyncPlanFeaturesAsync(ctx, ct);
        if (planFeatures.HasChanges)
            logger.LogInformation("BillingSeeder: plan_features +{Added} / ~{Updated}", planFeatures.Added, planFeatures.Updated);

        var storageLimits = await SyncStorageLimitsAsync(ctx, ct);
        if (storageLimits.HasChanges)
            logger.LogInformation("BillingSeeder: plan_storage_limits +{Added} / ~{Updated}", storageLimits.Added, storageLimits.Updated);

        await ctx.SaveChangesAsync(ct);
        logger.LogInformation("BillingSeeder: done");
    }

    private static async Task<int> InsertNewPlansAsync(BillingDbContext ctx, ILogger logger, CancellationToken ct)
    {
        var seedPlans = BillingSeedData.GetSubscriptionPlans();
        var existingPlanIds = await ctx.SubscriptionPlans.Select(p => p.Id).ToListAsync(ct);
        var existingPlanIdSet = existingPlanIds.ToHashSet();
        var newPlans = seedPlans.Where(p => !existingPlanIdSet.Contains(p.Id)).ToList();
        if (newPlans.Count > 0)
        {
            await ctx.SubscriptionPlans.AddRangeAsync(newPlans, ct);
            logger.LogInformation("BillingSeeder: inserting {Count} new subscription plans", newPlans.Count);
        }

        return newPlans.Count;
    }

    private static async Task<int> InsertNewFeaturesAsync(BillingDbContext ctx, ILogger logger, CancellationToken ct)
    {
        // Features identified by Code (stable, unique). Id is generated per
        // process so we can't compare on it for upsert without losing rows.
        var seedFeatures = BillingSeedData.GetFeatures();
        var existingFeatureCodes = (await ctx.Features.Select(f => f.Code).ToListAsync(ct)).ToHashSet();
        var newFeatures = seedFeatures.Where(f => !existingFeatureCodes.Contains(f.Code)).ToList();
        if (newFeatures.Count > 0)
        {
            await ctx.Features.AddRangeAsync(newFeatures, ct);
            logger.LogInformation("BillingSeeder: inserting {Count} new features", newFeatures.Count);
        }

        return newFeatures.Count;
    }

    private static async Task<SyncCounts> SyncPlanFeaturesAsync(BillingDbContext ctx, CancellationToken ct)
    {
        // Rebuild the plan-feature matrix using the actual Feature ids in the DB
        // (not the just-generated in-memory ones, which were discarded if a
        // matching code already existed).
        var allFeatures = await ctx.Features.AsNoTracking().ToListAsync(ct);
        var seedPlanFeatures = BillingSeedData.GetPlanFeatures(allFeatures);
        var existingPlanFeatures = await ctx.PlanFeatures.ToListAsync(ct);
        var existingByKey = existingPlanFeatures
            .ToDictionary(pf => (pf.PlanId, pf.FeatureId));

        var counts = new SyncCounts();
        foreach (var pf in seedPlanFeatures)
        {
            if (existingByKey.TryGetValue((pf.PlanId, pf.FeatureId), out var current))
            {
                counts.Updated += SyncIsEnabled(current, pf);
                continue;
            }

            await ctx.PlanFeatures.AddAsync(pf, ct);
            counts.Added++;
        }

        return counts;
    }

    // Sync IsEnabled if BillingSeedData was edited. LimitValue is
    // intentionally not synced here — it's per-plan-per-feature
    // override territory and admins may tune it via the UI.
    private static int SyncIsEnabled(PlanFeature current, PlanFeature seed)
    {
        if (current.IsEnabled == seed.IsEnabled)
            return 0;

        current.Update(seed.IsEnabled, current.LimitValue);
        return 1;
    }

    private static async Task<SyncCounts> SyncStorageLimitsAsync(BillingDbContext ctx, CancellationToken ct)
    {
        // Storage limits keyed by (PlanId, ResourceType).
        var seedLimits = BillingSeedData.GetPlanStorageLimits();
        var existingLimits = await ctx.PlanStorageLimits.ToListAsync(ct);
        var limitsByKey = existingLimits
            .ToDictionary(l => (l.PlanId, l.ResourceType));

        var counts = new SyncCounts();
        foreach (var lim in seedLimits)
        {
            if (limitsByKey.TryGetValue((lim.PlanId, lim.ResourceType), out var current))
            {
                counts.Updated += SyncLimitValue(current, lim);
                continue;
            }

            await ctx.PlanStorageLimits.AddAsync(lim, ct);
            counts.Added++;
        }

        return counts;
    }

    private static int SyncLimitValue(PlanStorageLimit current, PlanStorageLimit seed)
    {
        if (current.LimitValue == seed.LimitValue)
            return 0;

        current.Update(seed.LimitValue);
        return 1;
    }

    private sealed class SyncCounts
    {
        public int Added { get; set; }
        public int Updated { get; set; }
        public bool HasChanges => Added > 0 || Updated > 0;
    }
}
