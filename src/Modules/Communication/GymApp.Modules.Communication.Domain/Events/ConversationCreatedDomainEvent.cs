using GymApp.Common.Domain;

namespace GymApp.Modules.Communication.Domain.Events;

public sealed record ConversationCreatedDomainEvent(
    Guid ConversationId,
    Guid GymId) : IDomainEvent;
