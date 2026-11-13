using FluentValidation;
using GymApp.Modules.Progress.Domain.Enums;

namespace GymApp.Modules.Progress.Application.Commands.SetProgressGoal;

internal sealed class SetProgressGoalCommandValidator : AbstractValidator<SetProgressGoalCommand>
{
    public SetProgressGoalCommandValidator()
    {
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.Goal)
            .Must(g => Enum.TryParse<ProgressGoalType>(g, true, out var parsed) && Enum.IsDefined(parsed))
            .WithMessage("Goal must be LoseFat, BuildMuscle or GeneralFitness.");
        RuleFor(x => x.TargetWeight)
            .GreaterThan(0)
            .LessThanOrEqualTo(500)
            .When(x => x.TargetWeight.HasValue);
    }
}
