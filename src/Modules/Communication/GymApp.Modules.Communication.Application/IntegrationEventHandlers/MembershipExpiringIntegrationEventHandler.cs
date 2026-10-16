using GymApp.Common.Domain;
using GymApp.Modules.Billing.IntegrationEvents;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Repositories;
using MediatR;
using GymApp.Modules.Communication.Application.Abstractions;
using GymApp.Modules.Communication.Domain.ValueObjects;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

internal sealed class MembershipExpiringIntegrationEventHandler : INotificationHandler<MembershipExpiringIntegrationEvent>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICommunicationUnitOfWork _unitOfWork;

    public MembershipExpiringIntegrationEventHandler(
        INotificationRepository notificationRepository,
        ICommunicationUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(MembershipExpiringIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, string>
        {
            ["gymId"] = notification.GymId.ToString(),
            ["expiresAt"] = notification.ExpiresAt.ToString("O")
        };

        var notif = Notification.CreateLocalized(
            new NotificationDetails
            {
                UserId = notification.MemberId,
                Type = NotificationType.MembershipExpiry,
                GymId = notification.GymId,
                Data = data
            },
            NotificationTexts.MembershipExpiring(notification.ExpiresAt));

        await _notificationRepository.AddAsync(notif, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
