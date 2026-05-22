using GymApp.Modules.Gyms.Shared;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.DTOs;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;

namespace GymApp.Modules.Identity.Application.Queries.GetCurrentUser;

internal sealed class GetCurrentUserQueryHandler : IQueryHandler<GetCurrentUserQuery, UserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IGymLookupService _gymLookupService;
    private readonly IGymRegistry _gymRegistry;

    public GetCurrentUserQueryHandler(
        IUserRepository userRepository,
        IGymLookupService gymLookupService,
        IGymRegistry gymRegistry)
    {
        _userRepository = userRepository;
        _gymLookupService = gymLookupService;
        _gymRegistry = gymRegistry;
    }

    public async Task<Result<UserResponse>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<UserResponse>(UserErrors.NotFound(request.UserId));

        var principalType = MapPrincipalType(user.Role);

        if (user.Role == UserRole.GymOwner && user.OwnerId.HasValue)
        {
            // Two writers populate gyms_registry.OwnerId in production:
            //   * the legacy hard-isolation bootstrap migration stored the Owner *aggregate* Id
            //   * RegisterGymCommandHandler (admin /gyms flow) stores the User Id
            // Query both so owners always see every gym they own regardless of provisioning path.
            var byAggregate = await _gymRegistry.GetByOwnerAsync(user.OwnerId.Value, cancellationToken);
            var byUser = await _gymRegistry.GetByOwnerAsync(user.Id, cancellationToken);
            var ownedGyms = byAggregate
                .Concat(byUser)
                .GroupBy(g => g.GymId)
                .Select(g => g.First())
                .ToList();

            return Result.Success(new UserResponse(
                user.Id,
                user.Email.Value,
                user.FirstName,
                user.LastName,
                user.Role.ToRoleClaim(),
                GymId: null,
                GymName: null,
                PrincipalType: principalType,
                OwnerId: user.OwnerId,
                OwnedGyms: ownedGyms.Select(g => new OwnedGymSummary(g.GymId, g.Name, g.Slug, g.SchemaPrefix)).ToList(),
                DashboardMode: null));
        }

        var activeMembership = user.GymMemberships.FirstOrDefault(m => m.IsActive);
        string? gymName = null;
        if (activeMembership is not null)
            gymName = await _gymLookupService.GetGymNameAsync(activeMembership.GymId, cancellationToken);

        return Result.Success(new UserResponse(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.Role.ToRoleClaim(),
            activeMembership?.GymId,
            gymName,
            PrincipalType: principalType,
            OwnerId: user.OwnerId));
    }

    private static string? MapPrincipalType(UserRole role) => role switch
    {
        UserRole.GymOwner => "Owner",
        UserRole.GymStaff => "Staff",
        UserRole.Trainer => "Trainer",
        UserRole.Trainee => "Trainee",
        UserRole.PlatformAdmin => "PlatformAdmin",
        _ => null,
    };
}
