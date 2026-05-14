using FluentValidation;

namespace GymApp.Modules.Training.Application.Commands.LogWorkout;

internal sealed class LogWorkoutCommandValidator : AbstractValidator<LogWorkoutCommand>
{
    public LogWorkoutCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.StartedAt).NotEmpty();
        RuleFor(x => x.Sets).NotEmpty().WithMessage("At least one set is required.");
        RuleFor(x => x.PerceivedExertion)
            .InclusiveBetween(1, 10).When(x => x.PerceivedExertion.HasValue)
            .WithMessage("Perceived exertion must be between 1 and 10.");
        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).When(x => x.DurationMinutes.HasValue);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
