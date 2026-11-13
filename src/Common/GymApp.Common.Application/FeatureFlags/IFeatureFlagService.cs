namespace GymApp.Common.Application.FeatureFlags;

/// <summary>
/// The only way feature code reads flags. Swap the implementation (e.g. a self-hosted Flagsmith
/// provider) without touching callers. Implementations must be fail-closed: unknown key → OFF.
/// </summary>
public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(Guid gymId, string featureKey, CancellationToken cancellationToken = default);

    Task<GymFeatureFlags> GetResolvedAsync(Guid gymId, CancellationToken cancellationToken = default);
}

/// <summary>All resolved flags for one gym.</summary>
public sealed record GymFeatureFlags(Guid GymId, string PresetCode, IReadOnlyList<ResolvedFlag> Flags)
{
    public DateTime ResolvedAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When the gym's preset pins seat/storage limits to a subscription plan (pilot gyms use
    /// <c>business</c> limits), its code; otherwise null and the gym's own subscription plan applies.
    /// </summary>
    public string? LimitsPlanCode { get; init; }

    public bool IsEnabled(string key) =>
        Flags.FirstOrDefault(f => string.Equals(f.Key, key, StringComparison.Ordinal))?.Enabled ?? false;

    public IReadOnlyList<string> EnabledKeys() =>
        Flags.Where(f => f.Enabled).Select(f => f.Key).ToList();
}
