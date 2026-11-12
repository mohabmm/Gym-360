using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Scheduling.IntegrationEvents;
using MediatR;

namespace GymApp.Modules.Billing.Application.IntegrationEvents;

/// <summary>
/// The desk reversed a session the trainee disputed: give the session back to the package it
/// was counted against. Idempotent (SessionPackage.RestoreSession).
/// </summary>
internal sealed class SessionReversedIntegrationEventHandler : INotificationHandler<SessionReversedIntegrationEvent>
{
    private readonly ISessionPackageRepository _packageRepository;
    private readonly IBillingUnitOfWork _unitOfWork;

    public SessionReversedIntegrationEventHandler(
        ISessionPackageRepository packageRepository,
        IBillingUnitOfWork unitOfWork)
    {
        _packageRepository = packageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SessionReversedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var package = await _packageRepository.FindByUsageSessionAsync(
            notification.GymId, notification.SessionId, cancellationToken);
        if (package is null)
            return;

        package.RestoreSession(notification.SessionId);
        _packageRepository.Update(package);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
