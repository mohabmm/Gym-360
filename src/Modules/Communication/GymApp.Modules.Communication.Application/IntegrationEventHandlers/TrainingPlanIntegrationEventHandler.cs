using GymApp.Common.Application.FeatureFlags;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.ValueObjects;
using GymApp.Modules.Training.IntegrationEvents;
using MediatR;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

/// <summary>pt.training_plan — tells the trainee when their coach gives them a plan or edits it.</summary>
internal sealed class TrainingPlanIntegrationEventHandler
    : INotificationHandler<TrainingPlanAssignedIntegrationEvent>,
      INotificationHandler<TrainingPlanUpdatedIntegrationEvent>
{
    private readonly IFeatureFlagService _featureFlags;
    private readonly PtNotifier _notifier;

    public TrainingPlanIntegrationEventHandler(IFeatureFlagService featureFlags, PtNotifier notifier)
    {
        _featureFlags = featureFlags;
        _notifier = notifier;
    }

    public Task Handle(TrainingPlanAssignedIntegrationEvent e, CancellationToken cancellationToken) =>
        NotifyAsync(new PlanNotice(e.GymId, e.TrainerId, e.TraineeId, e.PlanId, "training_plan_assigned"),
            coach => TrainingPlanTexts.Assigned(coach, e.PlanNameAr ?? e.PlanName), cancellationToken);

    public Task Handle(TrainingPlanUpdatedIntegrationEvent e, CancellationToken cancellationToken) =>
        NotifyAsync(new PlanNotice(e.GymId, e.TrainerId, e.TraineeId, e.PlanId, "training_plan_updated"),
            coach => TrainingPlanTexts.Updated(coach, e.PlanNameAr ?? e.PlanName), cancellationToken);

    private sealed record PlanNotice(Guid GymId, Guid TrainerId, Guid TraineeId, Guid PlanId, string Type);

    private async Task NotifyAsync(PlanNotice notice, Func<string, NotificationText> text, CancellationToken cancellationToken)
    {
        if (!await _featureFlags.IsEnabledAsync(notice.GymId, FeatureKeys.PtTrainingPlan, cancellationToken))
            return;
        var coachName = await _notifier.DisplayNameAsync(notice.TrainerId, cancellationToken);
        var data = new Dictionary<string, string>
        {
            ["planId"] = notice.PlanId.ToString(),
            ["gymId"] = notice.GymId.ToString(),
            ["type"] = notice.Type,
            ["route"] = "/plan"
        };
        await _notifier.SendAsync(
            new PtNotification(notice.GymId, new[] { notice.TraineeId }, NotificationType.TrainingPlan, text(coachName), data),
            cancellationToken);
    }
}
