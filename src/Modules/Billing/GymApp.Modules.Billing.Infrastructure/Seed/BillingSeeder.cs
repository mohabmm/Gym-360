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

        var seedPlans = BillingSeedData.GetSubscriptionPlans();
        var existingPlanIds = await ctx.SubscriptionPlans.Select(p => p.Id).ToListAsync(ct);
        var existingPlanIdSet = existingPlanIds.ToHashSet();
        var newPlans = seedPlans.Where(p => !existingPlanIdSet.Contains(p.Id)).ToList();
        if (newPlans.Count > 0)
        {
            await ctx.SubscriptionPlans.AddRangeAsync(newPlans, ct);
            logger.LogInformation("BillingSeeder: inserting {Count} new subscription plans", newPlans.Count);
        }

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

        // Save plans + features first so plan_features FKs resolve.
        if (newPlans.Count > 0 || newFeatures.Count > 0)
            await ctx.SaveChangesAsync(ct);

        // Rebuild the plan-feature matrix using the actual Feature ids in the DB
        // (not the just-generated in-memory ones, which were discarded if a
        // matching code already existed).
        var allFeatures = await ctx.Features.AsNoTracking().ToListAsync(ct);
        var seedPlanFeatures = BillingSeedData.GetPlanFeatures(allFeatures);
        var existingPlanFeatures = await ctx.PlanFeatures.ToListAsync(ct);
        var existingByKey = existingPlanFeatures
            .ToDictionary(pf => (pf.PlanId, pf.FeatureId));

        var addedPf = 0;
        var updatedPf = 0;
        foreach (var pf in seedPlanFeatures)
        {
            if (existingByKey.TryGetValue((pf.PlanId, pf.FeatureId), out var current))
            {
                // Sync IsEnabled if BillingSeedData was edited. LimitValue is
                // intentionally not synced here — it's per-plan-per-feature
                // override territory and admins may tune it via the UI.
                if (current.IsEnabled != pf.IsEnabled)
                {
                    current.Update(pf.IsEnabled, current.LimitValue);
                    updatedPf++;
                }
            }
            else
            {
                await ctx.PlanFeatures.AddAsync(pf, ct);
                addedPf++;
            }
        }
        if (addedPf > 0 || updatedPf > 0)
            logger.LogInformation("BillingSeeder: plan_features +{Added} / ~{Updated}", addedPf, updatedPf);

        // Storage limits keyed by (PlanId, ResourceType).
        var seedLimits = BillingSeedData.GetPlanStorageLimits();
        var existingLimits = await ctx.PlanStorageLimits.ToListAsync(ct);
        var limitsByKey = existingLimits
            .ToDictionary(l => (l.PlanId, l.ResourceType));

        var addedLim = 0;
        var updatedLim = 0;
        foreach (var lim in seedLimits)
        {
            if (limitsByKey.TryGetValue((lim.PlanId, lim.ResourceType), out var current))
            {
                if (current.LimitValue != lim.LimitValue)
                {
                    current.Update(lim.LimitValue);
                    updatedLim++;
                }
            }
            else
            {
                await ctx.PlanStorageLimits.AddAsync(lim, ct);
                addedLim++;
            }
        }
        if (addedLim > 0 || updatedLim > 0)
            logger.LogInformation("BillingSeeder: plan_storage_limits +{Added} / ~{Updated}", addedLim, updatedLim);

        await ctx.SaveChangesAsync(ct);
        logger.LogInformation("BillingSeeder: done");
    }
}
