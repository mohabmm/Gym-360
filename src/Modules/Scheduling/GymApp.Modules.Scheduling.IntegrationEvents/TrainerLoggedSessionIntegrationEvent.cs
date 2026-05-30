using GymApp.Common.Domain;

namespace GymApp.Modules.Scheduling.IntegrationEvents;

public sealed record TrainerLoggedSessionIntegrationEvent(
    Guid SessionId,
    Guid TrainerId) : IIntegrationEvent;
