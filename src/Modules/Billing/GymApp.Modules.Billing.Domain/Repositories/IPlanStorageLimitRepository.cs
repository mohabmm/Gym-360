using GymApp.Modules.Billing.Domain.Entities;

namespace GymApp.Modules.Billing.Domain.Repositories;

public interface IPlanStorageLimitRepository
{
    Task<IReadOnlyList<PlanStorageLimit>> GetByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default);
    Task<PlanStorageLimit?> GetByPlanAndResourceAsync(Guid planId, string resourceType, CancellationToken cancellationToken = default);
    Task AddAsync(PlanStorageLimit limit, CancellationToken cancellationToken = default);
    void Update(PlanStorageLimit limit);
}
