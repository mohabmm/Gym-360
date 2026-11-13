using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Domain;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Scheduling.IntegrationEvents;
using MediatR;
using GymApp.Modules.Communication.Application.Abstractions;
using GymApp.Modules.Communication.Domain.ValueObjects;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

internal sealed class SessionCompletedIntegrationEventHandler : INotificationHandler<SessionCompletedIntegrationEvent>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICommunicationUnitOfWork _unitOfWork;
    private readonly IFeatureFlagService _featureFlags;
    private readonly PtNotifier _ptNotifier;

    public SessionCompletedIntegrationEventHandler(
        INotificationRepository notificationRepository,
        ICommunicationUnitOfWork unitOfWork,
        IFeatureFlagService featureFlags,
        PtNotifier ptNotifier)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _featureFlags = featureFlags;
        _ptNotifier = ptNotifier;
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
            new NotificationDetails
            {
                UserId = notification.TrainerId,
                Type = NotificationType.TrainerFeedback,
                GymId = notification.GymId,
                Data = data
            },
            NotificationTexts.SessionCompletedForTrainer());

        await _notificationRepository.AddAsync(trainerNotif, cancellationToken);

        // pt.dual_session_logging: ask the trainee to confirm (in-app + push) instead of the
        // plain "session completed" note.
        if (await _featureFlags.IsEnabledAsync(notification.GymId, FeatureKeys.PtDualSessionLogging, cancellationToken))
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            var confirmData = new Dictionary<string, string>(data) { ["type"] = "session_confirm" };
            await _ptNotifier.SendAsync(
                new PtNotification(notification.GymId, new[] { notification.MemberId }, NotificationType.SessionConfirmation,
                    PtNotificationTexts.SessionConfirmRequest(), confirmData),
                cancellationToken);
            return;
        }

        // Notify member
        var memberNotif = Notification.CreateLocalized(
            new NotificationDetails
            {
                UserId = notification.MemberId,
                Type = NotificationType.SessionReminder,
                GymId = notification.GymId,
                Data = data
            },
            NotificationTexts.SessionCompletedForMember());

        await _notificationRepository.AddAsync(memberNotif, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
