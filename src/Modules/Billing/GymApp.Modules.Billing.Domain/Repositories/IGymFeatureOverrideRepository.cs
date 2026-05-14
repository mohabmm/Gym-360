using GymApp.Modules.Billing.Domain.Entities;

namespace GymApp.Modules.Billing.Domain.Repositories;

public interface IGymFeatureOverrideRepository
{
    Task<IReadOnlyList<GymFeatureOverride>> GetByGymIdAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<GymFeatureOverride?> GetByGymAndFeatureAsync(Guid gymId, Guid featureId, CancellationToken cancellationToken = default);
    Task AddAsync(GymFeatureOverride gymFeatureOverride, CancellationToken cancellationToken = default);
    void Update(GymFeatureOverride gymFeatureOverride);
    void Delete(GymFeatureOverride gymFeatureOverride);
}
