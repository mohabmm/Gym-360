using GymApp.Modules.Communication.Application.Abstractions;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Identity.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

internal sealed class AccountDeletionRequestedIntegrationEventHandler
    : INotificationHandler<AccountDeletionRequestedIntegrationEvent>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICommunicationUnitOfWork _unitOfWork;
    private readonly ILogger<AccountDeletionRequestedIntegrationEventHandler> _logger;

    public AccountDeletionRequestedIntegrationEventHandler(
        INotificationRepository notificationRepository,
        ICommunicationUnitOfWork unitOfWork,
        ILogger<AccountDeletionRequestedIntegrationEventHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(AccountDeletionRequestedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing account deletion for user {UserId} in Communication module. " +
            "Notifications and messages will be deleted on {ScheduledAt}",
            notification.UserId, notification.ScheduledDeletionUtc);

        // Mark all notifications as read immediately (privacy measure — user can no longer access them)
        await _notificationRepository.MarkAllReadAsync(notification.UserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Conversation messages and conversation records will be purged by the
        // scheduled deletion job at ScheduledDeletionUtc.
        _logger.LogInformation(
            "Marked all notifications as read for deleted user {UserId}",
            notification.UserId);
    }
}
