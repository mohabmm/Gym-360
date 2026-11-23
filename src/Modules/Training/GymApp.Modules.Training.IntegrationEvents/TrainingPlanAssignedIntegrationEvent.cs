using GymApp.Common.Domain;

namespace GymApp.Modules.Training.IntegrationEvents;

/// <summary>pt.training_plan: a coach gave a trainee a new plan (the trainee is notified).</summary>
public sealed record TrainingPlanAssignedIntegrationEvent(
    Guid PlanId,
    Guid GymId,
    Guid TrainerId,
    Guid TraineeId,
    string PlanName,
    string? PlanNameAr) : IIntegrationEvent;
