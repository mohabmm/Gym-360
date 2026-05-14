using FluentValidation;

namespace GymApp.Modules.Training.Application.Commands.DeactivateRemoteWorkout;

internal sealed class DeactivateRemoteWorkoutCommandValidator : AbstractValidator<DeactivateRemoteWorkoutCommand>
{
    public DeactivateRemoteWorkoutCommandValidator()
    {
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.GymId).NotEmpty();
    }
}
