using GymApp.Modules.Gyms.Shared;
using GymApp.Common.Application.Tenancy;
using GymApp.Modules.Identity.Application.DTOs;
using GymApp.Modules.Identity.Domain.Enums;
using DomainEntities = GymApp.Modules.Identity.Domain.Entities;

namespace GymApp.Modules.Identity.Application.Commands.Login;

/// <summary>
/// Builds the <see cref="UserResponse"/> returned by <see cref="LoginCommandHandler"/>:
/// owned-gym chooser for Owner principals, active gym name for everyone else.
/// </summary>
internal sealed class LoginUserResponseBuilder
{
    private readonly IGymLookupService _gymLookupService;
    private readonly IGymRegistry _gymRegistry;

    public LoginUserResponseBuilder(IGymLookupService gymLookupService, IGymRegistry gymRegistry)
    {
        _gymLookupService = gymLookupService;
        _gymRegistry = gymRegistry;
    }

    public Task<UserResponse> BuildAsync(DomainEntities.User user, CancellationToken cancellationToken)
    {
        // Owner branch: surface owned gyms so the client can render a chooser.
        // No GymId is embedded in the token until the Owner picks via /api/auth/select-gym.
        return user.Role == UserRole.GymOwner && user.OwnerId.HasValue
            ? BuildOwnerResponseAsync(user, user.OwnerId.Value, cancellationToken)
            : BuildMemberResponseAsync(user, cancellationToken);
    }

    private async Task<UserResponse> BuildOwnerResponseAsync(
        DomainEntities.User user,
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        // gyms_registry.OwnerId may be either the Owner aggregate Id (legacy
        // bootstrap migration) or the User Id (admin /gyms flow). Query both
        // so the chooser always reflects every gym the user actually owns.
        var byAggregate = await _gymRegistry.GetByOwnerAsync(ownerId, cancellationToken);
        var byUser = await _gymRegistry.GetByOwnerAsync(user.Id, cancellationToken);
        var ownedGyms = byAggregate
            .Concat(byUser)
            .GroupBy(g => g.GymId)
            .Select(g => g.First())
            .ToList();

        return new UserResponse(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.Role.ToRoleClaim(),
            GymId: null,
            GymName: null,
            PrincipalType: MapPrincipalType(user.Role),
            OwnerId: user.OwnerId,
            OwnedGyms: ownedGyms.Select(g => new OwnedGymSummary(g.GymId, g.Name, g.Slug, g.SchemaPrefix)).ToList(),
            DashboardMode: null);
    }

    private async Task<UserResponse> BuildMemberResponseAsync(DomainEntities.User user, CancellationToken cancellationToken)
    {
        var activeMembership = user.GymMemberships.FirstOrDefault(m => m.IsActive);
        string? gymName = null;
        if (activeMembership is not null)
            gymName = await _gymLookupService.GetGymNameAsync(activeMembership.GymId, cancellationToken);

        return new UserResponse(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.Role.ToRoleClaim(),
            activeMembership?.GymId,
            gymName,
            PrincipalType: MapPrincipalType(user.Role),
            OwnerId: user.OwnerId);
    }

    private static string? MapPrincipalType(UserRole role) => role switch
    {
        UserRole.GymOwner => "Owner",
        UserRole.GymStaff => "Staff",
        UserRole.Trainer => "Trainer",
        UserRole.Trainee => "Trainee",
        UserRole.PlatformAdmin => "PlatformAdmin",
        _ => null
    };
}
