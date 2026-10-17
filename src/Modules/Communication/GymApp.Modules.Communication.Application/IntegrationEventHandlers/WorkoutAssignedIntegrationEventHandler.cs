using GymApp.Common.Domain;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Training.IntegrationEvents;
using MediatR;
using GymApp.Modules.Communication.Application.Abstractions;
using GymApp.Modules.Communication.Domain.ValueObjects;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

internal sealed class WorkoutAssignedIntegrationEventHandler : INotificationHandler<WorkoutAssignedIntegrationEvent>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICommunicationUnitOfWork _unitOfWork;

    public WorkoutAssignedIntegrationEventHandler(
        INotificationRepository notificationRepository,
        ICommunicationUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(WorkoutAssignedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, string>
        {
            ["trainerId"] = notification.TrainerId.ToString(),
            ["gymId"] = notification.GymId.ToString(),
            ["workoutName"] = notification.WorkoutName
        };

        var notif = Notification.CreateLocalized(
            new NotificationDetails
            {
                UserId = notification.ClientId,
                Type = NotificationType.WorkoutAssigned,
                GymId = notification.GymId,
                Data = data
            },
            NotificationTexts.WorkoutAssigned(notification.WorkoutName));

        await _notificationRepository.AddAsync(notif, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
