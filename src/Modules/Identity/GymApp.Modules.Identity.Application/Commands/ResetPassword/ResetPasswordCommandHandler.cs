using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Application.Contracts;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Domain.ValueObjects;

namespace GymApp.Modules.Identity.Application.Commands.ResetPassword;

internal sealed class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpCodeRepository _otpCodeRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordCommandHandler(
        IUserRepository userRepository,
        IOtpCodeRepository otpCodeRepository,
        IIdentityUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _otpCodeRepository = otpCodeRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result.Failure(UserErrors.InvalidCredentials);

        var user = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.InvalidCredentials);

        var otpCode = await _otpCodeRepository.GetLatestAsync(user.Id, OtpPurpose.PasswordReset, cancellationToken);
        if (otpCode is null)
            return Result.Failure(UserErrors.InvalidOtp);

        if (otpCode.IsExpired)
            return Result.Failure(UserErrors.OtpExpired);

        if (otpCode.IsUsed)
            return Result.Failure(UserErrors.InvalidOtp);

        if (otpCode.Code != request.Code)
            return Result.Failure(UserErrors.InvalidOtp);

        otpCode.MarkUsed();

        var passwordHash = _passwordHasher.Hash(request.NewPassword);
        user.UpdatePasswordHash(passwordHash);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
