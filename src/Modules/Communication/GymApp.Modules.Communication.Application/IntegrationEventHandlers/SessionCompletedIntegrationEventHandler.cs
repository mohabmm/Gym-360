using GymApp.Common.Domain;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Scheduling.IntegrationEvents;
using MediatR;
using GymApp.Modules.Communication.Application.Abstractions;

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
            NotificationTexts.SessionCompletedForTrainer(),
            notification.TrainerId,
            NotificationType.TrainerFeedback,
            notification.GymId,
            data);

        await _notificationRepository.AddAsync(trainerNotif, cancellationToken);

        // Notify member
        var memberNotif = Notification.CreateLocalized(
            NotificationTexts.SessionCompletedForMember(),
            notification.MemberId,
            NotificationType.SessionReminder,
            notification.GymId,
            data);

        await _notificationRepository.AddAsync(memberNotif, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
