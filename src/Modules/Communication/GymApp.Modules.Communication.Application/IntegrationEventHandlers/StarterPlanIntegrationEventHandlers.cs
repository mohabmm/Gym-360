using GymApp.Common.Application.FeatureFlags;
using GymApp.Modules.Billing.IntegrationEvents;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Training.IntegrationEvents;
using MediatR;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

/// <summary>
/// pt.starter_plan: tells the coach about a member the desk just gave them (every package,
/// trial or paid) and when the staff open the monthly programs for that member. The tap opens
/// the member's profile in the trainer app. Skipped for gyms where the flag is OFF.
/// </summary>
internal sealed class StarterPlanIntegrationEventHandlers
    : INotificationHandler<SessionPackageAssignedIntegrationEvent>,
      INotificationHandler<PlanProgramsUnlockedIntegrationEvent>
{
    public const string NewClientType = "new_client_assigned";
    public const string ProgramsUnlockedType = "plan_programs_unlocked";

    private readonly IFeatureFlagService _featureFlags;
    private readonly PtNotifier _notifier;

    public StarterPlanIntegrationEventHandlers(IFeatureFlagService featureFlags, PtNotifier notifier)
    {
        _featureFlags = featureFlags;
        _notifier = notifier;
    }

    public async Task Handle(SessionPackageAssignedIntegrationEvent e, CancellationToken cancellationToken)
    {
        if (!await _featureFlags.IsEnabledAsync(e.GymId, FeatureKeys.PtStarterPlan, cancellationToken))
            return;
        var memberName = await _notifier.DisplayNameAsync(e.MemberId, cancellationToken);
        var data = DataFor(new CoachClient(e.GymId, e.MemberId), NewClientType);
        data["packageId"] = e.PackageId.ToString();
        await _notifier.SendAsync(
            new PtNotification(e.GymId, new[] { e.TrainerId }, NotificationType.NewClientAssigned,
                StarterPlanTexts.NewClient(memberName, e.IsTrial, e.TotalSessions), data),
            cancellationToken);
    }

    public async Task Handle(PlanProgramsUnlockedIntegrationEvent e, CancellationToken cancellationToken)
    {
        if (!await _featureFlags.IsEnabledAsync(e.GymId, FeatureKeys.PtStarterPlan, cancellationToken))
            return;
        var memberName = await _notifier.DisplayNameAsync(e.TraineeId, cancellationToken);
        await _notifier.SendAsync(
            new PtNotification(e.GymId, new[] { e.TrainerId }, NotificationType.PlanProgramsUnlocked,
                StarterPlanTexts.ProgramsUnlocked(memberName, e.LengthDays), DataFor(new CoachClient(e.GymId, e.TraineeId), ProgramsUnlockedType)),
            cancellationToken);
    }

    private static Dictionary<string, string> DataFor(CoachClient client, string type) => new()
    {
        ["type"] = type,
        ["gymId"] = client.GymId.ToString(),
        ["memberId"] = client.MemberId.ToString(),
        ["route"] = $"/clients/{client.MemberId}",
    };

    private readonly record struct CoachClient(Guid GymId, Guid MemberId);
}
