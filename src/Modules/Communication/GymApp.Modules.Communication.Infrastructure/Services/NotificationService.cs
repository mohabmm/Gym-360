using GymApp.Modules.Communication.Application.Abstractions;
using GymApp.Modules.Communication.Application.Contracts;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Communication.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Communication.Infrastructure.Services;

internal sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ICommunicationUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        INotificationPreferenceRepository preferenceRepository,
        IPushNotificationService pushNotificationService,
        ICommunicationUnitOfWork unitOfWork,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _preferenceRepository = preferenceRepository;
        _pushNotificationService = pushNotificationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task SendNotificationAsync(
        Guid userId,
        PushContent content,
        string type,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<NotificationType>(type, true, out var notificationType))
        {
            notificationType = NotificationType.General;
        }

        var preference = await _preferenceRepository.GetByUserAsync(userId, cancellationToken: cancellationToken);

        if (preference is not null && !preference.IsEnabled(notificationType))
        {
            _logger.LogInformation(
                "Notification of type {Type} suppressed for user {UserId} due to preferences",
                type, userId);
            return;
        }

        var details = new NotificationDetails
        {
            UserId = userId,
            Type = notificationType,
            Data = content.Data
        };
        var notification = Notification.Create(details, content.Title, content.Body);
        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (ShouldPush(preference, notificationType))
        {
            await SendPushNotificationAsync(userId, content, cancellationToken);
        }
    }

    public async Task SendPushNotificationAsync(
        Guid userId,
        PushContent content,
        CancellationToken cancellationToken = default)
    {
        await _pushNotificationService.SendAsync(userId, content, cancellationToken);
    }

    // No preference row means defaults: push on, no quiet hours.
    private static bool ShouldPush(NotificationPreference? preference, NotificationType notificationType) =>
        preference is null
        || (preference.IsPushEnabled(notificationType)
            && !preference.IsInQuietHours(TimeOnly.FromDateTime(DateTime.UtcNow)));
}
