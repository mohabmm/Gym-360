using GymApp.Common.Domain;

namespace GymApp.Modules.Training.IntegrationEvents;

public sealed record RemoteWorkoutActivatedIntegrationEvent(
    Guid TrainerId,
    Guid ClientId,
    Guid GymId,
    string? Reason) : IIntegrationEvent;
