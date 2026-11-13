using GymApp.Common.Application.FeatureFlags;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Gyms.Shared;
using GymApp.Modules.Scheduling.IntegrationEvents;
using MediatR;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

/// <summary>pt.dual_session_logging — the trainee said "not me": alert the desk and the coach.</summary>
internal sealed class SessionDisputedIntegrationEventHandler : INotificationHandler<SessionDisputedIntegrationEvent>
{
    private readonly IFeatureFlagService _featureFlags;
    private readonly IGymDeskLookup _desk;
    private readonly PtNotifier _notifier;

    public SessionDisputedIntegrationEventHandler(IFeatureFlagService featureFlags, IGymDeskLookup desk, PtNotifier notifier)
    {
        _featureFlags = featureFlags;
        _desk = desk;
        _notifier = notifier;
    }

    public async Task Handle(SessionDisputedIntegrationEvent e, CancellationToken cancellationToken)
    {
        if (!await _featureFlags.IsEnabledAsync(e.GymId, FeatureKeys.PtDualSessionLogging, cancellationToken))
            return;

        var desk = await _desk.GetDeskUserIdsAsync(e.GymId, cancellationToken);
        var memberName = await _notifier.DisplayNameAsync(e.MemberId, cancellationToken);
        var data = new Dictionary<string, string>
        {
            ["type"] = "session_disputed",
            ["sessionId"] = e.SessionId.ToString(),
            ["memberId"] = e.MemberId.ToString(),
            ["gymId"] = e.GymId.ToString(),
        };

        await _notifier.SendAsync(
            new PtNotification(e.GymId, desk.Append(e.TrainerId).ToList(), NotificationType.SessionDisputed,
                PtNotificationTexts.SessionDisputed(memberName), data),
            cancellationToken);
    }
}
