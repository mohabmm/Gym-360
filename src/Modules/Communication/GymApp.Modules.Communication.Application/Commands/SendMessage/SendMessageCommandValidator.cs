using FluentValidation;

namespace GymApp.Modules.Communication.Application.Commands.SendMessage;

internal sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.SenderId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.MediaType)
            .Must(mt => mt is null or "None" or "Image" or "Video" or "Document")
            .When(x => x.MediaType is not null)
            .WithMessage("MediaType must be None, Image, Video, or Document.");
    }
}
