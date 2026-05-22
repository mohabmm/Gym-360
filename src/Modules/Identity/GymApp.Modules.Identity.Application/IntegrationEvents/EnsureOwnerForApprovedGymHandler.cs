using GymApp.Modules.Gyms.IntegrationEvents;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Identity.Application.IntegrationEvents;

/// <summary>
/// When admin registers a gym via /api/v1/gyms (or approves a pending gym), the
/// caller passes the owner's User Id but no Owner aggregate is provisioned in the
/// Identity module. Without an Owner row + User.OwnerId link, the owner cannot
/// login as an Owner principal: Login/SelectGym/GetCurrentUser all guard on
/// user.OwnerId.HasValue and fall through to the no-membership branch (which
/// returns NotOwner / empty ownedGyms). Per CLAUDE.md rule 11, owners scope
/// across gyms via User.OwnerId, so the link is required for the chooser to work.
/// This handler ensures both exist. Idempotent: if the user is already linked to
/// an Owner aggregate (the self-serve signup flow does it inline), it's a no-op.
/// </summary>
internal sealed class EnsureOwnerForApprovedGymHandler
    : INotificationHandler<GymApprovedIntegrationEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ILogger<EnsureOwnerForApprovedGymHandler> _logger;

    public EnsureOwnerForApprovedGymHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        ILogger<EnsureOwnerForApprovedGymHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(GymApprovedIntegrationEvent n, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(n.OwnerUserId, ct);
        if (user is null)
        {
            _logger.LogWarning(
                "GymApproved: owner user {UserId} not found; cannot link to gym {GymId}",
                n.OwnerUserId, n.GymId);
            return;
        }

        if (user.Role != UserRole.GymOwner)
        {
            _logger.LogWarning(
                "GymApproved: user {UserId} is not a GymOwner (role={Role}); skipping owner provisioning for gym {GymId}",
                n.OwnerUserId, user.Role, n.GymId);
            return;
        }

        if (user.OwnerId.HasValue) return;

        var billingEmail = user.Email.Value;
        var companyName = $"{user.FirstName} {user.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(companyName)) companyName = billingEmail;

        var owner = Owner.Create(user.Id, companyName, billingEmail);
        await _userRepository.AddOwnerAsync(owner, ct);
        user.LinkToOwner(owner.Id);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "GymApproved: provisioned Owner aggregate {OwnerId} and linked user {UserId} for gym {GymId}",
            owner.Id, user.Id, n.GymId);
    }
}
