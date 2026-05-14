using FluentValidation;

namespace GymApp.Modules.Scheduling.Application.Commands.LogMemberSession;

internal sealed class LogMemberSessionCommandValidator : AbstractValidator<LogMemberSessionCommand>
{
    public LogMemberSessionCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("Session ID is required.");

        RuleFor(x => x.MemberId)
            .NotEmpty().WithMessage("Member ID is required.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.PerceivedExertion)
            .InclusiveBetween(1, 10).WithMessage("Perceived exertion (RPE) must be between 1 and 10.");

        RuleFor(x => x.Satisfaction)
            .InclusiveBetween(1, 5).WithMessage("Satisfaction must be between 1 and 5.");

        RuleFor(x => x.PersonalNotes)
            .MaximumLength(1000).WithMessage("Personal notes cannot exceed 1000 characters.");

        RuleFor(x => x.Discomfort)
            .MaximumLength(500).WithMessage("Discomfort description cannot exceed 500 characters.");
    }
}
