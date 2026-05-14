using FluentValidation;

namespace GymApp.Modules.Training.Application.Commands.ActivateRemoteWorkout;

internal sealed class ActivateRemoteWorkoutCommandValidator : AbstractValidator<ActivateRemoteWorkoutCommand>
{
    public ActivateRemoteWorkoutCommandValidator()
    {
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
