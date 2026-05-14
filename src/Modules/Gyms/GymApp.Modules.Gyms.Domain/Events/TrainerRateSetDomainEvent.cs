using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Events;

public sealed record TrainerRateSetDomainEvent(
    Guid GymId,
    Guid TrainerId,
    decimal RatePerSession) : IDomainEvent;
