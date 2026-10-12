using GymApp.Common.Domain;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Training.IntegrationEvents;
using MediatR;
using GymApp.Modules.Communication.Application.Abstractions;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

internal sealed class RemoteWorkoutDeactivatedIntegrationEventHandler : INotificationHandler<RemoteWorkoutDeactivatedIntegrationEvent>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICommunicationUnitOfWork _unitOfWork;

    public RemoteWorkoutDeactivatedIntegrationEventHandler(
        INotificationRepository notificationRepository,
        ICommunicationUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RemoteWorkoutDeactivatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, string>
        {
            ["trainerId"] = notification.TrainerId.ToString(),
            ["gymId"] = notification.GymId.ToString()
        };

        var notif = NotificationTexts.RemoteWorkoutDeactivated().ToNotification(
            notification.ClientId,
            NotificationType.RemoteWorkoutActivated,
            notification.GymId,
            data);

        await _notificationRepository.AddAsync(notif, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
