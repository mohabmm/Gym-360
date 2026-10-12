using GymApp.Common.Domain;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Progress.IntegrationEvents;
using MediatR;
using GymApp.Modules.Communication.Application.Abstractions;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

internal sealed class StreakMilestoneReachedIntegrationEventHandler : INotificationHandler<StreakMilestoneReachedIntegrationEvent>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICommunicationUnitOfWork _unitOfWork;

    public StreakMilestoneReachedIntegrationEventHandler(
        INotificationRepository notificationRepository,
        ICommunicationUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(StreakMilestoneReachedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, string>
        {
            ["streakType"] = notification.StreakType,
            ["milestoneDay"] = notification.MilestoneDay.ToString(),
            ["gymId"] = notification.GymId.ToString()
        };

        var notif = NotificationTexts.StreakMilestone(notification.MilestoneDay, notification.StreakType).ToNotification(
            notification.UserId,
            NotificationType.StreakMilestone,
            notification.GymId,
            data);

        await _notificationRepository.AddAsync(notif, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
