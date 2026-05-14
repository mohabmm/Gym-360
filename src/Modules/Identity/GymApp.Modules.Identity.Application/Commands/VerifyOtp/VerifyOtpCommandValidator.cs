using FluentValidation;

namespace GymApp.Modules.Identity.Application.Commands.VerifyOtp;

internal sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("OTP code is required.")
            .Length(6).WithMessage("OTP code must be 6 digits.");

        RuleFor(x => x.Purpose)
            .NotEmpty().WithMessage("Purpose is required.");
    }
}
