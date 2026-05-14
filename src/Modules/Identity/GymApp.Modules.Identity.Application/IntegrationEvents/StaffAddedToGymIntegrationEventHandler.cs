using GymApp.Modules.Gyms.IntegrationEvents;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Identity.Application.IntegrationEvents;

internal sealed class StaffAddedToGymIntegrationEventHandler
    : INotificationHandler<StaffAddedToGymIntegrationEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ILogger<StaffAddedToGymIntegrationEventHandler> _logger;

    public StaffAddedToGymIntegrationEventHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        ILogger<StaffAddedToGymIntegrationEventHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(StaffAddedToGymIntegrationEvent n, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(n.UserId, ct);
        if (user is null)
        {
            _logger.LogWarning(
                "StaffAddedToGym: user {UserId} not found while linking to gym {GymId}",
                n.UserId, n.GymId);
            return;
        }

        if (user.GymMemberships.Any(m => m.GymId == n.GymId && m.Role == UserRole.GymStaff && m.IsActive))
            return;

        var otherActive = user.GymMemberships.FirstOrDefault(m => m.IsActive && m.GymId != n.GymId);
        if (otherActive is not null)
        {
            _logger.LogWarning(
                "StaffAddedToGym: user {UserId} already has active membership at gym {OtherGymId}; skipping link to {GymId}",
                n.UserId, otherActive.GymId, n.GymId);
            return;
        }

        var linkResult = user.LinkToGym(n.GymId, UserRole.GymStaff, section: null, memberNumber: null);
        if (linkResult.IsFailure)
        {
            _logger.LogWarning(
                "StaffAddedToGym: LinkToGym failed for user {UserId} gym {GymId}: {Error}",
                n.UserId, n.GymId, linkResult.Error.Code);
            return;
        }

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
