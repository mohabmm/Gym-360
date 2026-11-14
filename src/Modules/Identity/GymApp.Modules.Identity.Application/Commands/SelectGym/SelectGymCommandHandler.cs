using GymApp.Common.Application.Messaging;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Application.Contracts;
using GymApp.Modules.Identity.Application.DTOs;
using GymApp.Modules.Identity.Application.Services;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;

namespace GymApp.Modules.Identity.Application.Commands.SelectGym;

/// <summary>
/// Scopes an owner's session to one of their gyms. The new refresh token is saved with the gym,
/// so refreshing later keeps the session on that gym instead of dropping back to no gym.
/// </summary>
internal sealed class SelectGymCommandHandler : ICommandHandler<SelectGymCommand, TokenResponse>
{
    private static readonly Error NotOwner = new("SelectGym.NotOwner", "Gym selection is only available for Owner principals.");
    private static readonly Error GymNotFound = new("SelectGym.NotFound", "Gym not found in registry.");
    private static readonly Error Forbidden = new("SelectGym.Forbidden", "You do not own this gym.");

    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IGymRegistry _gymRegistry;

    public SelectGymCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        IGymRegistry gymRegistry)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _gymRegistry = gymRegistry;
    }

    public async Task<Result<TokenResponse>> Handle(SelectGymCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<TokenResponse>(UserErrors.NotFound(request.UserId));

        if (!OwnerGymScope.IsOwner(user))
            return Result.Failure<TokenResponse>(NotOwner);

        var registration = await _gymRegistry.FindAsync(request.GymId, cancellationToken);
        if (registration is null)
            return Result.Failure<TokenResponse>(GymNotFound);

        if (!OwnerGymScope.Owns(user, registration))
            return Result.Failure<TokenResponse>(Forbidden);

        return Result.Success(await IssueForGymAsync(user, registration, cancellationToken));
    }

    private async Task<TokenResponse> IssueForGymAsync(User user, GymRegistration gym, CancellationToken cancellationToken)
    {
        var accessToken = await _jwtTokenService.GenerateAccessTokenForGymAsync(
            user, gym.GymId, gym.SchemaPrefix, cancellationToken);

        // Persist the refresh token: before, it was returned but never saved, so the owner's
        // first refresh (after the access token expired) failed and gym screens broke.
        var refreshTokenValue = await _jwtTokenService.GenerateRefreshTokenAsync(cancellationToken);
        user.AddRefreshToken(Domain.Entities.RefreshToken.Create(refreshTokenValue, _jwtTokenService.GetRefreshTokenExpiryTime(), gym.GymId));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TokenResponse(
            accessToken,
            refreshTokenValue,
            _jwtTokenService.GetAccessTokenExpiryTime(),
            OwnerUserResponse.For(user, gym));
    }
}
