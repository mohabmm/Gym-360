using FluentValidation;

namespace GymApp.Modules.Scheduling.Application.Commands.QuickLogSession;

internal sealed class QuickLogSessionCommandValidator : AbstractValidator<QuickLogSessionCommand>
{
    public QuickLogSessionCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.DurationMinutes!.Value)
            .InclusiveBetween(QuickLogSchedule.MinDurationMinutes, QuickLogSchedule.MaxDurationMinutes)
            .When(x => x.DurationMinutes.HasValue)
            .OverridePropertyName(nameof(QuickLogSessionCommand.DurationMinutes));
        RuleFor(x => x.StartTime)
            .NotNull()
            .When(x => x.Date.HasValue)
            .WithMessage("Pick the start time of the session.");
        RuleFor(x => x.PlanDay!.PlanId).NotEmpty().When(x => x.PlanDay is not null)
            .OverridePropertyName("PlanDay.PlanId");
        RuleFor(x => x.PlanDay!.DayKey).NotEmpty().MaximumLength(50).When(x => x.PlanDay is not null)
            .OverridePropertyName("PlanDay.DayKey");
    }
}
