using GymApp.Common.Domain;
using GymApp.Modules.Billing.IntegrationEvents;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Repositories;
using MediatR;
using GymApp.Modules.Communication.Application.Abstractions;

namespace GymApp.Modules.Communication.Application.IntegrationEventHandlers;

internal sealed class SessionPackageLowIntegrationEventHandler : INotificationHandler<SessionPackageLowIntegrationEvent>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICommunicationUnitOfWork _unitOfWork;

    public SessionPackageLowIntegrationEventHandler(
        INotificationRepository notificationRepository,
        ICommunicationUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SessionPackageLowIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, string>
        {
            ["packageId"] = notification.PackageId.ToString(),
            ["gymId"] = notification.GymId.ToString(),
            ["remaining"] = notification.Remaining.ToString()
        };

        // Notify member
        var memberNotif = Notification.Create(
            notification.MemberId,
            "Session Package Running Low",
            $"You have only {notification.Remaining} sessions remaining in your package.",
            NotificationType.PackageLow,
            notification.GymId,
            data);

        await _notificationRepository.AddAsync(memberNotif, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
