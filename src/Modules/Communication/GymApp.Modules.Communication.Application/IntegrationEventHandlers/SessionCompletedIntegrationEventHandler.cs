using GymApp.Common.Domain;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Scheduling.IntegrationEvents;
using MediatR;
using GymApp.Modules.Communication.Application.Abstractions;
using GymApp.Modules.Communication.Domain.ValueObjects;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

internal sealed class SessionCompletedIntegrationEventHandler : INotificationHandler<SessionCompletedIntegrationEvent>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICommunicationUnitOfWork _unitOfWork;

    public SessionCompletedIntegrationEventHandler(
        INotificationRepository notificationRepository,
        ICommunicationUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SessionCompletedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, string>
        {
            ["sessionId"] = notification.SessionId.ToString(),
            ["gymId"] = notification.GymId.ToString()
        };

        // Notify trainer
        var trainerNotif = Notification.CreateLocalized(
            new NotificationDetails
            {
                UserId = notification.TrainerId,
                Type = NotificationType.TrainerFeedback,
                GymId = notification.GymId,
                Data = data
            },
            NotificationTexts.SessionCompletedForTrainer());

        await _notificationRepository.AddAsync(trainerNotif, cancellationToken);

        // Notify member
        var memberNotif = Notification.CreateLocalized(
            new NotificationDetails
            {
                UserId = notification.MemberId,
                Type = NotificationType.SessionReminder,
                GymId = notification.GymId,
                Data = data
            },
            NotificationTexts.SessionCompletedForMember());

        await _notificationRepository.AddAsync(memberNotif, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
