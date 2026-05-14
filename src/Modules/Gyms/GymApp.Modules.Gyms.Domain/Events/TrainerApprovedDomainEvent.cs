using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Events;

public sealed record TrainerApprovedDomainEvent(Guid GymId, Guid TrainerId) : IDomainEvent;
