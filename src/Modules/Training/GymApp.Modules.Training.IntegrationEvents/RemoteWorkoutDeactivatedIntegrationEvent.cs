using GymApp.Common.Domain;

namespace GymApp.Modules.Training.IntegrationEvents;

public sealed record RemoteWorkoutDeactivatedIntegrationEvent(
    Guid TrainerId,
    Guid ClientId,
    Guid GymId) : IDomainEvent;
