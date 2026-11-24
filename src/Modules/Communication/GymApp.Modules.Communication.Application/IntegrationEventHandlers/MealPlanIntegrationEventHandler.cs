using GymApp.Common.Application.FeatureFlags;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Nutrition.IntegrationEvents;
using MediatR;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

/// <summary>Tells the member their coach gave them a diet plan; the tap opens it.</summary>
internal sealed class MealPlanIntegrationEventHandler : INotificationHandler<MealPlanAssignedIntegrationEvent>
{
    private readonly IFeatureFlagService _featureFlags;
    private readonly PtNotifier _notifier;

    public MealPlanIntegrationEventHandler(IFeatureFlagService featureFlags, PtNotifier notifier)
    {
        _featureFlags = featureFlags;
        _notifier = notifier;
    }

    public async Task Handle(MealPlanAssignedIntegrationEvent e, CancellationToken cancellationToken)
    {
        if (!await _featureFlags.IsEnabledAsync(e.GymId, FeatureKeys.Nutrition, cancellationToken))
            return;
        var coachName = await _notifier.DisplayNameAsync(e.TrainerId, cancellationToken);
        var data = new Dictionary<string, string>
        {
            ["gymId"] = e.GymId.ToString(),
            ["type"] = "meal_plan_assigned",
            ["route"] = "/more/nutrition/meal-plan",
            ["mealPlanId"] = e.MealPlanId.ToString()
        };
        await _notifier.SendAsync(
            new PtNotification(e.GymId, new[] { e.MemberId }, NotificationType.MealPlan, MealPlanTexts.Assigned(coachName, e.PlanName, e.DailyCalories), data),
            cancellationToken);
    }
}
