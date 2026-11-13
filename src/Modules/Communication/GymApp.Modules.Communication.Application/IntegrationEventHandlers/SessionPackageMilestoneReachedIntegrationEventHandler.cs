using GymApp.Common.Application.FeatureFlags;
using GymApp.Modules.Billing.IntegrationEvents;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.ValueObjects;
using GymApp.Modules.Gyms.Shared;
using MediatR;
using Milestones = GymApp.Modules.Billing.IntegrationEvents.SessionPackageMilestoneReachedIntegrationEvent.Milestones;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

/// <summary>
/// pt.renewal_reminders — at exactly 3 and exactly 1 sessions left: trainee + coach (desk too at 1).
/// pt.trial_end_alerts — last free session and trial finished: coach + desk.
/// pt.trial_followups — trial finished, then day 3 and day 12 without a package: the trainee.
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

    public Task Handle(SessionPackageMilestoneReachedIntegrationEvent e, CancellationToken cancellationToken) =>
        e.Milestone switch
        {
            Milestones.TrialLastSession => NotifyTrialStaffAsync(e, cancellationToken),
            Milestones.TrialFinished => NotifyTrialFinishedAsync(e, cancellationToken),
            Milestones.TrialFollowUpDay3 or Milestones.TrialLastChanceDay12 => NotifyMemberFollowUpAsync(e, cancellationToken),
            _ => NotifyRenewalAsync(e, cancellationToken),
        };

    private async Task NotifyTrialFinishedAsync(SessionPackageMilestoneReachedIntegrationEvent e, CancellationToken cancellationToken)
    {
        await NotifyTrialStaffAsync(e, cancellationToken);
        await NotifyMemberFollowUpAsync(e, cancellationToken);
    }

    private async Task NotifyTrialStaffAsync(SessionPackageMilestoneReachedIntegrationEvent e, CancellationToken cancellationToken)
    {
        if (!await _featureFlags.IsEnabledAsync(e.GymId, FeatureKeys.PtTrialEndAlerts, cancellationToken))
            return;

        var memberName = await _notifier.DisplayNameAsync(e.MemberId, cancellationToken);
        var desk = await _desk.GetDeskUserIdsAsync(e.GymId, cancellationToken);
        var text = e.Milestone == Milestones.TrialFinished
            ? PtNotificationTexts.TrialFinished(memberName)
            : PtNotificationTexts.TrialLastSession(memberName);
        await _notifier.SendAsync(
            new PtNotification(e.GymId, desk.Append(e.TrainerId).ToList(), NotificationType.TrialAlert, text, DataOf(e)),
            cancellationToken);
    }

    private async Task NotifyMemberFollowUpAsync(SessionPackageMilestoneReachedIntegrationEvent e, CancellationToken cancellationToken)
    {
        if (!await _featureFlags.IsEnabledAsync(e.GymId, FeatureKeys.PtTrialFollowUps, cancellationToken))
            return;

        var coachName = await _notifier.DisplayNameAsync(e.TrainerId, cancellationToken);
        await _notifier.SendAsync(
            new PtNotification(e.GymId, new[] { e.MemberId }, NotificationType.TrialFollowUp, FollowUpText(e.Milestone, coachName), DataOf(e)),
            cancellationToken);
    }

    private static NotificationText FollowUpText(string milestone, string coachName) => milestone switch
    {
        Milestones.TrialFollowUpDay3 => PtConversionTexts.FollowUpDay3ForMember(coachName),
        Milestones.TrialLastChanceDay12 => PtConversionTexts.LastChanceForMember(coachName),
        _ => PtConversionTexts.TrialFinishedForMember(coachName),
    };

    private async Task NotifyRenewalAsync(SessionPackageMilestoneReachedIntegrationEvent e, CancellationToken cancellationToken)
    {
        if (!await _featureFlags.IsEnabledAsync(e.GymId, FeatureKeys.PtRenewalReminders, cancellationToken))
            return;

        var data = DataOf(e);
        await _notifier.SendAsync(
            new PtNotification(e.GymId, new[] { e.MemberId }, NotificationType.PackageLow,
                PtNotificationTexts.RenewalReminderForMember(e.Remaining), data),
            cancellationToken);

        var staff = new List<Guid> { e.TrainerId };
        if (e.Milestone == Milestones.RenewalOneLeft)
            staff.AddRange(await _desk.GetDeskUserIdsAsync(e.GymId, cancellationToken));

        var memberName = await _notifier.DisplayNameAsync(e.MemberId, cancellationToken);
        await _notifier.SendAsync(
            new PtNotification(e.GymId, staff, NotificationType.PackageLow,
                PtNotificationTexts.RenewalReminderForStaff(memberName, e.Remaining), data),
            cancellationToken);
    }

    private static Dictionary<string, string> DataOf(SessionPackageMilestoneReachedIntegrationEvent e) => new()
    {
        ["type"] = e.Milestone,
        ["packageId"] = e.PackageId.ToString(),
        ["memberId"] = e.MemberId.ToString(),
        ["trainerId"] = e.TrainerId.ToString(),
        ["gymId"] = e.GymId.ToString(),
        ["remaining"] = e.Remaining.ToString(System.Globalization.CultureInfo.InvariantCulture),
    };
}
