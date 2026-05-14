using GymApp.Modules.Billing.Domain.Entities;

namespace GymApp.Modules.Billing.Domain.Repositories;

public interface IPlanChangeLogRepository
{
    Task<IReadOnlyList<PlanChangeLog>> GetByGymIdAsync(Guid gymId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlanChangeLog>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(PlanChangeLog log, CancellationToken cancellationToken = default);
}
