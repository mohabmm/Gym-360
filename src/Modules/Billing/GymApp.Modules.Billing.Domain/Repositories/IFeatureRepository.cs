using GymApp.Modules.Billing.Domain.Entities;

namespace GymApp.Modules.Billing.Domain.Repositories;

public interface IFeatureRepository
{
    Task<Feature?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Feature?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Feature>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Feature feature, CancellationToken cancellationToken = default);
    void Update(Feature feature);
}
