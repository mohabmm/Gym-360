using GymApp.Common.Domain;

namespace GymApp.Modules.Communication.Domain.Events;

public sealed record NotificationCreatedDomainEvent(
    Guid NotificationId,
    Guid UserId,
    string Type) : IDomainEvent;
