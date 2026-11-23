using GymApp.Common.Domain;

namespace GymApp.Modules.Training.Domain.Events;

public sealed record TrainingPlanUpdatedDomainEvent(Guid PlanId, Guid TraineeId, int Version) : IDomainEvent;
