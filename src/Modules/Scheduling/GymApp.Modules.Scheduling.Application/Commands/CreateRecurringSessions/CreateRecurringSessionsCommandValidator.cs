using FluentValidation;

namespace GymApp.Modules.Scheduling.Application.Commands.CreateRecurringSessions;

internal sealed class CreateRecurringSessionsCommandValidator : AbstractValidator<CreateRecurringSessionsCommand>
{
    public CreateRecurringSessionsCommandValidator()
    {
        RuleFor(x => x.GymId)
            .NotEmpty().WithMessage("Gym ID is required.");

        RuleFor(x => x.TrainerId)
            .NotEmpty().WithMessage("Trainer ID is required.");

        RuleFor(x => x.MemberId)
            .NotEmpty().WithMessage("Member ID is required.");

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(15, 180).WithMessage("Duration must be between 15 and 180 minutes.");

        RuleFor(x => x.SessionType)
            .NotEmpty().WithMessage("Session type is required.");

        RuleFor(x => x.WeeksAhead)
            .InclusiveBetween(1, 12).WithMessage("Weeks ahead must be between 1 and 12.");
    }
}
