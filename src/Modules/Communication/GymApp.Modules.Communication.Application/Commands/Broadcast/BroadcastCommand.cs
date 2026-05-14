using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Communication.Application.Commands.Broadcast;

public sealed record BroadcastCommand(
    Guid GymId,
    Guid SenderId,
    string Content,
    List<Guid> RecipientIds) : ICommand;
