using GymApp.Modules.Communication.Application.Abstractions;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Gyms.IntegrationEvents;
using GymApp.Modules.Gyms.Shared;
using MediatR;
using Microsoft.Extensions.Logging;
using GymApp.Modules.Communication.Domain.ValueObjects;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

/// <summary>
/// Fans an announcement out to a personal notification per gym member. Previously
/// this wrote a single placeholder notification with <c>UserId = Guid.Empty</c>,
/// which no member's "my notifications" query could ever match — so announcements
/// were effectively invisible in-app. We now resolve the gym roster via the
/// Gyms cross-module membership contract and create one notification each.
/// </summary>
internal sealed class AnnouncementPublishedIntegrationEventHandler
    : INotificationHandler<AnnouncementPublishedIntegrationEvent>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICommunicationUnitOfWork _unitOfWork;
    private readonly IGymMembershipChecker _membershipChecker;
    private readonly ILogger<AnnouncementPublishedIntegrationEventHandler> _logger;

    public AnnouncementPublishedIntegrationEventHandler(
        INotificationRepository notificationRepository,
        ICommunicationUnitOfWork unitOfWork,
        IGymMembershipChecker membershipChecker,
        ILogger<AnnouncementPublishedIntegrationEventHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _membershipChecker = membershipChecker;
        _logger = logger;
    }

    public async Task Handle(
        AnnouncementPublishedIntegrationEvent notification,
        CancellationToken cancellationToken)
    {
        var memberIds = await _membershipChecker.GetMemberIdsAsync(
            notification.GymId, cancellationToken);

        if (memberIds.Count == 0)
        {
            _logger.LogInformation(
                "Announcement {AnnouncementId} for gym {GymId} published with no members to notify.",
                notification.AnnouncementId, notification.GymId);
            return;
        }

        var data = new Dictionary<string, string>
        {
            ["announcementId"] = notification.AnnouncementId.ToString(),
            ["gymId"] = notification.GymId.ToString(),
        };

        foreach (var memberId in memberIds)
        {
            var notif = Notification.CreateLocalized(
                new NotificationDetails
                {
                    UserId = memberId,
                    Type = NotificationType.Announcement,
                    GymId = notification.GymId,
                    // Each notification carries its own copy of the metadata.
                    Data = new Dictionary<string, string>(data)
                },
                NotificationTexts.Announcement(notification.Title));

            await _notificationRepository.AddAsync(notif, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Fanned out announcement {AnnouncementId} (gym {GymId}) to {Count} member(s).",
            notification.AnnouncementId, notification.GymId, memberIds.Count);
    }
}
