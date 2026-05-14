using FluentValidation;

namespace GymApp.Modules.Scheduling.Application.Commands.LogSessionExercises;

internal sealed class LogSessionExercisesCommandValidator : AbstractValidator<LogSessionExercisesCommand>
{
    public LogSessionExercisesCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.Items)
            .NotNull()
            .Must(i => i.Count > 0).WithMessage("At least one exercise log item is required.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ExerciseName)
                .NotEmpty().WithMessage("Exercise name is required.")
                .MaximumLength(200);
            item.RuleFor(i => i.ExerciseNameAr).MaximumLength(200);
            item.RuleFor(i => i.Reps)
                .NotEmpty().WithMessage("Reps is required.")
                .MaximumLength(50);
            item.RuleFor(i => i.Sets)
                .GreaterThanOrEqualTo(1).WithMessage("Sets must be at least 1.");
            item.RuleFor(i => i.Weight)
                .GreaterThanOrEqualTo(0).When(i => i.Weight.HasValue)
                .WithMessage("Weight cannot be negative.");
            item.RuleFor(i => i.OrderIndex)
                .GreaterThanOrEqualTo(0);
            item.RuleFor(i => i.Notes).MaximumLength(1000);
        });
    }
}
