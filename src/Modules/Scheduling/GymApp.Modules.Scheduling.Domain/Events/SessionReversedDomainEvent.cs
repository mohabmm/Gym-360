using GymApp.Common.Domain;

namespace GymApp.Modules.Scheduling.Domain.Events;

public sealed record SessionReversedDomainEvent(Guid SessionId, Guid TrainerId, Guid MemberId, Guid GymId) : IDomainEvent;
