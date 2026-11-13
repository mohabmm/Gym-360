using GymApp.Common.Application.FeatureFlags;
using GymApp.Modules.Billing.IntegrationEvents;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Gyms.Shared;
using MediatR;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

/// <summary>pt.package_interest — a trainee tapped "I'm interested": desk + coach get the lead.</summary>
internal sealed class PackageInterestCreatedIntegrationEventHandler
    : INotificationHandler<PackageInterestCreatedIntegrationEvent>
{
    private readonly IFeatureFlagService _featureFlags;
    private readonly IGymDeskLookup _desk;
    private readonly PtNotifier _notifier;

    public PackageInterestCreatedIntegrationEventHandler(
        IFeatureFlagService featureFlags,
        IGymDeskLookup desk,
        PtNotifier notifier)
    {
        _featureFlags = featureFlags;
        _desk = desk;
        _notifier = notifier;
    }

    public async Task Handle(PackageInterestCreatedIntegrationEvent e, CancellationToken cancellationToken)
    {
        if (!await _featureFlags.IsEnabledAsync(e.GymId, FeatureKeys.PtPackageInterest, cancellationToken))
            return;

        var memberName = await _notifier.DisplayNameAsync(e.MemberId, cancellationToken);
        var desk = await _desk.GetDeskUserIdsAsync(e.GymId, cancellationToken);
        var data = new Dictionary<string, string>
        {
            ["type"] = "package_interest",
            ["interestId"] = e.InterestId.ToString(),
            ["memberId"] = e.MemberId.ToString(),
            ["trainerId"] = e.TrainerId.ToString(),
            ["gymId"] = e.GymId.ToString(),
            ["source"] = e.Source,
        };

        await _notifier.SendAsync(
            new PtNotification(e.GymId, desk.Append(e.TrainerId).ToList(), NotificationType.PackageInterest,
                PtConversionTexts.InterestForStaff(memberName, e.RecommendedSessions), data),
            cancellationToken);
    }
}
