using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Modules.Billing.Application.Queries.PlanFeatures;

/// <summary>The feature catalog and one plan's feature rows (limits), read together.</summary>
internal sealed class PlanFeatureCatalog
{
    private readonly IFeatureRepository _features;
    private readonly IPlanFeatureRepository _planFeatures;

    public PlanFeatureCatalog(IFeatureRepository features, IPlanFeatureRepository planFeatures)
    {
        _features = features;
        _planFeatures = planFeatures;
    }

    public async Task<(IReadOnlyList<Feature> Features, IReadOnlyList<PlanFeature> PlanFeatures)> LoadAsync(
        Guid planId, CancellationToken cancellationToken)
    {
        var features = await _features.GetAllAsync(cancellationToken);
        var planFeatures = await _planFeatures.GetByPlanIdAsync(planId, cancellationToken);
        return (features, planFeatures);
    }
}
