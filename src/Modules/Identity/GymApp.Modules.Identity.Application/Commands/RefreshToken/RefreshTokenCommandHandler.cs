using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Application.Contracts;
using GymApp.Modules.Identity.Application.DTOs;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;
using DomainEntities = GymApp.Modules.Identity.Domain.Entities;

namespace GymApp.Modules.Identity.Application.Commands.RefreshToken;

internal sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, TokenResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<TokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Result.Failure<TokenResponse>(UserErrors.InvalidCredentials);

        var user = await _userRepository.GetByRefreshTokenAsync(request.Token, cancellationToken);
        if (user is null)
            return Result.Failure<TokenResponse>(UserErrors.InvalidCredentials);

        if (user.Status == UserStatus.Suspended)
            return Result.Failure<TokenResponse>(UserErrors.AccountSuspended);

        if (user.Status == UserStatus.Deleted)
            return Result.Failure<TokenResponse>(UserErrors.AccountDeleted);

        var existing = user.RefreshTokens.FirstOrDefault(rt => rt.Token == request.Token);
        if (existing is null)
            return Result.Failure<TokenResponse>(UserErrors.InvalidCredentials);

        if (existing.IsRevoked)
            return Result.Failure<TokenResponse>(UserErrors.RefreshTokenRevoked);

        if (existing.IsExpired)
            return Result.Failure<TokenResponse>(UserErrors.RefreshTokenExpired);

        // Rotate: revoke old, issue new pair.
        existing.Revoke();

        var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(user, cancellationToken);
        var newRefreshValue = await _jwtTokenService.GenerateRefreshTokenAsync(cancellationToken);
        var newRefreshExpiry = _jwtTokenService.GetRefreshTokenExpiryTime();

        var newRefresh = DomainEntities.RefreshToken.Create(newRefreshValue, newRefreshExpiry);
        user.AddRefreshToken(newRefresh);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var activeMembership = user.GymMemberships.FirstOrDefault(m => m.IsActive);
        var userResponse = new UserResponse(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.Role.ToRoleClaim(),
            activeMembership?.GymId,
            null);

        return Result.Success(new TokenResponse(
            accessToken,
            newRefreshValue,
            _jwtTokenService.GetAccessTokenExpiryTime(),
            userResponse));
    }
}
