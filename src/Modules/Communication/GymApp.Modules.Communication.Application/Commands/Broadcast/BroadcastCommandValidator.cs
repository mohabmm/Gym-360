using FluentValidation;

namespace GymApp.Modules.Communication.Application.Commands.Broadcast;

internal sealed class BroadcastCommandValidator : AbstractValidator<BroadcastCommand>
{
    public BroadcastCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.SenderId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.RecipientIds).NotEmpty().WithMessage("At least one recipient is required.");
    }
}
