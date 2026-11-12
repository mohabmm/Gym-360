using GymApp.Common.Application.Messaging;
using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Scheduling.IntegrationEvents;
using MediatR;

namespace GymApp.Modules.Billing.Application.IntegrationEvents;

internal sealed class SessionLateCancelledIntegrationEventHandler
    : INotificationHandler<SessionLateCancelledIntegrationEvent>
{
    private readonly ISessionPackageRepository _packageRepository;
    private readonly IBillingUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _integrationEvents;

    public SessionLateCancelledIntegrationEventHandler(
        ISessionPackageRepository packageRepository,
        IBillingUnitOfWork unitOfWork,
        IIntegrationEventPublisher integrationEvents)
    {
        _integrationEvents = integrationEvents;
        _packageRepository = packageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SessionLateCancelledIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var package = await _packageRepository.GetActiveByMemberAndTrainerAsync(
            notification.GymId,
            notification.MemberId,
            notification.TrainerId,
            cancellationToken);

        if (package is null || package.Status != PackageStatus.Active)
            return;

        var result = package.RecordLateCancelUsage(notification.SessionId);
        if (result.IsFailure)
            return;

        _packageRepository.Update(package);
        PackageMilestonePublisher.Enqueue(_integrationEvents, package);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
