using GymApp.Modules.Billing.Domain.Entities;

namespace GymApp.Modules.Billing.Domain.Repositories;

public interface IGymStorageUsageRepository
{
    Task<IReadOnlyList<GymStorageUsage>> GetByGymIdAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<GymStorageUsage?> GetByGymAndResourceAsync(Guid gymId, string resourceType, CancellationToken cancellationToken = default);
    Task AddAsync(GymStorageUsage usage, CancellationToken cancellationToken = default);
    void Update(GymStorageUsage usage);
}
