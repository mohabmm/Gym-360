using FluentValidation;

namespace GymApp.Modules.Communication.Application.Commands.CreateConversation;

internal sealed class CreateConversationCommandValidator : AbstractValidator<CreateConversationCommand>
{
    public CreateConversationCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.Participant1).NotEmpty();
        RuleFor(x => x.Participant2).NotEmpty();
        RuleFor(x => x.Participant1)
            .NotEqual(x => x.Participant2)
            .WithMessage("A conversation cannot be created between the same user.");
    }
}
