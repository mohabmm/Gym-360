using GymApp.Common.Domain;

namespace GymApp.Modules.Nutrition.IntegrationEvents;

/// <summary>A coach gave a member a diet plan (the member is notified).</summary>
public sealed record MealPlanAssignedIntegrationEvent(
    Guid MealPlanId,
    Guid GymId,
    Guid TrainerId,
    Guid MemberId,
    string PlanName,
    int DailyCalories) : IIntegrationEvent;
