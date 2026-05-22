using GymApp.Common.Application.Abstractions;
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
        return featureSet?.StorageLimitBytes ?? 100L * 1024 * 1024; // 100MB default
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

        var featureSet = await BuildFeatureSetAsync(gymId, cancellationToken);
        if (featureSet is not null)
        {
            await _cacheService.SetAsync(cacheKey, featureSet, CacheTtl, cancellationToken);
        }

        return featureSet;
    }

    private async Task<GymFeatureSet?> BuildFeatureSetAsync(
        Guid gymId,
        CancellationToken cancellationToken)
    {
        // 1. Get the gym's subscription
        var subscription = await _dbContext.GymSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.GymId == gymId && s.IsActive, cancellationToken);

        if (subscription is null)
        {
            _logger.LogWarning("No active subscription found for gym {GymId}", gymId);
            return null;
        }

        // 2. Get the plan details
        var plan = await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == subscription.PlanId, cancellationToken);

        if (plan is null)
        {
            _logger.LogWarning("Plan {PlanId} not found for gym {GymId}", subscription.PlanId, gymId);
            return null;
        }

        // 3. Get all active features
        var allFeatures = await _dbContext.Features
            .AsNoTracking()
            .Where(f => f.IsActive)
            .ToListAsync(cancellationToken);

        // 4. Get plan-feature mappings
        var planFeatures = await _dbContext.PlanFeatures
            .AsNoTracking()
            .Where(pf => pf.PlanId == subscription.PlanId)
            .ToListAsync(cancellationToken);

        // 5. Get gym-specific overrides
        var overrides = await _dbContext.GymFeatureOverrides
            .AsNoTracking()
            .Where(o => o.GymId == gymId)
            .ToListAsync(cancellationToken);

        // 6. Get storage limits for this plan
        var storageLimits = await _dbContext.PlanStorageLimits
            .AsNoTracking()
            .Where(psl => psl.PlanId == subscription.PlanId)
            .ToListAsync(cancellationToken);

        // 7. Build the resolved feature set: Override > Plan Default > OFF
        var enabledFeatures = new List<string>();

        foreach (var feature in allFeatures)
        {
            var overrideEntry = overrides.FirstOrDefault(o => o.FeatureId == feature.Id);
            if (overrideEntry is not null)
            {
                // Override takes precedence
                if (overrideEntry.OverrideValue)
                {
                    enabledFeatures.Add(feature.Code);
                }
                continue;
            }

            var planFeature = planFeatures.FirstOrDefault(pf => pf.FeatureId == feature.Id);
            if (planFeature is not null && planFeature.IsEnabled)
            {
                enabledFeatures.Add(feature.Code);
            }
            // If no record found → feature is OFF (fail-closed)
        }

        // Resolve storage limits
        var totalStorageLimit = storageLimits
            .FirstOrDefault(sl => sl.ResourceType == "total_storage");
        var photoLimit = storageLimits
            .FirstOrDefault(sl => sl.ResourceType == "progress_photos_per_member");
        var checkInRetention = storageLimits
            .FirstOrDefault(sl => sl.ResourceType == "checkin_retention_days");

        return new GymFeatureSet
        {
            GymId = gymId,
            PlanCode = plan.Code,
            EnabledFeatures = enabledFeatures,
            MemberLimit = plan.MemberLimit,
            TrainerLimit = plan.TrainerLimit,
            StaffLimit = plan.StaffLimit,
            StorageLimitBytes = totalStorageLimit?.LimitValue ?? 100L * 1024 * 1024,
            PhotoLimitPerMember = photoLimit is not null ? (int)photoLimit.LimitValue : 5,
            CheckInRetentionDays = checkInRetention is not null ? (int)checkInRetention.LimitValue : 30
        };
    }
}

/// <summary>
/// Cached gym feature set — stored in Redis with TTL 5 min.
/// </summary>
internal sealed class GymFeatureSet
{
    public Guid GymId { get; init; }
    public string PlanCode { get; init; } = "free";
    public List<string> EnabledFeatures { get; init; } = new();
    public int MemberLimit { get; init; }
    public int TrainerLimit { get; init; }
    public int StaffLimit { get; init; }
    public long StorageLimitBytes { get; init; }
    public int PhotoLimitPerMember { get; init; }
    public int CheckInRetentionDays { get; init; }

    public bool IsEnabled(string featureCode) => EnabledFeatures.Contains(featureCode);
}
