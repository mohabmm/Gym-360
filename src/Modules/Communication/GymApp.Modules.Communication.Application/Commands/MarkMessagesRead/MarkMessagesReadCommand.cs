using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Communication.Application.Commands.MarkMessagesRead;

public sealed record MarkMessagesReadCommand(
    Guid ConversationId,
    Guid UserId) : ICommand;
