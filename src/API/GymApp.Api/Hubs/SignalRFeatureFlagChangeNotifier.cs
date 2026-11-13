using GymApp.Common.Application.FeatureFlags;
using Microsoft.AspNetCore.SignalR;

namespace GymApp.Api.Hubs;

/// <summary>
/// Sends <c>config_updated</c> ({ gymId, scope: "feature_flags" }) to each changed gym's ConfigHub
/// group so connected web clients refetch flags immediately. Mobile apps refresh on resume/timer.
/// </summary>
internal sealed class SignalRFeatureFlagChangeNotifier : IFeatureFlagChangeNotifier
{
    private readonly IHubContext<ConfigHub> _hub;

    public SignalRFeatureFlagChangeNotifier(IHubContext<ConfigHub> hub)
    {
        _hub = hub;
    }

    public async Task NotifyGymsChangedAsync(IReadOnlyCollection<Guid> gymIds, CancellationToken cancellationToken = default)
    {
        foreach (var gymId in gymIds)
        {
            await _hub.Clients
                .Group(ConfigHub.GymGroup(gymId))
                .SendAsync(ConfigHub.ConfigUpdatedEvent, new { gymId, scope = "feature_flags" }, cancellationToken);
        }
    }
}
