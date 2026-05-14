using GymApp.Common.Domain;

namespace GymApp.Modules.Scheduling.IntegrationEvents;

public sealed record SessionBookedIntegrationEvent(
    Guid SessionId,
    Guid TrainerId,
    Guid MemberId,
    Guid GymId,
    DateTime ScheduledAt) : IDomainEvent;
