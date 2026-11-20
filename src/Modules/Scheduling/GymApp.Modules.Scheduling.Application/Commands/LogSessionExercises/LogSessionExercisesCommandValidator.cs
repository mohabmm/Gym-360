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
                .When(i => i.SetEntries is not { Count: > 0 });
            item.RuleFor(i => i.Reps).MaximumLength(50);
            item.RuleFor(i => i.Sets)
                .GreaterThanOrEqualTo(1).WithMessage("Sets must be at least 1.")
                .When(i => i.SetEntries is not { Count: > 0 });
            item.RuleFor(i => i.Weight)
                .GreaterThanOrEqualTo(0).When(i => i.Weight.HasValue)
                .WithMessage("Weight cannot be negative.");
            item.RuleFor(i => i.Measure).IsInEnum();
            item.RuleFor(i => i.SetEntries)
                .Must(s => s!.Count <= ExerciseSetEntry.MaxSets)
                .WithMessage($"At most {ExerciseSetEntry.MaxSets} sets per exercise.")
                .When(i => i.SetEntries is not null);
            item.RuleForEach(i => i.SetEntries).SetValidator(i => SetEntryValidators.For(i.Measure));
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
        RuleFor(t => t.Seconds).InclusiveBetween(1, ExerciseSetEntry.MaxSeconds).When(t => t.Seconds.HasValue);
    }
}

/// <summary>pt.exercise_progress: one set of a reps exercise — reps required, kg optional.</summary>
internal sealed class RepsSetEntryValidator : AbstractValidator<ExerciseSetEntry>
{
    public RepsSetEntryValidator()
    {
        RuleFor(s => s.Reps).NotNull().WithMessage("Reps are required for every set.")
            .InclusiveBetween(1, ExerciseSetEntry.MaxReps);
        RuleFor(s => s.Weight).InclusiveBetween(0m, ExerciseSetEntry.MaxWeightKg).When(s => s.Weight.HasValue);
    }
}

/// <summary>pt.exercise_progress: one set of a timed exercise (plank) — seconds required, kg optional.</summary>
internal sealed class TimedSetEntryValidator : AbstractValidator<ExerciseSetEntry>
{
    public TimedSetEntryValidator()
    {
        RuleFor(s => s.Seconds).NotNull().WithMessage("Seconds are required for every set.")
            .InclusiveBetween(1, ExerciseSetEntry.MaxSeconds);
        RuleFor(s => s.Weight).InclusiveBetween(0m, ExerciseSetEntry.MaxWeightKg).When(s => s.Weight.HasValue);
    }
}

internal static class SetEntryValidators
{
    private static readonly RepsSetEntryValidator Reps = new();
    private static readonly TimedSetEntryValidator Timed = new();

    public static IValidator<ExerciseSetEntry> For(ExerciseMeasure measure) =>
        measure == ExerciseMeasure.Time ? Timed : Reps;
}
