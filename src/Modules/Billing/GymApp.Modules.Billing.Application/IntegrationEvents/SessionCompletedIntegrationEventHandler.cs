using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;
using GymApp.Modules.Scheduling.IntegrationEvents;
using MediatR;
using GymApp.Modules.Billing.Application.Abstractions;

namespace GymApp.Modules.Billing.Application.IntegrationEvents;

internal sealed class SessionCompletedIntegrationEventHandler : INotificationHandler<SessionCompletedIntegrationEvent>
{
    private readonly ISessionPackageRepository _packageRepository;
    private readonly ITrainerEarningsRepository _earningsRepository;
    private readonly IBillingUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _integrationEvents;

    public SessionCompletedIntegrationEventHandler(
        ISessionPackageRepository packageRepository,
        ITrainerEarningsRepository earningsRepository,
        IBillingUnitOfWork unitOfWork,
        IIntegrationEventPublisher integrationEvents)
    {
        _integrationEvents = integrationEvents;
        _packageRepository = packageRepository;
        _earningsRepository = earningsRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SessionCompletedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        // Existing logic: deduct session from active package
        var package = await _packageRepository.GetActiveByMemberAndTrainerAsync(
            notification.GymId,
            notification.MemberId,
            notification.TrainerId,
            cancellationToken);

        if (package is not null && package.Status == PackageStatus.Active)
        {
            package.UseSession(notification.SessionId);
            _packageRepository.Update(package);
            PackageMilestonePublisher.Enqueue(_integrationEvents, package);
        }

        // New logic: record trainer earnings at configured rate
        var rate = await _earningsRepository.GetTrainerRateAsync(
            notification.GymId,
            notification.TrainerId,
            cancellationToken);

        // Idempotency: at-least-once delivery may re-run this; never pay a trainer twice for the
        // same session. (The package deduction above is guarded inside SessionPackage.UseSession.)
        var alreadyRecorded = await _earningsRepository.ExistsForSessionAsync(
            notification.GymId,
            notification.TrainerId,
            notification.SessionId,
            cancellationToken);

        if (rate.HasValue && !alreadyRecorded)
        {
            var earningsRecord = TrainerEarningsRecord.Create(new TrainerEarningsDetails
            {
                GymId = notification.GymId,
                TrainerId = notification.TrainerId,
                SessionId = notification.SessionId,
                MemberId = notification.MemberId,
                RateApplied = rate.Value
            });

            await _earningsRepository.AddAsync(earningsRecord, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
