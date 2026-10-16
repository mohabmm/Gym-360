using GymApp.Common.Domain;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Scheduling.IntegrationEvents;
using MediatR;
using GymApp.Modules.Communication.Application.Abstractions;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

internal sealed class SessionBookedIntegrationEventHandler : INotificationHandler<SessionBookedIntegrationEvent>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICommunicationUnitOfWork _unitOfWork;

    public SessionBookedIntegrationEventHandler(
        INotificationRepository notificationRepository,
        ICommunicationUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SessionBookedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, string>
        {
            ["sessionId"] = notification.SessionId.ToString(),
            ["memberId"] = notification.MemberId.ToString(),
            ["gymId"] = notification.GymId.ToString(),
            ["scheduledAt"] = notification.ScheduledAt.ToString("O")
        };

        var notif = Notification.CreateLocalized(
            NotificationTexts.SessionBooked(notification.ScheduledAt),
            notification.TrainerId,
            NotificationType.SessionReminder,
            notification.GymId,
            data);

        await _notificationRepository.AddAsync(notif, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
