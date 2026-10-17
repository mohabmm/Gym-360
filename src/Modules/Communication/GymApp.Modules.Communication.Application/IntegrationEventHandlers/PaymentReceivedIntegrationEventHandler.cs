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

internal sealed class PaymentReceivedIntegrationEventHandler : INotificationHandler<PaymentReceivedIntegrationEvent>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICommunicationUnitOfWork _unitOfWork;

    public PaymentReceivedIntegrationEventHandler(
        INotificationRepository notificationRepository,
        ICommunicationUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(PaymentReceivedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, string>
        {
            ["paymentId"] = notification.PaymentId.ToString(),
            ["gymId"] = notification.GymId.ToString(),
            ["amount"] = notification.Amount.ToString("F2")
        };

        var notif = Notification.CreateLocalized(
            new NotificationDetails
            {
                UserId = notification.MemberId,
                Type = NotificationType.PaymentReceived,
                GymId = notification.GymId,
                Data = data
            },
            NotificationTexts.PaymentReceived(notification.Amount));

        await _notificationRepository.AddAsync(notif, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
