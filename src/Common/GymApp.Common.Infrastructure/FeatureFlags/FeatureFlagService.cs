using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Infrastructure.FeatureFlags.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GymApp.Common.Infrastructure.FeatureFlags;

/// <summary>
/// Database-backed <see cref="IFeatureFlagService"/>: override &gt; preset &gt; OFF, cached per gym in
/// Redis (<c>ff:v1:{gymId}</c>, 5 min). Every admin change removes the gym's key, so the API sees
/// changes immediately; apps pick them up on their next flag refresh.
/// </summary>
internal sealed class FeatureFlagService : IFeatureFlagService
{
    private readonly FeatureFlagsDbContext _db;
    private readonly FeatureFlagCache _cache;
    private readonly FeatureFlagOptions _options;

    public FeatureFlagService(
        FeatureFlagsDbContext db,
        FeatureFlagCache cache,
        IOptions<FeatureFlagOptions> options)
    {
        _db = db;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<bool> IsEnabledAsync(Guid gymId, string featureKey, CancellationToken cancellationToken = default)
    {
        if (!FeatureRegistry.IsKnown(featureKey))
            return false;

        var flags = await GetResolvedAsync(gymId, cancellationToken);
        return flags.IsEnabled(featureKey);
    }

    public async Task<GymFeatureFlags> GetResolvedAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetAsync(gymId, cancellationToken);
        if (cached is not null)
            return cached;

        var resolved = await ResolveFromDatabaseAsync(gymId, cancellationToken);
        await _cache.SetAsync(resolved, cancellationToken);
        return resolved;
    }

    private async Task<GymFeatureFlags> ResolveFromDatabaseAsync(Guid gymId, CancellationToken cancellationToken)
    {
        var presetCode = await _db.GymPresets.AsNoTracking()
            .Where(g => g.GymId == gymId)
            .Select(g => g.PresetCode)
            .FirstOrDefaultAsync(cancellationToken) ?? _options.DefaultPreset;

        var limitsPlanCode = await _db.Presets.AsNoTracking()
            .Where(p => p.Code == presetCode)
            .Select(p => p.LimitsPlanCode)
            .FirstOrDefaultAsync(cancellationToken);

        var presetValues = await _db.PresetFeatures.AsNoTracking()
            .Where(p => p.PresetCode == presetCode)
            .ToDictionaryAsync(p => p.FeatureKey, p => p.Enabled, cancellationToken);

        var overrides = await _db.GymOverrides.AsNoTracking()
            .Where(o => o.GymId == gymId)
            .ToDictionaryAsync(o => o.FeatureKey, o => o.Enabled, cancellationToken);

        // Keys come from code, not the DB, so a flag defined in code is always resolved (OFF by default).
        var flags = FeatureResolver.Resolve(FeatureRegistry.All.Select(d => d.Key), presetValues, overrides);

        return new GymFeatureFlags(gymId, presetCode, flags, DateTime.UtcNow, limitsPlanCode);
    }
}

/// <summary>Redis wrapper that never lets a cache outage fail a request.</summary>
internal sealed class FeatureFlagCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    private readonly ICacheService _cache;
    private readonly ILogger<FeatureFlagCache> _logger;

    public FeatureFlagCache(ICacheService cache, ILogger<FeatureFlagCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public static string KeyFor(Guid gymId) => $"ff:v1:{gymId}";

    public async Task<GymFeatureFlags?> GetAsync(Guid gymId, CancellationToken cancellationToken)
    {
        try
        {
            return await _cache.GetAsync<GymFeatureFlags>(KeyFor(gymId), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Feature-flag cache read failed for gym {GymId}; using database", gymId);
            return null;
        }
    }

    public async Task SetAsync(GymFeatureFlags flags, CancellationToken cancellationToken)
    {
        try
        {
            await _cache.SetAsync(KeyFor(flags.GymId), flags, Ttl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Feature-flag cache write failed for gym {GymId}", flags.GymId);
        }
    }

    public async Task InvalidateAsync(IEnumerable<Guid> gymIds, CancellationToken cancellationToken)
    {
        foreach (var gymId in gymIds.Distinct())
        {
            try
            {
                await _cache.RemoveAsync(KeyFor(gymId), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Feature-flag cache invalidation failed for gym {GymId}", gymId);
            }
        }
    }
}

public sealed class FeatureFlagOptions
{
    public const string SectionName = "FeatureFlags";

    /// <summary>Preset used for gyms with no assignment yet (new gyms). Never "all on".</summary>
    public string DefaultPreset { get; set; } = FeatureRegistry.PtModulePreset;
}
