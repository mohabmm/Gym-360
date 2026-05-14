using GymApp.Common.Application.Messaging;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Contracts;
using GymApp.Modules.Identity.Application.DTOs;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;

namespace GymApp.Modules.Identity.Application.Commands.SelectGym;

internal sealed class SelectGymCommandHandler : ICommandHandler<SelectGymCommand, TokenResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IGymRegistry _gymRegistry;

    public SelectGymCommandHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IGymRegistry gymRegistry)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _gymRegistry = gymRegistry;
    }

    public async Task<Result<TokenResponse>> Handle(SelectGymCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<TokenResponse>(UserErrors.NotFound(request.UserId));

        if (user.Role != UserRole.GymOwner || !user.OwnerId.HasValue)
            return Result.Failure<TokenResponse>(new Error("SelectGym.NotOwner", "Gym selection is only available for Owner principals."));

        var registration = await _gymRegistry.FindAsync(request.GymId, cancellationToken);
        if (registration is null)
            return Result.Failure<TokenResponse>(new Error("SelectGym.NotFound", "Gym not found in registry."));

        // registration.OwnerId may be the Owner aggregate Id (legacy bootstrap)
        // or the User Id (admin /gyms flow). Accept either so the owner chooser
        // works regardless of provisioning path.
        if (registration.OwnerId != user.OwnerId.Value && registration.OwnerId != user.Id)
            return Result.Failure<TokenResponse>(new Error("SelectGym.Forbidden", "You do not own this gym."));

        var accessToken = await _jwtTokenService.GenerateAccessTokenForGymAsync(
            user,
            registration.GymId,
            registration.SchemaPrefix,
            cancellationToken);

        var refreshTokenValue = await _jwtTokenService.GenerateRefreshTokenAsync(cancellationToken);

        var userResponse = new UserResponse(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.Role.ToRoleClaim(),
            registration.GymId,
            registration.Name,
            PrincipalType: "Owner",
            OwnerId: user.OwnerId);

        return Result.Success(new TokenResponse(
            accessToken,
            refreshTokenValue,
            _jwtTokenService.GetAccessTokenExpiryTime(),
            userResponse));
    }
}
