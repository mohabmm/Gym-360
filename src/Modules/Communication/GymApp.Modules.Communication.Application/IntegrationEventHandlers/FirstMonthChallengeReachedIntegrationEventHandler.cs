using GymApp.Common.Application.FeatureFlags;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.ValueObjects;
using GymApp.Modules.Gyms.Shared;
using GymApp.Modules.Training.IntegrationEvents;
using MediatR;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

/// <summary>
/// mem.first_month_challenge — one notification each to the trainee, their coach and the desk.
/// Training raises the event once per member (unique award), so this sends once.
/// </summary>
internal sealed class FirstMonthChallengeReachedIntegrationEventHandler
    : INotificationHandler<FirstMonthChallengeReachedIntegrationEvent>
{
    private readonly IFeatureFlagService _featureFlags;
    private readonly IGymDeskLookup _desk;
    private readonly PtNotifier _notifier;

    public FirstMonthChallengeReachedIntegrationEventHandler(IFeatureFlagService featureFlags, IGymDeskLookup desk, PtNotifier notifier)
    {
        _featureFlags = featureFlags;
        _desk = desk;
        _notifier = notifier;
    }

    public async Task Handle(FirstMonthChallengeReachedIntegrationEvent e, CancellationToken cancellationToken)
    {
        if (!await _featureFlags.IsEnabledAsync(e.GymId, FeatureKeys.MemFirstMonthChallenge, cancellationToken))
            return;

        await SendAsync(e, new Notice(new[] { e.TraineeId }, FirstMonthChallengeTexts.ForTrainee(e.Count, e.RewardTextAr, e.RewardTextEn), "/plan"), cancellationToken);

        var memberName = await _notifier.DisplayNameAsync(e.TraineeId, cancellationToken);
        if (e.TrainerId is { } trainerId)
            await SendAsync(e, new Notice(new[] { trainerId }, FirstMonthChallengeTexts.ForCoach(memberName, e.Count), "/plans/clients"), cancellationToken);

        // A coach who is also on the desk list already heard it as the coach.
        var desk = (await _desk.GetDeskUserIdsAsync(e.GymId, cancellationToken))
            .Where(id => id != e.TrainerId && id != e.TraineeId)
            .ToList();
        if (desk.Count > 0)
            await SendAsync(e, new Notice(desk, FirstMonthChallengeTexts.ForDesk(memberName, e.RewardTextAr, e.RewardTextEn), "/pt/challenge"), cancellationToken);
    }

    /// <summary>Who hears it, in which words, and where tapping it opens.</summary>
    private sealed record Notice(IReadOnlyList<Guid> To, NotificationText Text, string Route);

    private Task SendAsync(FirstMonthChallengeReachedIntegrationEvent e, Notice notice, CancellationToken cancellationToken) =>
        _notifier.SendAsync(
            new PtNotification(e.GymId, notice.To, NotificationType.FirstMonthChallenge, notice.Text, new Dictionary<string, string>
            {
                ["gymId"] = e.GymId.ToString(),
                ["type"] = "first_month_challenge_reached",
                ["awardId"] = e.AwardId.ToString(),
                ["memberId"] = e.TraineeId.ToString(),
                ["route"] = notice.Route
            }),
            cancellationToken);
}
