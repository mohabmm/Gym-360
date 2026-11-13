using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Domain;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Gamification.IntegrationEvents;
using MediatR;
using GymApp.Modules.Communication.Application.Abstractions;
using GymApp.Modules.Communication.Domain.ValueObjects;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

internal sealed class TrainerRatedIntegrationEventHandler : INotificationHandler<TrainerRatedIntegrationEvent>
{
    private readonly IFeatureFlagService _featureFlags;
    private readonly INotificationRepository _notificationRepository;
    private readonly ICommunicationUnitOfWork _unitOfWork;

    public TrainerRatedIntegrationEventHandler(
        IFeatureFlagService featureFlags,
        INotificationRepository notificationRepository,
        ICommunicationUnitOfWork unitOfWork)
    {
        _featureFlags = featureFlags;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(TrainerRatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        // Notifications of disabled features never run for that gym.
        if (!await _featureFlags.IsEnabledAsync(notification.GymId, FeatureKeys.PtCoachRatings, cancellationToken))
            return;

        var data = new Dictionary<string, string>
        {
            ["trainerId"] = notification.TrainerId.ToString(),
            ["gymId"] = notification.GymId.ToString(),
            ["stars"] = notification.Stars.ToString()
        };

        // Notify the trainer about the new rating
        var notif = Notification.CreateLocalized(
            new NotificationDetails
            {
                UserId = notification.TrainerId,
                Type = NotificationType.TrainerFeedback,
                GymId = notification.GymId,
                Data = data
            },
            NotificationTexts.TrainerRated(notification.Stars));

        await _notificationRepository.AddAsync(notif, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
