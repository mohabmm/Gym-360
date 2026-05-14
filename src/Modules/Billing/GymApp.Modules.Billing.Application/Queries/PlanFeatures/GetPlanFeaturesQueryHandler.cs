using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Modules.Billing.Application.Queries.PlanFeatures;

internal sealed class GetPlanFeaturesQueryHandler : IQueryHandler<GetPlanFeaturesQuery, PlanFeaturesResponse>
{
    private readonly IGymSubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IPlanFeatureRepository _planFeatureRepository;
    private readonly IFeatureRepository _featureRepository;
    private readonly IGymFeatureOverrideRepository _overrideRepository;

    public GetPlanFeaturesQueryHandler(
        IGymSubscriptionRepository subscriptionRepository,
        ISubscriptionPlanRepository planRepository,
        IPlanFeatureRepository planFeatureRepository,
        IFeatureRepository featureRepository,
        IGymFeatureOverrideRepository overrideRepository)
    {
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
        _planFeatureRepository = planFeatureRepository;
        _featureRepository = featureRepository;
        _overrideRepository = overrideRepository;
    }

    public async Task<Result<PlanFeaturesResponse>> Handle(GetPlanFeaturesQuery request, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepository.GetByGymIdAsync(request.GymId, cancellationToken);

        if (subscription is null)
        {
            return Result.Failure<PlanFeaturesResponse>(BillingErrors.SubscriptionNotFound);
        }

        var plan = await _planRepository.GetByIdAsync(subscription.PlanId, cancellationToken);

        if (plan is null)
        {
            return Result.Failure<PlanFeaturesResponse>(BillingErrors.PlanNotFound);
        }

        var allFeatures = await _featureRepository.GetAllAsync(cancellationToken);
        var planFeatures = await _planFeatureRepository.GetByPlanIdAsync(subscription.PlanId, cancellationToken);
        var overrides = await _overrideRepository.GetByGymIdAsync(request.GymId, cancellationToken);

        var overrideDict = overrides.ToDictionary(o => o.FeatureId);
        var planFeatureDict = planFeatures.ToDictionary(pf => pf.FeatureId);

        var featureItems = allFeatures
            .Where(f => f.IsActive)
            .Select(f =>
            {
                var hasOverride = overrideDict.TryGetValue(f.Id, out var gymOverride);
                var hasPlanFeature = planFeatureDict.TryGetValue(f.Id, out var planFeature);

                bool isEnabled = hasOverride
                    ? gymOverride!.OverrideValue
                    : hasPlanFeature && planFeature!.IsEnabled;

                int? limitValue = hasPlanFeature ? planFeature!.LimitValue : null;

                return new PlanFeatureItemDto(
                    f.Code,
                    f.Name,
                    isEnabled,
                    limitValue,
                    hasOverride);
            })
            .ToList();

        var response = new PlanFeaturesResponse(
            request.GymId,
            plan.Code,
            plan.Name,
            plan.MemberLimit,
            plan.TrainerLimit,
            plan.StaffLimit,
            featureItems);

        return Result.Success(response);
    }
}
