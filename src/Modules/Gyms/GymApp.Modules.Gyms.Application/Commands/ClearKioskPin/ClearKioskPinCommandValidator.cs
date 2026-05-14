using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.ClearKioskPin;

internal sealed class ClearKioskPinCommandValidator : AbstractValidator<ClearKioskPinCommand>
{
    public ClearKioskPinCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.CurrentPin)
            .NotEmpty()
            .Matches(@"^\d{4,8}$")
            .WithMessage("Current kiosk PIN must be 4 to 8 digits.");
    }
}
