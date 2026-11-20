using FluentValidation;
using GymApp.Modules.Scheduling.Application.Commands.LogSessionExercises;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Modules.Scheduling.Application.Commands.UpdateSessionExerciseLog;

internal sealed class UpdateSessionExerciseLogCommandValidator : AbstractValidator<UpdateSessionExerciseLogCommand>
{
    public UpdateSessionExerciseLogCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.LogId).NotEmpty();
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.ExerciseName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ExerciseNameAr).MaximumLength(200);
        RuleFor(x => x.Reps).NotEmpty().When(x => x.SetEntries is not { Count: > 0 });
        RuleFor(x => x.Reps).MaximumLength(50);
        RuleFor(x => x.Sets).GreaterThanOrEqualTo(1).When(x => x.SetEntries is not { Count: > 0 });
        RuleFor(x => x.Measure).IsInEnum();
        RuleFor(x => x.SetEntries)
            .Must(s => s!.Count <= ExerciseSetEntry.MaxSets)
            .WithMessage($"At most {ExerciseSetEntry.MaxSets} sets per exercise.")
            .When(x => x.SetEntries is not null);
        RuleForEach(x => x.SetEntries).SetValidator(x => SetEntryValidators.For(x.Measure));
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Weight)
            .GreaterThanOrEqualTo(0).When(x => x.Weight.HasValue);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Target!).SetValidator(new ExerciseTargetValidator()).When(x => x.Target is not null);
    }
}
