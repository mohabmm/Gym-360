namespace GymApp.Common.Application.FeatureFlags;

/// <summary>
/// Tells connected clients that a gym's flags changed (web apps over SignalR). Best effort:
/// mobile apps also refresh on resume and every few minutes, so a missed signal only delays.
/// </summary>
public interface IFeatureFlagChangeNotifier
{
    Task NotifyGymsChangedAsync(IReadOnlyCollection<Guid> gymIds, CancellationToken cancellationToken = default);
}
