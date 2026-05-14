using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Application.Contracts;
using GymApp.Modules.Identity.Application.DTOs;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Domain.ValueObjects;
using DomainEntities = GymApp.Modules.Identity.Domain.Entities;

namespace GymApp.Modules.Identity.Application.Commands.Login;

internal sealed class LoginCommandHandler : ICommandHandler<LoginCommand, TokenResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IGymLookupService _gymLookupService;
    private readonly IGymRegistry _gymRegistry;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IGymLookupService gymLookupService,
        IGymRegistry gymRegistry)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _gymLookupService = gymLookupService;
        _gymRegistry = gymRegistry;
    }

    public async Task<Result<TokenResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result.Failure<TokenResponse>(UserErrors.InvalidCredentials);

        var user = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
        if (user is null)
            return Result.Failure<TokenResponse>(UserErrors.InvalidCredentials);

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result.Failure<TokenResponse>(UserErrors.InvalidCredentials);

        if (user.Status == UserStatus.Suspended)
            return Result.Failure<TokenResponse>(UserErrors.AccountSuspended);

        if (user.Status == UserStatus.Deleted)
            return Result.Failure<TokenResponse>(UserErrors.AccountDeleted);

        user.RecordLogin();

        var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(user, cancellationToken);
        var refreshTokenValue = await _jwtTokenService.GenerateRefreshTokenAsync(cancellationToken);
        var refreshTokenExpiry = _jwtTokenService.GetRefreshTokenExpiryTime();

        var refreshToken = DomainEntities.RefreshToken.Create(refreshTokenValue, refreshTokenExpiry);
        user.AddRefreshToken(refreshToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var principalType = MapPrincipalType(user.Role);

        // Owner branch: surface owned gyms so the client can render a chooser.
        // No GymId is embedded in the token until the Owner picks via /api/auth/select-gym.
        if (user.Role == UserRole.GymOwner && user.OwnerId.HasValue)
        {
            // gyms_registry.OwnerId may be either the Owner aggregate Id (legacy
            // bootstrap migration) or the User Id (admin /gyms flow). Query both
            // so the chooser always reflects every gym the user actually owns.
            var byAggregate = await _gymRegistry.GetByOwnerAsync(user.OwnerId.Value, cancellationToken);
            var byUser = await _gymRegistry.GetByOwnerAsync(user.Id, cancellationToken);
            var ownedGyms = byAggregate
                .Concat(byUser)
                .GroupBy(g => g.GymId)
                .Select(g => g.First())
                .ToList();

            var ownerResponse = new UserResponse(
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
                DashboardMode: null);

            return Result.Success(new TokenResponse(
                accessToken,
                refreshTokenValue,
                _jwtTokenService.GetAccessTokenExpiryTime(),
                ownerResponse));
        }

        var activeMembership = user.GymMemberships.FirstOrDefault(m => m.IsActive);
        string? gymName = null;
        if (activeMembership is not null)
            gymName = await _gymLookupService.GetGymNameAsync(activeMembership.GymId, cancellationToken);

        var userResponse = new UserResponse(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.Role.ToRoleClaim(),
            activeMembership?.GymId,
            gymName,
            PrincipalType: principalType,
            OwnerId: user.OwnerId);

        return Result.Success(new TokenResponse(
            accessToken,
            refreshTokenValue,
            _jwtTokenService.GetAccessTokenExpiryTime(),
            userResponse));
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
