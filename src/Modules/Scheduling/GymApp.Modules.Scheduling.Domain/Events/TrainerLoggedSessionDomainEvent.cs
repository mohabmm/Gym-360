using GymApp.Common.Domain;

namespace GymApp.Modules.Scheduling.Domain.Events;

public sealed record TrainerLoggedSessionDomainEvent(
    Guid SessionId,
    Guid TrainerId) : IDomainEvent;
