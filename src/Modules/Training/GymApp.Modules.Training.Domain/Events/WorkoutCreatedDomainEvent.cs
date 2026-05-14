using GymApp.Common.Domain;

namespace GymApp.Modules.Training.Domain.Events;

public sealed record WorkoutCreatedDomainEvent(Guid WorkoutId, Guid TrainerId) : IDomainEvent;
