using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Communication.Application.Contracts;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Communication.Domain.ValueObjects;
using GymApp.Modules.Communication.Application.Abstractions;

namespace GymApp.Modules.Communication.Application.Commands.CreateNotification;

internal sealed class CreateNotificationCommandHandler : ICommandHandler<CreateNotificationCommand, Guid>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ICommunicationUnitOfWork _unitOfWork;

    public CreateNotificationCommandHandler(
        INotificationRepository notificationRepository,
        INotificationPreferenceRepository preferenceRepository,
        IPushNotificationService pushNotificationService,
        ICommunicationUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _preferenceRepository = preferenceRepository;
        _pushNotificationService = pushNotificationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<NotificationType>(request.Type, ignoreCase: true, out var notificationType))
            notificationType = NotificationType.General;

        var preferences = await _preferenceRepository.GetByUserAsync(request.UserId, request.GymId, cancellationToken);

        // Check if user has this notification type disabled
        if (preferences is not null && !preferences.IsEnabled(notificationType))
            return Result.Success(Guid.Empty);

        var details = new NotificationDetails
        {
            UserId = request.UserId,
            Type = notificationType,
            GymId = request.GymId,
            Data = request.Data
        };
        var notification = Notification.Create(details, request.Title, request.Body);

        await _notificationRepository.AddAsync(notification, cancellationToken);

        // Send push notification if enabled
        if (preferences is null || preferences.IsPushEnabled(notificationType))
        {
            bool isInQuietHours = preferences?.IsInQuietHours(TimeOnly.FromDateTime(DateTime.UtcNow)) ?? false;

            if (!isInQuietHours)
            {
                await _pushNotificationService.SendAsync(
                    request.UserId,
                    new PushContent(request.Title, request.Body, request.Data),
                    cancellationToken);

                notification.MarkPushSent();
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(notification.Id);
    }
}
