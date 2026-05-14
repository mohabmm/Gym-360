using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Modules.Billing.Application.Queries.Features;

internal sealed class GetFeatureMatrixQueryHandler : IQueryHandler<GetFeatureMatrixQuery, FeatureMatrixResponse>
{
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IFeatureRepository _featureRepository;
    private readonly IPlanFeatureRepository _planFeatureRepository;

    public GetFeatureMatrixQueryHandler(
        ISubscriptionPlanRepository planRepository,
        IFeatureRepository featureRepository,
        IPlanFeatureRepository planFeatureRepository)
    {
        _planRepository = planRepository;
        _featureRepository = featureRepository;
        _planFeatureRepository = planFeatureRepository;
    }

    public async Task<Result<FeatureMatrixResponse>> Handle(GetFeatureMatrixQuery request, CancellationToken cancellationToken)
    {
        var plans = await _planRepository.GetAllActiveAsync(cancellationToken);
        var features = await _featureRepository.GetAllAsync(cancellationToken);

        var planDtos = plans.Select(p => new SubscriptionPlanDto(
            p.Id, p.Code, p.Name, p.Description,
            p.MonthlyPriceEgp, p.AnnualPriceEgp,
            p.MemberLimit, p.TrainerLimit, p.StaffLimit,
            p.IsActive, p.IsPublic, p.SortOrder,
            p.CreatedAt, p.UpdatedAt)).ToList();

        var featureDtos = features.Select(f => new FeatureDto(
            f.Id, f.Code, f.Name, f.Description,
            f.Category, f.IsActive, f.SortOrder)).ToList();

        var matrixEntries = new List<PlanFeatureEntryDto>();

        foreach (var plan in plans)
        {
            var planFeatures = await _planFeatureRepository.GetByPlanIdAsync(plan.Id, cancellationToken);

            matrixEntries.AddRange(planFeatures.Select(pf => new PlanFeatureEntryDto(
                pf.PlanId, pf.FeatureId, pf.IsEnabled, pf.LimitValue)));
        }

        var response = new FeatureMatrixResponse(planDtos, featureDtos, matrixEntries);

        return Result.Success(response);
    }
}
