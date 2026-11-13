using GymApp.Modules.Communication.Application.Abstractions;
using GymApp.Modules.Communication.Application.Contracts;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Communication.Domain.ValueObjects;
using GymApp.Modules.Identity.Shared;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Communication.Application.Notifications;

/// <summary>
/// Sends one PT notification to several people: an in-app notification (Arabic + English) and
/// an FCM push, honouring each recipient's per-type preferences. Push failures never fail
/// the event; the in-app notification is still stored.
/// </summary>
internal sealed class PtNotifier
{
    private readonly INotificationRepository _notifications;
    private readonly INotificationPreferenceRepository _preferences;
    private readonly PtPushSender _push;
    private readonly IUserDisplayLookup _users;
    private readonly ICommunicationUnitOfWork _unitOfWork;

    public PtNotifier(
        INotificationRepository notifications,
        INotificationPreferenceRepository preferences,
        PtPushSender push,
        IUserDisplayLookup users,
        ICommunicationUnitOfWork unitOfWork)
    {
        _notifications = notifications;
        _preferences = preferences;
        _push = push;
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> DisplayNameAsync(Guid userId, CancellationToken cancellationToken)
    {
        var names = await _users.GetDisplayNamesAsync(new[] { userId }, cancellationToken);
        return names.TryGetValue(userId, out var display) && !string.IsNullOrWhiteSpace(display.FullName)
            ? display.FullName
            : "—";
    }

    public async Task SendAsync(PtNotification notification, CancellationToken cancellationToken)
    {
        var pushTo = new List<Guid>();

        foreach (var userId in notification.Recipients.Where(id => id != Guid.Empty).Distinct())
        {
            var preference = await _preferences.GetByUserAsync(userId, notification.GymId, cancellationToken);
            if (preference is not null && !preference.IsEnabled(notification.Type))
                continue;

            await _notifications.AddAsync(ToNotification(notification, userId), cancellationToken);

            if (preference is null || preference.IsPushEnabled(notification.Type))
                pushTo.Add(userId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _push.SendAsync(notification, pushTo, cancellationToken);
    }

    private static Notification ToNotification(PtNotification notification, Guid userId) =>
        Notification.CreateLocalized(
            new NotificationDetails
            {
                UserId = userId,
                Type = notification.Type,
                GymId = notification.GymId,
                Data = notification.Data,
            },
            notification.Text);
}

/// <summary>FCM push for PT notifications (Arabic text). A failed push is logged and never fails the event.</summary>
internal sealed class PtPushSender
{
    private readonly IPushNotificationService _push;
    private readonly ILogger<PtPushSender> _logger;

    public PtPushSender(IPushNotificationService push, ILogger<PtPushSender> logger)
    {
        _push = push;
        _logger = logger;
    }

    public async Task SendAsync(PtNotification notification, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        var content = new PushContent(notification.Text.TitleAr, notification.Text.BodyAr, notification.Data);
        foreach (var userId in userIds)
        {
            try
            {
                await _push.SendAsync(userId, content, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PT push to {UserId} failed ({Type})", userId, notification.Type);
            }
        }
    }
}

internal sealed record PtNotification(
    Guid GymId,
    IReadOnlyCollection<Guid> Recipients,
    NotificationType Type,
    NotificationText Text,
    Dictionary<string, string> Data);
