using FluentValidation;

namespace GymApp.Modules.Training.Application.Commands.AssignWorkout;

internal sealed class AssignWorkoutCommandValidator : AbstractValidator<AssignWorkoutCommand>
{
    public AssignWorkoutCommandValidator()
    {
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.WorkoutId).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty().GreaterThanOrEqualTo(DateTime.UtcNow.Date);
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date must be after start date.");
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
