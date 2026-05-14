using FluentValidation;

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
        RuleFor(x => x.Reps).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Sets).GreaterThanOrEqualTo(1);
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Weight)
            .GreaterThanOrEqualTo(0).When(x => x.Weight.HasValue);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
