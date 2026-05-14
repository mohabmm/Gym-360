using GymApp.Modules.Gyms.IntegrationEvents;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace GymApp.Api.Hubs;

/// <summary>
/// Broadcasts gym branding updates to connected clients via SignalR so they
/// invalidate cached branding without re-login. Fire-and-forget: clients not
/// currently connected pick up changes on next fetch (e.g., next login or
/// app foreground).
/// </summary>
internal sealed class GymBrandingUpdatedIntegrationEventHandler
    : INotificationHandler<GymBrandingUpdatedIntegrationEvent>
{
    private readonly IHubContext<ConfigHub> _hub;
    private readonly ILogger<GymBrandingUpdatedIntegrationEventHandler> _logger;

    public GymBrandingUpdatedIntegrationEventHandler(
        IHubContext<ConfigHub> hub,
        ILogger<GymBrandingUpdatedIntegrationEventHandler> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task Handle(GymBrandingUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var payload = new
        {
            gymId = notification.GymId,
            logoUrl = notification.LogoUrl,
            primaryColor = notification.PrimaryColor,
            secondaryColor = notification.SecondaryColor,
            coverPhotoUrl = notification.CoverPhotoUrl,
        };

        await _hub.Clients
            .Group(ConfigHub.GymGroup(notification.GymId))
            .SendAsync(ConfigHub.BrandingUpdatedEvent, payload, cancellationToken);

        _logger.LogInformation(
            "Broadcast branding_updated to gym:{GymId}",
            notification.GymId);
    }
}
