using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.SetTrainerRate;

internal sealed class SetTrainerRateCommandValidator : AbstractValidator<SetTrainerRateCommand>
{
    public SetTrainerRateCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.SetByUserId).NotEmpty();
        RuleFor(x => x.RatePerSession)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(10000);
    }
}
