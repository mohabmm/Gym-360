using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Application.Abstractions;

namespace GymApp.Modules.Identity.Application.Commands.VerifyOtp;

internal sealed class VerifyOtpCommandHandler : ICommandHandler<VerifyOtpCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpCodeRepository _otpCodeRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public VerifyOtpCommandHandler(
        IUserRepository userRepository,
        IOtpCodeRepository otpCodeRepository,
        IIdentityUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _otpCodeRepository = otpCodeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound(request.UserId));

        if (!Enum.TryParse<OtpPurpose>(request.Purpose, true, out var purpose))
            return Result.Failure(new Error("User.InvalidOtpPurpose", "The provided OTP purpose is not valid."));

        var otpCode = await _otpCodeRepository.GetLatestAsync(request.UserId, purpose, cancellationToken);
        if (otpCode is null)
            return Result.Failure(UserErrors.InvalidOtp);

        if (otpCode.IsExpired)
            return Result.Failure(UserErrors.OtpExpired);

        if (otpCode.IsUsed)
            return Result.Failure(UserErrors.InvalidOtp);

        if (otpCode.Code != request.Code)
            return Result.Failure(UserErrors.InvalidOtp);

        otpCode.MarkUsed();

        if (purpose == OtpPurpose.EmailVerification)
        {
            user.Activate();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
