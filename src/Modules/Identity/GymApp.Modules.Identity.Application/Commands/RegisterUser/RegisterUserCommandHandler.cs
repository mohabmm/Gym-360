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

namespace GymApp.Modules.Identity.Application.Commands.RegisterUser;

internal sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, TokenResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserConsentRepository _userConsentRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IUserConsentRepository userConsentRepository,
        IIdentityUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _userConsentRepository = userConsentRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<TokenResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result.Failure<TokenResponse>(emailResult.Error);

        if (await _userRepository.ExistsAsync(emailResult.Value, cancellationToken))
            return Result.Failure<TokenResponse>(UserErrors.EmailAlreadyExists);

        var phoneResult = PhoneNumber.Create(NormalizePhone(request.Phone, request.Email));
        if (phoneResult.IsFailure)
            return Result.Failure<TokenResponse>(phoneResult.Error);

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            return Result.Failure<TokenResponse>(new Error("User.InvalidRole", "The provided role is not valid."));

        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = DomainEntities.User.Create(
            new NewUserDetails
            {
                Email = emailResult.Value,
                PhoneNumber = phoneResult.Value,
                PasswordHash = passwordHash,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Gender = Gender.Male // Default, updated via profile
            },
            role);

        user.Activate();

        await _userRepository.AddAsync(user, cancellationToken);

        // Record PDPL consent from registration
        if (request.DataProcessingConsent)
        {
            var dataConsent = DomainEntities.UserConsent.Create(
                user.Id,
                "data_processing",
                "Consent granted during registration",
                "registration");
            await _userConsentRepository.AddAsync(dataConsent, cancellationToken);
        }

        if (request.MarketingConsent)
        {
            var marketingConsent = DomainEntities.UserConsent.Create(
                user.Id,
                "marketing",
                "Marketing consent granted during registration",
                "registration");
            await _userConsentRepository.AddAsync(marketingConsent, cancellationToken);
        }

        var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(user, cancellationToken);
        var refreshTokenValue = await _jwtTokenService.GenerateRefreshTokenAsync(cancellationToken);
        var refreshTokenExpiry = _jwtTokenService.GetRefreshTokenExpiryTime();

        var refreshToken = DomainEntities.RefreshToken.Create(refreshTokenValue, refreshTokenExpiry);
        user.AddRefreshToken(refreshToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var userResponse = new UserResponse(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.Role.ToRoleClaim());

        return Result.Success(new TokenResponse(
            accessToken,
            refreshTokenValue,
            _jwtTokenService.GetAccessTokenExpiryTime(),
            userResponse));
    }

    // Normalizes user-entered phone numbers to E.164 so the PhoneNumber VO accepts them.
    // Egypt PDPL/OTP flow assumes phone is provided eventually, but registration leaves it
    // optional — when empty we synthesize a deterministic placeholder so the NOT NULL
    // column is satisfied. Users replace the placeholder via profile update.
    private static string NormalizePhone(string? raw, string email)
    {
        var trimmed = raw?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(trimmed))
        {
            // Deterministic placeholder derived from the email so retries don't collide
            // and the same user always gets the same synthetic number.
            var hash = Math.Abs(email.GetHashCode()).ToString().PadLeft(10, '0');
            return "+999" + hash[..Math.Min(10, hash.Length)];
        }

        // Strip spaces, dashes, parentheses.
        var digits = new string(trimmed.Where(c => char.IsDigit(c) || c == '+').ToArray());

        // Already E.164.
        if (digits.StartsWith("+")) return digits;

        // "00" international prefix -> "+"
        if (digits.StartsWith("00")) return "+" + digits[2..];

        // Egyptian local: 11 digits starting "01" (mobile) -> "+20" + last 10 digits.
        if (digits.Length == 11 && digits.StartsWith("01")) return "+20" + digits[1..];

        // 10 digits starting "1" (mobile minus the leading 0) -> "+20" + digits.
        if (digits.Length == 10 && digits.StartsWith("1")) return "+20" + digits;

        // Fallback: prepend + and let the VO validate.
        return "+" + digits;
    }
}
