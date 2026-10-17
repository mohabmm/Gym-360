using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Shared;
using GymApp.Modules.Billing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Billing.Infrastructure.Services;

/// <summary>
/// The CRITICAL service that ALL modules depend on for feature gating.
/// Resolution order: Override > Plan Default > OFF (fail-closed).
/// Results are cached in Redis with key "v2:plan-features:{gymId}", TTL 5 min.
/// The "v1:" prefix allows cache invalidation across all gyms by bumping the version prefix.
/// </summary>
internal sealed class GymPlanService : IGymPlanService
{
    private readonly BillingDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GymPlanService> _logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public GymPlanService(
        BillingDbContext dbContext,
        ICacheService cacheService,
        ILogger<GymPlanService> logger)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<bool> IsFeatureEnabledAsync(
        Guid gymId,
        string featureCode,
        CancellationToken cancellationToken = default)
    {
        var featureSet = await GetOrBuildFeatureSetAsync(gymId, cancellationToken);
        return featureSet?.EnabledFeatures.Contains(featureCode) ?? false;
    }

    public async Task<List<string>> GetEnabledFeaturesAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        var featureSet = await GetOrBuildFeatureSetAsync(gymId, cancellationToken);
        return featureSet?.EnabledFeatures ?? new List<string>();
    }

    public async Task<int> GetMemberLimitAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        var featureSet = await GetOrBuildFeatureSetAsync(gymId, cancellationToken);
        return featureSet?.MemberLimit ?? 25;
    }

    public async Task<int> GetTrainerLimitAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        var featureSet = await GetOrBuildFeatureSetAsync(gymId, cancellationToken);
        return featureSet?.TrainerLimit ?? 1;
    }

    public async Task<int> GetStaffLimitAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        var featureSet = await GetOrBuildFeatureSetAsync(gymId, cancellationToken);
        return featureSet?.StaffLimit ?? 1;
    }

    public async Task<string> GetPlanCodeAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        var featureSet = await GetOrBuildFeatureSetAsync(gymId, cancellationToken);
        return featureSet?.PlanCode ?? "free";
    }

    public async Task<long> GetStorageLimitBytesAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        var featureSet = await GetOrBuildFeatureSetAsync(gymId, cancellationToken);
        return featureSet?.StorageLimitBytes ?? StorageLimitLookup.DefaultStorageLimitBytes;
    }

    public async Task<long> GetCurrentStorageUsedBytesAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        var usage = await _dbContext.GymStorageUsages
            .AsNoTracking()
            .Where(u => u.GymId == gymId && u.ResourceType == "total_storage")
            .FirstOrDefaultAsync(cancellationToken);

        return usage?.CurrentUsage ?? 0;
    }

    public async Task<int> GetPhotoLimitPerMemberAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        var featureSet = await GetOrBuildFeatureSetAsync(gymId, cancellationToken);
        return featureSet?.PhotoLimitPerMember ?? 5;
    }

    public async Task<int> GetCheckInRetentionDaysAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        var featureSet = await GetOrBuildFeatureSetAsync(gymId, cancellationToken);
        return featureSet?.CheckInRetentionDays ?? 30;
    }

    private async Task<GymFeatureSet?> GetOrBuildFeatureSetAsync(
        Guid gymId,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"v2:plan-features:{gymId}";

        var cached = await _cacheService.GetAsync<GymFeatureSet>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        // 1. Get the gym's subscription
        var subscription = await _dbContext.GymSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.GymId == gymId && s.IsActive, cancellationToken);

        if (subscription is null)
        {
            _logger.LogWarning("No active subscription found for gym {GymId}", gymId);
            return null;
        }

        var featureSet = await BuildFeatureSetAsync(subscription, cancellationToken);
        if (featureSet is not null)
        {
            await _cacheService.SetAsync(cacheKey, featureSet, CacheTtl, cancellationToken);
        }

        return featureSet;
    }

    private async Task<GymFeatureSet?> BuildFeatureSetAsync(
        GymSubscription subscription,
        CancellationToken cancellationToken)
    {
        // 2. Get the plan details
        var plan = await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == subscription.PlanId, cancellationToken);

        if (plan is null)
        {
            _logger.LogWarning("Plan {PlanId} not found for gym {GymId}", subscription.PlanId, subscription.GymId);
            return null;
        }

        var enabledFeatures = await ResolveEnabledFeaturesAsync(subscription, cancellationToken);
        var storageLimits = await LoadStorageLimitsAsync(subscription, cancellationToken);

        return CreateFeatureSet(subscription, plan, enabledFeatures, storageLimits);
    }

    private async Task<List<string>> ResolveEnabledFeaturesAsync(
        GymSubscription subscription,
        CancellationToken cancellationToken)
    {
        var allFeatures = await LoadActiveFeaturesAsync(cancellationToken);
        var planFeatures = await LoadPlanFeaturesAsync(subscription, cancellationToken);
        var overrides = await LoadOverridesAsync(subscription, cancellationToken);

        // 7. Build the resolved feature set: Override > Plan Default > OFF
        return allFeatures
            .Where(feature => IsFeatureOn(feature, planFeatures, overrides))
            .Select(feature => feature.Code)
            .ToList();
    }

    // 3. Get all active features
    private Task<List<Feature>> LoadActiveFeaturesAsync(CancellationToken cancellationToken) =>
        _dbContext.Features
            .AsNoTracking()
            .Where(f => f.IsActive)
            .ToListAsync(cancellationToken);

    // 4. Get plan-feature mappings
    private Task<List<PlanFeature>> LoadPlanFeaturesAsync(
        GymSubscription subscription,
        CancellationToken cancellationToken) =>
        _dbContext.PlanFeatures
            .AsNoTracking()
            .Where(pf => pf.PlanId == subscription.PlanId)
            .ToListAsync(cancellationToken);

    // 5. Get gym-specific overrides
    private Task<List<GymFeatureOverride>> LoadOverridesAsync(
        GymSubscription subscription,
        CancellationToken cancellationToken) =>
        _dbContext.GymFeatureOverrides
            .AsNoTracking()
            .Where(o => o.GymId == subscription.GymId)
            .ToListAsync(cancellationToken);

    // 6. Get storage limits for this plan
    private async Task<StorageLimitLookup> LoadStorageLimitsAsync(
        GymSubscription subscription,
        CancellationToken cancellationToken)
    {
        var storageLimits = await _dbContext.PlanStorageLimits
            .AsNoTracking()
            .Where(psl => psl.PlanId == subscription.PlanId)
            .ToListAsync(cancellationToken);

        return new StorageLimitLookup(storageLimits);
    }

    private static bool IsFeatureOn(
        Feature feature,
        List<PlanFeature> planFeatures,
        List<GymFeatureOverride> overrides)
    {
        // Override takes precedence
        var overrideEntry = overrides.FirstOrDefault(o => o.FeatureId == feature.Id);
        if (overrideEntry is not null)
            return overrideEntry.OverrideValue;

        // If no record found → feature is OFF (fail-closed)
        var planFeature = planFeatures.FirstOrDefault(pf => pf.FeatureId == feature.Id);
        return planFeature is not null && planFeature.IsEnabled;
    }

    private static GymFeatureSet CreateFeatureSet(
        GymSubscription subscription,
        SubscriptionPlan plan,
        List<string> enabledFeatures,
        StorageLimitLookup storageLimits)
    {
        return new GymFeatureSet
        {
            GymId = subscription.GymId,
            PlanCode = plan.Code,
            EnabledFeatures = enabledFeatures,
            MemberLimit = plan.MemberLimit,
            TrainerLimit = plan.TrainerLimit,
            StaffLimit = plan.StaffLimit,
            StorageLimitBytes = storageLimits.TotalStorageBytes,
            PhotoLimitPerMember = storageLimits.PhotoLimitPerMember,
            CheckInRetentionDays = storageLimits.CheckInRetentionDays
        };
    }
}
