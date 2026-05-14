using GymApp.Common.Domain;

namespace GymApp.Modules.Training.Domain.Events;

public sealed record WorkoutAssignedDomainEvent(Guid AssignmentId, Guid TrainerId, Guid ClientId) : IDomainEvent;
