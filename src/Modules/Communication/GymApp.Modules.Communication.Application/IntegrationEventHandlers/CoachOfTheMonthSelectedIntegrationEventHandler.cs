using GymApp.Common.Domain;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Gamification.IntegrationEvents;
using MediatR;
using GymApp.Modules.Communication.Application.Abstractions;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

internal sealed class CoachOfTheMonthSelectedIntegrationEventHandler : INotificationHandler<CoachOfTheMonthSelectedIntegrationEvent>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICommunicationUnitOfWork _unitOfWork;

    public CoachOfTheMonthSelectedIntegrationEventHandler(
        INotificationRepository notificationRepository,
        ICommunicationUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CoachOfTheMonthSelectedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, string>
        {
            ["trainerId"] = notification.TrainerId.ToString(),
            ["gymId"] = notification.GymId.ToString(),
            ["month"] = notification.Month.ToString(),
            ["year"] = notification.Year.ToString()
        };

        // Notify the trainer
        var trainerNotif = NotificationTexts.CoachOfTheMonth(notification.Month, notification.Year).ToNotification(
            notification.TrainerId,
            NotificationType.CoachOfTheMonth,
            notification.GymId,
            data);

        await _notificationRepository.AddAsync(trainerNotif, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
