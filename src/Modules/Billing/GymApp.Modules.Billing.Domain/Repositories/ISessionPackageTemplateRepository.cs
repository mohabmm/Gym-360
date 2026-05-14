using GymApp.Modules.Billing.Domain.Entities;

namespace GymApp.Modules.Billing.Domain.Repositories;

public interface ISessionPackageTemplateRepository
{
    Task<SessionPackageTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SessionPackageTemplate>> GetByGymIdAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task AddAsync(SessionPackageTemplate template, CancellationToken cancellationToken = default);
    void Update(SessionPackageTemplate template);
}
