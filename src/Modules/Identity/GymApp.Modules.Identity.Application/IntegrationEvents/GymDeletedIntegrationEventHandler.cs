using GymApp.Modules.Gyms.IntegrationEvents;
using GymApp.Modules.Identity.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Identity.Application.IntegrationEvents;

internal sealed class GymDeletedIntegrationEventHandler
    : INotificationHandler<GymDeletedIntegrationEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GymDeletedIntegrationEventHandler> _logger;

    public GymDeletedIntegrationEventHandler(
        IUserRepository userRepository,
        ILogger<GymDeletedIntegrationEventHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task Handle(GymDeletedIntegrationEvent n, CancellationToken ct)
    {
        var removed = await _userRepository.RemoveMembershipsForGymAsync(n.GymId, ct);
        _logger.LogInformation(
            "GymDeleted: cleared {Count} gym memberships at gym {GymId}", removed, n.GymId);
    }
}
