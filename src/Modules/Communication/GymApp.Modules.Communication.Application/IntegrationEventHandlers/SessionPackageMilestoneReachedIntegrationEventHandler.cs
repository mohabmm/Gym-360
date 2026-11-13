using GymApp.Common.Application.FeatureFlags;
using GymApp.Modules.Billing.IntegrationEvents;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Gyms.Shared;
using MediatR;
using Milestones = GymApp.Modules.Billing.IntegrationEvents.SessionPackageMilestoneReachedIntegrationEvent.Milestones;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

/// <summary>
/// pt.renewal_reminders — at exactly 3 and exactly 1 sessions left: trainee + coach (desk too at 1).
/// pt.trial_end_alerts — last free session and trial finished: coach + desk.
/// Each is skipped for gyms where its flag is OFF.
/// </summary>
internal sealed class SessionPackageMilestoneReachedIntegrationEventHandler
    : INotificationHandler<SessionPackageMilestoneReachedIntegrationEvent>
{
    private readonly IFeatureFlagService _featureFlags;
    private readonly IGymDeskLookup _desk;
    private readonly PtNotifier _notifier;

    public SessionPackageMilestoneReachedIntegrationEventHandler(
        IFeatureFlagService featureFlags,
        IGymDeskLookup desk,
        PtNotifier notifier)
    {
        _featureFlags = featureFlags;
        _desk = desk;
        _notifier = notifier;
    }

    public async Task Handle(SessionPackageMilestoneReachedIntegrationEvent e, CancellationToken cancellationToken)
    {
        var isTrialMilestone = e.Milestone is Milestones.TrialLastSession or Milestones.TrialFinished;
        var flag = isTrialMilestone ? FeatureKeys.PtTrialEndAlerts : FeatureKeys.PtRenewalReminders;
        if (!await _featureFlags.IsEnabledAsync(e.GymId, flag, cancellationToken))
            return;

        var data = new Dictionary<string, string>
        {
            ["type"] = e.Milestone,
            ["packageId"] = e.PackageId.ToString(),
            ["memberId"] = e.MemberId.ToString(),
            ["trainerId"] = e.TrainerId.ToString(),
            ["gymId"] = e.GymId.ToString(),
            ["remaining"] = e.Remaining.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };

        var memberName = await _notifier.DisplayNameAsync(e.MemberId, cancellationToken);

        if (isTrialMilestone)
        {
            var desk = await _desk.GetDeskUserIdsAsync(e.GymId, cancellationToken);
            var text = e.Milestone == Milestones.TrialFinished
                ? PtNotificationTexts.TrialFinished(memberName)
                : PtNotificationTexts.TrialLastSession(memberName);
            await _notifier.SendAsync(
                new PtNotification(e.GymId, desk.Append(e.TrainerId).ToList(), NotificationType.TrialAlert, text, data),
                cancellationToken);
            return;
        }

        await _notifier.SendAsync(
            new PtNotification(e.GymId, new[] { e.MemberId }, NotificationType.PackageLow,
                PtNotificationTexts.RenewalReminderForMember(e.Remaining), data),
            cancellationToken);

        var staff = new List<Guid> { e.TrainerId };
        if (e.Milestone == Milestones.RenewalOneLeft)
            staff.AddRange(await _desk.GetDeskUserIdsAsync(e.GymId, cancellationToken));

        await _notifier.SendAsync(
            new PtNotification(e.GymId, staff, NotificationType.PackageLow,
                PtNotificationTexts.RenewalReminderForStaff(memberName, e.Remaining), data),
            cancellationToken);
    }
}
