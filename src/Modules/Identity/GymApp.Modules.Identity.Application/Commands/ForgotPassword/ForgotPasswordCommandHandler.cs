using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Application.Contracts;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Domain.ValueObjects;

namespace GymApp.Modules.Identity.Application.Commands.ForgotPassword;

internal sealed class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpCodeRepository _otpCodeRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IOtpSender _otpSender;

    private static readonly TimeSpan OtpValidity = TimeSpan.FromMinutes(15);

    public ForgotPasswordCommandHandler(
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

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result.Success(); // Don't reveal if email exists

        var user = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
        if (user is null)
            return Result.Success(); // Don't reveal if email exists

        var code = Random.Shared.Next(100000, 999999).ToString();
        var otpCode = OtpCode.Create(user.Id, code, OtpPurpose.PasswordReset, OtpValidity);

        await _otpCodeRepository.AddAsync(otpCode, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _otpSender.SendAsync(user.Email.Value, code, "PasswordReset", cancellationToken);

        return Result.Success();
    }
}
