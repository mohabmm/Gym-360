using GymApp.Common.Domain;

namespace GymApp.Modules.Scheduling.IntegrationEvents;

public sealed record SessionLateCancelledIntegrationEvent(
    Guid SessionId,
    Guid TrainerId,
    Guid MemberId,
    Guid GymId,
    Guid CancelledBy) : IDomainEvent;
