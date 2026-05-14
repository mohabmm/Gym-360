using FluentValidation;

namespace GymApp.Modules.Scheduling.Application.Commands.StartSession;

internal sealed class StartSessionCommandValidator : AbstractValidator<StartSessionCommand>
{
    public StartSessionCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("Session ID is required.");
    }
}
