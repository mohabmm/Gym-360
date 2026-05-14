using GymApp.Common.Domain;

namespace GymApp.Modules.Scheduling.Domain.Events;

public sealed record SessionBookedDomainEvent(
    Guid SessionId,
    Guid TrainerId,
    Guid MemberId,
    Guid GymId) : IDomainEvent;
