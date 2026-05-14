using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Events;

public sealed record TrainerAddedToGymDomainEvent(Guid GymId, Guid TrainerId) : IDomainEvent;
