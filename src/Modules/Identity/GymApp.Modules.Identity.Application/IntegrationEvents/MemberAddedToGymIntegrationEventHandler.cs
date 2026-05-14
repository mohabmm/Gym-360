using GymApp.Modules.Gyms.IntegrationEvents;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Identity.Application.IntegrationEvents;

internal sealed class MemberAddedToGymIntegrationEventHandler
    : INotificationHandler<MemberAddedToGymIntegrationEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ILogger<MemberAddedToGymIntegrationEventHandler> _logger;

    public MemberAddedToGymIntegrationEventHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        ILogger<MemberAddedToGymIntegrationEventHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(MemberAddedToGymIntegrationEvent n, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(n.MemberId, ct);
        if (user is null)
        {
            _logger.LogWarning(
                "MemberAddedToGym: user {UserId} not found while linking to gym {GymId}",
                n.MemberId, n.GymId);
            return;
        }

        if (user.GymMemberships.Any(m => m.GymId == n.GymId && m.Role == UserRole.Trainee && m.IsActive))
            return;

        var otherActive = user.GymMemberships.FirstOrDefault(m => m.IsActive && m.GymId != n.GymId);
        if (otherActive is not null)
        {
            _logger.LogWarning(
                "MemberAddedToGym: user {UserId} already has active membership at gym {OtherGymId}; skipping link to {GymId}",
                n.MemberId, otherActive.GymId, n.GymId);
            return;
        }

        var linkResult = user.LinkToGym(n.GymId, UserRole.Trainee, n.Section, n.MemberNumber);
        if (linkResult.IsFailure)
        {
            _logger.LogWarning(
                "MemberAddedToGym: LinkToGym failed for user {UserId} gym {GymId}: {Error}",
                n.MemberId, n.GymId, linkResult.Error.Code);
            return;
        }

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
