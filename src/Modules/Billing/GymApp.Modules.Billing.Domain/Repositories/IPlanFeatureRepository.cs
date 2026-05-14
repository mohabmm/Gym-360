using GymApp.Modules.Billing.Domain.Entities;

namespace GymApp.Modules.Billing.Domain.Repositories;

public interface IPlanFeatureRepository
{
    Task<IReadOnlyList<PlanFeature>> GetByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default);
    Task<PlanFeature?> GetByPlanAndFeatureAsync(Guid planId, Guid featureId, CancellationToken cancellationToken = default);
    Task AddAsync(PlanFeature planFeature, CancellationToken cancellationToken = default);
    void Update(PlanFeature planFeature);
    Task DeleteByPlanAndFeatureAsync(Guid planId, Guid featureId, CancellationToken cancellationToken = default);
}
