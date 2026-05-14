using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Events;

public sealed record TrainerRejectedDomainEvent(Guid GymId, Guid TrainerId) : IDomainEvent;
