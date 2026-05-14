using FluentValidation;

namespace GymApp.Modules.Scheduling.Application.Commands.SetAvailability;

internal sealed class SetAvailabilityCommandValidator : AbstractValidator<SetAvailabilityCommand>
{
    public SetAvailabilityCommandValidator()
    {
        RuleFor(x => x.TrainerId)
            .NotEmpty().WithMessage("Trainer ID is required.");

        RuleFor(x => x.GymId)
            .NotEmpty().WithMessage("Gym ID is required.");

        RuleFor(x => x.SessionDurationMinutes)
            .InclusiveBetween(15, 180).WithMessage("Session duration must be between 15 and 180 minutes.");

        RuleFor(x => x.BufferMinutes)
            .InclusiveBetween(0, 60).WithMessage("Buffer must be between 0 and 60 minutes.");

        RuleFor(x => x.WeeklySlots)
            .NotEmpty().WithMessage("At least one availability slot is required.");

        RuleForEach(x => x.WeeklySlots).ChildRules(slot =>
        {
            slot.RuleFor(s => s.StartTime)
                .LessThan(s => s.EndTime).WithMessage("Start time must be before end time.");
        });
    }
}
