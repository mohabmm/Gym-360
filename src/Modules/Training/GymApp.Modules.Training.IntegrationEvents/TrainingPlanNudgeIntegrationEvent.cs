using GymApp.Common.Domain;

namespace GymApp.Modules.Training.IntegrationEvents;

/// <summary>pt.training_plan: a coach reminds an inactive trainee to get back to their plan.</summary>
/// <param name="Message">The coach's own words; null = the standard reminder.</param>
public sealed record TrainingPlanNudgeIntegrationEvent(Guid GymId, Guid TrainerId, Guid TraineeId, string? Message) : IIntegrationEvent;
