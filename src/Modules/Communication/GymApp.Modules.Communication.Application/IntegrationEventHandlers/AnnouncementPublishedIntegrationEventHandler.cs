using GymApp.Modules.Communication.Application.Abstractions;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Gyms.IntegrationEvents;
using GymApp.Modules.Identity.Shared;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

/// <summary>
/// Fans an announcement out to a personal notification per gym member. Previously
/// this wrote a single placeholder notification with <c>UserId = Guid.Empty</c>,
/// which no member's "my notifications" query could ever match — so announcements
/// were effectively invisible in-app. We now resolve the gym's members via the
/// Identity cross-module profile lookup and create one notification each.
/// </summary>
internal sealed class AnnouncementPublishedIntegrationEventHandler
    : INotificationHandler<AnnouncementPublishedIntegrationEvent>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICommunicationUnitOfWork _unitOfWork;
    private readonly IUserProfileLookup _userProfileLookup;
    private readonly ILogger<AnnouncementPublishedIntegrationEventHandler> _logger;

    public AnnouncementPublishedIntegrationEventHandler(
        INotificationRepository notificationRepository,
        ICommunicationUnitOfWork unitOfWork,
        IUserProfileLookup userProfileLookup,
        ILogger<AnnouncementPublishedIntegrationEventHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _userProfileLookup = userProfileLookup;
        _logger = logger;
    }

    public async Task Handle(
        AnnouncementPublishedIntegrationEvent notification,
        CancellationToken cancellationToken)
    {
        var members = await _userProfileLookup.GetProfilesByGymAsync(
            notification.GymId, cancellationToken);

        if (members.Count == 0)
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

        var created = 0;
        foreach (var member in members)
        {
            var notif = Notification.Create(
                member.UserId,
                notification.Title,
                $"New announcement: {notification.Title}",
                NotificationType.Announcement,
                notification.GymId,
                // Each notification carries its own copy of the metadata.
                new Dictionary<string, string>(data));

            await _notificationRepository.AddAsync(notif, cancellationToken);
            created++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Fanned out announcement {AnnouncementId} (gym {GymId}) to {Count} member(s).",
            notification.AnnouncementId, notification.GymId, created);
    }
}
