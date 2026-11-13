using GymApp.Common.Application.FeatureFlags;
using GymApp.Modules.Billing.IntegrationEvents;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Domain.Enums;
using MediatR;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

/// <summary>pt.trial_recap — tells the trainee their coach's plan is ready.</summary>
internal sealed class TrialRecapSentIntegrationEventHandler : INotificationHandler<TrialRecapSentIntegrationEvent>
{
    private readonly IFeatureFlagService _featureFlags;
    private readonly PtNotifier _notifier;

    public TrialRecapSentIntegrationEventHandler(IFeatureFlagService featureFlags, PtNotifier notifier)
    {
        _featureFlags = featureFlags;
        _notifier = notifier;
    }

    public async Task Handle(TrialRecapSentIntegrationEvent e, CancellationToken cancellationToken)
    {
        if (!await _featureFlags.IsEnabledAsync(e.GymId, FeatureKeys.PtTrialRecap, cancellationToken))
            return;

        var coachName = await _notifier.DisplayNameAsync(e.TrainerId, cancellationToken);
        var data = new Dictionary<string, string>
        {
            ["type"] = "trial_recap",
            ["packageId"] = e.PackageId.ToString(),
            ["trainerId"] = e.TrainerId.ToString(),
            ["gymId"] = e.GymId.ToString(),
        };

        await _notifier.SendAsync(
            new PtNotification(e.GymId, new[] { e.MemberId }, NotificationType.TrialRecap,
                PtConversionTexts.RecapForMember(coachName), data),
            cancellationToken);
    }
}
