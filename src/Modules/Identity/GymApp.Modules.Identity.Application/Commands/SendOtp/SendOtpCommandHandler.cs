using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Application.Contracts;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;

namespace GymApp.Modules.Identity.Application.Commands.SendOtp;

internal sealed class SendOtpCommandHandler : ICommandHandler<SendOtpCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpCodeRepository _otpCodeRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IOtpSender _otpSender;

    private const int MaxOtpRequestsPerHour = 5;
    private static readonly TimeSpan OtpValidity = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromHours(1);

    public SendOtpCommandHandler(
        IUserRepository userRepository,
        IOtpCodeRepository otpCodeRepository,
        IIdentityUnitOfWork unitOfWork,
        IOtpSender otpSender)
    {
        _userRepository = userRepository;
        _otpCodeRepository = otpCodeRepository;
        _unitOfWork = unitOfWork;
        _otpSender = otpSender;
    }

    public async Task<Result> Handle(SendOtpCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound(request.UserId));

        if (!Enum.TryParse<OtpPurpose>(request.Purpose, true, out var purpose))
            return Result.Failure(new Error("User.InvalidOtpPurpose", "The provided OTP purpose is not valid."));

        var recentCount = await _otpCodeRepository.GetCountInWindowAsync(
            request.UserId, purpose, RateLimitWindow, cancellationToken);

        if (recentCount >= MaxOtpRequestsPerHour)
            return Result.Failure(UserErrors.OtpRateLimitExceeded);

        var code = GenerateOtpCode();
        var otpCode = OtpCode.Create(request.UserId, code, purpose, OtpValidity);

        await _otpCodeRepository.AddAsync(otpCode, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _otpSender.SendAsync(user.Email.Value, code, request.Purpose, cancellationToken);

        return Result.Success();
    }

    private static string GenerateOtpCode()
    {
        return Random.Shared.Next(100000, 999999).ToString();
    }
}
