using FluentValidation;

namespace GymApp.Modules.Training.Application.Commands.CreateProgram;

internal sealed class CreateProgramCommandValidator : AbstractValidator<CreateProgramCommand>
{
    public CreateProgramCommandValidator()
    {
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DurationWeeks).InclusiveBetween(1, 52);
        RuleFor(x => x.Weeks).NotEmpty().WithMessage("At least one week is required.");
        RuleForEach(x => x.Weeks).ChildRules(week =>
        {
            week.RuleFor(w => w.WeekNumber).GreaterThan(0);
            week.RuleForEach(w => w.DayWorkouts!).ChildRules(d =>
            {
                d.RuleFor(x => x.DayOfWeek)
                    .NotEmpty().WithMessage("DayOfWeek is required.")
                    .Must(s => Enum.TryParse<DayOfWeek>(s, ignoreCase: true, out _))
                    .WithMessage("DayOfWeek must be a valid day name (Sunday..Saturday).");
                d.RuleFor(x => x.WorkoutId).NotEmpty();
            }).When(w => w.DayWorkouts != null);
        });
    }
}
