using GymApp.Common.Domain;

namespace GymApp.Modules.Scheduling.Domain.Events;

public sealed record SessionNoShowDomainEvent(
    Guid SessionId,
    Guid MemberId,
    Guid GymId) : IDomainEvent;
