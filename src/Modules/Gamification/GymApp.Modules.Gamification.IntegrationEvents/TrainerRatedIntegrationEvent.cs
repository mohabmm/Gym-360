using GymApp.Common.Domain;

namespace GymApp.Modules.Gamification.IntegrationEvents;

public sealed record TrainerRatedIntegrationEvent(
    Guid TrainerId,
    Guid GymId,
    int Stars) : IIntegrationEvent;
