using GymApp.Modules.Gyms.IntegrationEvents;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Identity.Application.IntegrationEvents;

internal sealed class TrainerAddedToGymIntegrationEventHandler
    : INotificationHandler<TrainerAddedToGymIntegrationEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ILogger<TrainerAddedToGymIntegrationEventHandler> _logger;

    public TrainerAddedToGymIntegrationEventHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        ILogger<TrainerAddedToGymIntegrationEventHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(TrainerAddedToGymIntegrationEvent n, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(n.TrainerId, ct);
        if (user is null)
        {
            _logger.LogWarning(
                "TrainerAddedToGym: user {UserId} not found while linking to gym {GymId}",
                n.TrainerId, n.GymId);
            return;
        }

        if (user.GymMemberships.Any(m => m.GymId == n.GymId && m.Role == UserRole.Trainer && m.IsActive))
            return;

        var linkResult = user.LinkToGym(n.GymId, UserRole.Trainer, null, null);
        if (linkResult.IsFailure)
        {
            _logger.LogWarning(
                "TrainerAddedToGym: LinkToGym failed for user {UserId} gym {GymId}: {Error}",
                n.TrainerId, n.GymId, linkResult.Error.Code);
            return;
        }

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
