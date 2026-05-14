using GymApp.Common.Domain;

namespace GymApp.Modules.Scheduling.IntegrationEvents;

public sealed record SessionCompletedIntegrationEvent(
    Guid SessionId,
    Guid TrainerId,
    Guid MemberId,
    Guid GymId) : IDomainEvent;
