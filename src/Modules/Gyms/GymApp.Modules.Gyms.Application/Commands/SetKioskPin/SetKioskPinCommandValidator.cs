using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.SetKioskPin;

internal sealed class SetKioskPinCommandValidator : AbstractValidator<SetKioskPinCommand>
{
    public SetKioskPinCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.NewPin)
            .NotEmpty()
            .Matches(@"^\d{4,8}$")
            .WithMessage("Kiosk PIN must be 4 to 8 digits.");
        RuleFor(x => x.CurrentPin)
            .Matches(@"^\d{4,8}$")
            .When(x => !string.IsNullOrEmpty(x.CurrentPin))
            .WithMessage("Current kiosk PIN must be 4 to 8 digits.");
    }
}
