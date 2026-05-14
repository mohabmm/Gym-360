using GymApp.Common.Domain;

namespace GymApp.Modules.Scheduling.Domain.Events;

public sealed record SessionCancelledDomainEvent(
    Guid SessionId,
    Guid CancelledBy,
    bool IsLateCancellation) : IDomainEvent;
