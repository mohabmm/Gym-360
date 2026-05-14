using FluentValidation;

namespace GymApp.Modules.Scheduling.Application.Commands.ConfirmSession;

internal sealed class ConfirmSessionCommandValidator : AbstractValidator<ConfirmSessionCommand>
{
    public ConfirmSessionCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("Session ID is required.");
    }
}
