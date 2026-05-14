using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GymApp.Api.Hubs;

/// <summary>
/// SignalR hub for real-time plan/feature configuration updates.
/// When admin deploys changes, the <c>ConfigUpdated</c> event is broadcast to subscribed clients
/// (Web apps) so they can invalidate cached plan-features queries without re-login.
/// Mobile apps receive the equivalent signal via FCM silent push.
/// </summary>
[Authorize]
public sealed class ConfigHub : Hub
{
    public const string HubPath = "/hubs/config";
    public const string ConfigUpdatedEvent = "config_updated";
    public const string BrandingUpdatedEvent = "branding_updated";

    public static string GymGroup(Guid gymId) => $"gym:{gymId}";

    /// <summary>Clients subscribe to a gym scope group to receive gym-specific config updates.</summary>
    public Task SubscribeToGym(Guid gymId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GymGroup(gymId));

    public Task UnsubscribeFromGym(Guid gymId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GymGroup(gymId));
}
