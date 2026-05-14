using GymApp.Common.Domain;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Gyms.IntegrationEvents;
using MediatR;
using GymApp.Modules.Communication.Application.Abstractions;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

internal sealed class AnnouncementPublishedIntegrationEventHandler : INotificationHandler<AnnouncementPublishedIntegrationEvent>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICommunicationUnitOfWork _unitOfWork;

    public AnnouncementPublishedIntegrationEventHandler(
        INotificationRepository notificationRepository,
        ICommunicationUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AnnouncementPublishedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        // Note: In a full implementation, this handler would look up the target audience
        // from the Gyms module and create notifications for each member.
        // For now, we create a gym-level notification that can be fetched by all members.
        var data = new Dictionary<string, string>
        {
            ["announcementId"] = notification.AnnouncementId.ToString(),
            ["gymId"] = notification.GymId.ToString()
        };

        // This creates a placeholder notification. The Presentation/Infrastructure layer
        // would fan out to individual members based on the announcement's target audience.
        var notif = Notification.Create(
            Guid.Empty, // Will be fan-out per member in Infrastructure
            notification.Title,
            $"New announcement: {notification.Title}",
            NotificationType.Announcement,
            notification.GymId,
            data);

        await _notificationRepository.AddAsync(notif, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
