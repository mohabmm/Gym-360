using GymApp.Common.Application.Messaging;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Application.Contracts;
using GymApp.Modules.Identity.Application.DTOs;
using GymApp.Modules.Identity.Application.Services;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;
using DomainEntities = GymApp.Modules.Identity.Domain.Entities;

namespace GymApp.Modules.Identity.Application.Commands.RefreshToken;

/// <summary>
/// Rotates the refresh token and issues a new access token. An owner who selected a gym stays
/// scoped to it (ownership is re-checked); everyone else is scoped from their active membership.
/// </summary>
internal sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, TokenResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IGymRegistry _gymRegistry;

    public RefreshTokenCommandHandler(
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

    public async Task<Result<TokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Result.Failure<TokenResponse>(UserErrors.InvalidCredentials);

        var user = await _userRepository.GetByRefreshTokenAsync(request.Token, cancellationToken);
        if (user is null)
            return Result.Failure<TokenResponse>(UserErrors.InvalidCredentials);

        var existing = user.RefreshTokens.FirstOrDefault(rt => rt.Token == request.Token);
        if (CheckUsable(user, existing) is { } error)
            return Result.Failure<TokenResponse>(error);

        // Rotate: revoke old, issue new pair.
        existing!.Revoke();

        var selectedGym = existing.SelectedGymId is { } gymId
            ? await OwnerGymScope.FindOwnedGymAsync(_gymRegistry, user, gymId, cancellationToken)
            : null;

        var accessToken = selectedGym is null
            ? await _jwtTokenService.GenerateAccessTokenAsync(user, cancellationToken)
            : await _jwtTokenService.GenerateAccessTokenForGymAsync(
                user, selectedGym.GymId, selectedGym.SchemaPrefix, cancellationToken);

        var newRefreshValue = await _jwtTokenService.GenerateRefreshTokenAsync(cancellationToken);
        user.AddRefreshToken(DomainEntities.RefreshToken.Create(
            newRefreshValue, _jwtTokenService.GetRefreshTokenExpiryTime(), selectedGym?.GymId));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new TokenResponse(
            accessToken,
            newRefreshValue,
            _jwtTokenService.GetAccessTokenExpiryTime(),
            selectedGym is null ? MembershipUserResponse(user) : OwnerUserResponse.For(user, selectedGym)));
    }

    /// <summary>Why the account or token can't be refreshed, or null when it can.</summary>
    private static Error? CheckUsable(User user, DomainEntities.RefreshToken? token)
    {
        if (user.Status == UserStatus.Suspended)
            return UserErrors.AccountSuspended;

        if (user.Status == UserStatus.Deleted)
            return UserErrors.AccountDeleted;

        if (token is null)
            return UserErrors.InvalidCredentials;

        if (token.IsRevoked)
            return UserErrors.RefreshTokenRevoked;

        return token.IsExpired ? UserErrors.RefreshTokenExpired : null;
    }

    private static UserResponse MembershipUserResponse(User user) =>
        new(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.Role.ToRoleClaim(),
            user.GymMemberships.FirstOrDefault(m => m.IsActive)?.GymId,
            null);
}
