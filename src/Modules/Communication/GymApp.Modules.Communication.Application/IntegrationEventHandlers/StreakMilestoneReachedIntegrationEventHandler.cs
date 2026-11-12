using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Domain;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Progress.IntegrationEvents;
using MediatR;
using GymApp.Modules.Communication.Application.Abstractions;
using GymApp.Modules.Communication.Domain.ValueObjects;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

internal sealed class StreakMilestoneReachedIntegrationEventHandler : INotificationHandler<StreakMilestoneReachedIntegrationEvent>
{
    private readonly IFeatureFlagService _featureFlags;
    private readonly INotificationRepository _notificationRepository;
    private readonly ICommunicationUnitOfWork _unitOfWork;

    public StreakMilestoneReachedIntegrationEventHandler(
        IFeatureFlagService featureFlags,
        INotificationRepository notificationRepository,
        ICommunicationUnitOfWork unitOfWork)
    {
        _featureFlags = featureFlags;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(StreakMilestoneReachedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        // Notifications of disabled features never run for that gym.
        if (!await _featureFlags.IsEnabledAsync(notification.GymId, FeatureKeys.GamificationStreaks, cancellationToken))
            return;

        var data = new Dictionary<string, string>
        {
            ["streakType"] = notification.StreakType,
            ["milestoneDay"] = notification.MilestoneDay.ToString(),
            ["gymId"] = notification.GymId.ToString()
        };

        var notif = Notification.CreateLocalized(
            new NotificationDetails
            {
                UserId = notification.UserId,
                Type = NotificationType.StreakMilestone,
                GymId = notification.GymId,
                Data = data
            },
            NotificationTexts.StreakMilestone(notification.MilestoneDay, notification.StreakType));

        await _notificationRepository.AddAsync(notif, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
