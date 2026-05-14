using FluentValidation;

namespace GymApp.Modules.Training.Application.Commands.UpdateWorkout;

internal sealed class UpdateWorkoutCommandValidator : AbstractValidator<UpdateWorkoutCommand>
{
    public UpdateWorkoutCommandValidator()
    {
        RuleFor(x => x.WorkoutId).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.EstimatedDuration)
            .GreaterThan(0).When(x => x.EstimatedDuration.HasValue);
    }
}
