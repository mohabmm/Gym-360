using FluentValidation;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Modules.Scheduling.Application.Commands.LogSessionExercises;

internal sealed class LogSessionExercisesCommandValidator : AbstractValidator<LogSessionExercisesCommand>
{
    public LogSessionExercisesCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.DayLabel).MaximumLength(Session.MaxDayLabelLength);
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
            item.RuleFor(i => i.Target!).SetValidator(new ExerciseTargetValidator()).When(i => i.Target is not null);
            item.RuleFor(i => i.OrderIndex)
                .GreaterThanOrEqualTo(0);
            item.RuleFor(i => i.Notes).MaximumLength(1000);
        });
    }
}

/// <summary>pt.exercise_progress: the coach's optional next-session target.</summary>
internal sealed class ExerciseTargetValidator : AbstractValidator<ExerciseTarget>
{
    private const decimal MaxWeightKg = 500m;

    public ExerciseTargetValidator()
    {
        RuleFor(t => t.Sets).InclusiveBetween(1, 50).When(t => t.Sets.HasValue);
        RuleFor(t => t.Reps).MaximumLength(50);
        RuleFor(t => t.Weight).InclusiveBetween(0m, MaxWeightKg).When(t => t.Weight.HasValue);
    }
}
