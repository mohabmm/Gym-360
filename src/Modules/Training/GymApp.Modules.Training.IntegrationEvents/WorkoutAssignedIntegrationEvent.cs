using GymApp.Common.Domain;

namespace GymApp.Modules.Training.IntegrationEvents;

public sealed record WorkoutAssignedIntegrationEvent(
    Guid TrainerId,
    Guid ClientId,
    Guid GymId,
    string WorkoutName) : IDomainEvent;
