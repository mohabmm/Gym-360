using FluentValidation;

namespace GymApp.Modules.Scheduling.Application.Commands.CancelSession;

internal sealed class CancelSessionCommandValidator : AbstractValidator<CancelSessionCommand>
{
    public CancelSessionCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("Session ID is required.");

        RuleFor(x => x.CancelledBy)
            .NotEmpty().WithMessage("Canceller ID is required.");
    }
}
