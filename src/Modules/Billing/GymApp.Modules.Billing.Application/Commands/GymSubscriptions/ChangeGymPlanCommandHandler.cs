using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Application.Abstractions;

namespace GymApp.Modules.Billing.Application.Commands.GymSubscriptions;

internal sealed class ChangeGymPlanCommandHandler : ICommandHandler<ChangeGymPlanCommand>
{
    private readonly IGymSubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IPlanChangeLogRepository _changeLogRepository;
    private readonly IBillingUnitOfWork _unitOfWork;

    public ChangeGymPlanCommandHandler(
        IGymSubscriptionRepository subscriptionRepository,
        ISubscriptionPlanRepository planRepository,
        IPlanChangeLogRepository changeLogRepository,
        IBillingUnitOfWork unitOfWork)
    {
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
        _changeLogRepository = changeLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ChangeGymPlanCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepository.GetByGymIdAsync(request.GymId, cancellationToken);

        if (subscription is null)
        {
            return Result.Failure(BillingErrors.SubscriptionNotFound);
        }

        var newPlan = await _planRepository.GetByIdAsync(request.PlanId, cancellationToken);

        if (newPlan is null)
        {
            return Result.Failure(BillingErrors.PlanNotFound);
        }

        if (!newPlan.IsActive)
        {
            return Result.Failure(BillingErrors.PlanNotActive);
        }

        var oldPlanId = subscription.PlanId;
        subscription.ChangePlan(request.PlanId);
        _subscriptionRepository.Update(subscription);

        var log = PlanChangeLog.Create(
            "PlanChanged",
            request.ChangedBy,
            request.GymId,
            "GymSubscription",
            subscription.Id,
            oldPlanId.ToString(),
            request.PlanId.ToString(),
            request.Reason);

        await _changeLogRepository.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
