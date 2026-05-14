using FluentValidation;

namespace GymApp.Modules.Scheduling.Application.Commands.MarkNoShow;

internal sealed class MarkNoShowCommandValidator : AbstractValidator<MarkNoShowCommand>
{
    public MarkNoShowCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("Session ID is required.");
    }
}
