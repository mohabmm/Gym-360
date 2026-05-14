using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Modules.Billing.Application.Queries.GymSubscriptions;

internal sealed class GetGymSubscriptionQueryHandler : IQueryHandler<GetGymSubscriptionQuery, GymSubscriptionDetailDto>
{
    private readonly IGymSubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionPlanRepository _planRepository;

    public GetGymSubscriptionQueryHandler(
        IGymSubscriptionRepository subscriptionRepository,
        ISubscriptionPlanRepository planRepository)
    {
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
    }

    public async Task<Result<GymSubscriptionDetailDto>> Handle(GetGymSubscriptionQuery request, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepository.GetByGymIdAsync(request.GymId, cancellationToken);

        if (subscription is null)
        {
            return Result.Failure<GymSubscriptionDetailDto>(BillingErrors.SubscriptionNotFound);
        }

        var plan = await _planRepository.GetByIdAsync(subscription.PlanId, cancellationToken);

        var dto = new GymSubscriptionDetailDto(
            subscription.Id,
            subscription.GymId,
            subscription.PlanId,
            plan?.Code ?? "Unknown",
            plan?.Name ?? "Unknown",
            subscription.StartedAt,
            subscription.ExpiresAt,
            subscription.IsActive,
            subscription.IsTrial,
            subscription.TrialExpiresAt,
            subscription.PaymentStatus.ToString(),
            subscription.LastPaymentAt,
            subscription.NextPaymentDue,
            subscription.OverdueSince,
            subscription.GracePeriodEndsAt);

        return Result.Success(dto);
    }
}
