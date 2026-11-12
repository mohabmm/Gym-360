using FluentValidation;

namespace GymApp.Modules.Scheduling.Application.Commands.RespondToSession;

internal sealed class RespondToSessionCommandValidator : AbstractValidator<RespondToSessionCommand>
{
    public RespondToSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
