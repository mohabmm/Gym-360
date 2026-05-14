using GymApp.Common.Domain;

namespace GymApp.Modules.Communication.Domain.Events;

public sealed record MessageSentDomainEvent(
    Guid ConversationId,
    Guid SenderId,
    Guid ReceiverId) : IDomainEvent;
