using GymApp.Common.Application.Messaging;
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
    private readonly LoginUserResponseBuilder _userResponseBuilder;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        LoginUserResponseBuilder userResponseBuilder)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _userResponseBuilder = userResponseBuilder;
    }

    public async Task<Result<TokenResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var userResult = await AuthenticateAsync(request, cancellationToken);
        if (userResult.IsFailure)
            return Result.Failure<TokenResponse>(userResult.Error);

        var user = userResult.Value;
        user.RecordLogin();

        var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(user, cancellationToken);
        var refreshTokenValue = await _jwtTokenService.GenerateRefreshTokenAsync(cancellationToken);
        var refreshTokenExpiry = _jwtTokenService.GetRefreshTokenExpiryTime();

        var refreshToken = DomainEntities.RefreshToken.Create(refreshTokenValue, refreshTokenExpiry);
        user.AddRefreshToken(refreshToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var userResponse = await _userResponseBuilder.BuildAsync(user, cancellationToken);

        return Result.Success(new TokenResponse(
            accessToken,
            refreshTokenValue,
            _jwtTokenService.GetAccessTokenExpiryTime(),
            userResponse));
    }

    private async Task<Result<DomainEntities.User>> AuthenticateAsync(LoginCommand request, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result.Failure<DomainEntities.User>(UserErrors.InvalidCredentials);

        var user = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result.Failure<DomainEntities.User>(UserErrors.InvalidCredentials);

        return EnsureAccountCanSignIn(user);
    }

    private static Result<DomainEntities.User> EnsureAccountCanSignIn(DomainEntities.User user) => user.Status switch
    {
        UserStatus.Suspended => Result.Failure<DomainEntities.User>(UserErrors.AccountSuspended),
        UserStatus.Deleted => Result.Failure<DomainEntities.User>(UserErrors.AccountDeleted),
        _ => Result.Success(user)
    };
}
