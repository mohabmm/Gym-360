using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.IntegrationEvents;

public sealed record TrainerAddedToGymIntegrationEvent(
    Guid GymId,
    Guid TrainerId) : IDomainEvent;
