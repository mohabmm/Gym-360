using GymApp.Common.Domain;

namespace GymApp.Modules.Training.Domain.Events;

public sealed record TrainingPlanAssignedDomainEvent(Guid PlanId, Guid TrainerId, Guid TraineeId) : IDomainEvent;
