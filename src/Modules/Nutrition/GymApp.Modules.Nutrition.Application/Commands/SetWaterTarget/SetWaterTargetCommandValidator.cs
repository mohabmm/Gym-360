using FluentValidation;

namespace GymApp.Modules.Nutrition.Application.Commands.SetWaterTarget;

internal sealed class SetWaterTargetCommandValidator : AbstractValidator<SetWaterTargetCommand>
{
    public SetWaterTargetCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.TargetMl).GreaterThan(0).WithMessage("Water target must be greater than zero.");
    }
}
