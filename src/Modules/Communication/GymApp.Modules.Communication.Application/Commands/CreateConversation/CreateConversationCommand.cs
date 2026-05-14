using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Communication.Application.Commands.CreateConversation;

public sealed record CreateConversationCommand(
    Guid GymId,
    Guid Participant1,
    Guid Participant2) : ICommand<Guid>;
