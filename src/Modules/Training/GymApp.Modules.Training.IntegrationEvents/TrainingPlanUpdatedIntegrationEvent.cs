using GymApp.Common.Domain;

namespace GymApp.Modules.Training.IntegrationEvents;

/// <summary>pt.training_plan: the coach edited the trainee's active plan (the trainee is notified).</summary>
public sealed record TrainingPlanUpdatedIntegrationEvent(
    Guid PlanId,
    Guid GymId,
    Guid TrainerId,
    Guid TraineeId,
    int Version,
    string PlanName,
    string? PlanNameAr) : IIntegrationEvent;
