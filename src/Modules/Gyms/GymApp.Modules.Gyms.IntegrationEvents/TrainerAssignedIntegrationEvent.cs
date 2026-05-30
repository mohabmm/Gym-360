using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.IntegrationEvents;

public sealed record TrainerAssignedIntegrationEvent(Guid GymId, Guid TrainerId) : IIntegrationEvent;
