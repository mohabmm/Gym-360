using FluentValidation;

namespace GymApp.Modules.Identity.Application.Commands.SendOtp;

internal sealed class SendOtpCommandValidator : AbstractValidator<SendOtpCommand>
{
    public SendOtpCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Purpose)
            .NotEmpty().WithMessage("Purpose is required.");
    }
}
