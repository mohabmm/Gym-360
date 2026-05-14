using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Communication.Application.Commands.SendMessage;

public sealed record SendMessageCommand(
    Guid ConversationId,
    Guid SenderId,
    string Content,
    string? MediaUrl,
    string? MediaType) : ICommand<Guid>;
