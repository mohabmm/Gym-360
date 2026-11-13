using GymApp.Common.Application.FeatureFlags;
using Microsoft.Extensions.Logging;

namespace GymApp.Common.Infrastructure.FeatureFlags;

/// <summary>
/// After an admin change is saved: drops the affected gyms' cached flags and tells open apps to
/// refetch. The notification is best effort; clients still refresh on resume and on their timer.
/// </summary>
internal sealed class FeatureFlagChangePublisher
{
    private readonly FeatureFlagCache _cache;
    private readonly IFeatureFlagChangeNotifier _notifier;
    private readonly ILogger<FeatureFlagChangePublisher> _logger;

    public FeatureFlagChangePublisher(
        FeatureFlagCache cache,
        IFeatureFlagChangeNotifier notifier,
        ILogger<FeatureFlagChangePublisher> logger)
    {
        _cache = cache;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task PublishAsync(IReadOnlyCollection<Guid> gymIds, CancellationToken cancellationToken)
    {
        await _cache.InvalidateAsync(gymIds, cancellationToken);

        try
        {
            await _notifier.NotifyGymsChangedAsync(gymIds, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Feature-flag change notification failed");
        }
    }
}
