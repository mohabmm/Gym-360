using FluentValidation;

namespace GymApp.Modules.Training.Application.Commands.CreateWorkout;

internal sealed class CreateWorkoutCommandValidator : AbstractValidator<CreateWorkoutCommand>
{
    public CreateWorkoutCommandValidator()
    {
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Exercises).NotEmpty().WithMessage("At least one exercise is required.");
        RuleFor(x => x.EstimatedDuration)
            .GreaterThan(0).When(x => x.EstimatedDuration.HasValue)
            .WithMessage("Estimated duration must be greater than 0.");

        RuleForEach(x => x.Exercises).ChildRules(exercise =>
        {
            exercise.RuleFor(e => e.Sets)
                .GreaterThan(0).WithMessage("Sets must be greater than 0.");

            // Reps is intentionally a string to support rep schemes that aren't a single
            // integer: fixed counts ("10"), ranges ("8-12"), AMRAP ("AMRAP"), and
            // time-based prescriptions ("30s"). Validation guards length + shape.
            exercise.RuleFor(e => e.Reps)
                .NotEmpty().WithMessage("Reps value is required.")
                .MaximumLength(50);
        });
    }
}
