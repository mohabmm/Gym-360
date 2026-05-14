using GymApp.Modules.Billing.Domain.Entities;

namespace GymApp.Modules.Billing.Domain.Repositories;

public interface ISessionPackageRepository
{
    Task<SessionPackage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SessionPackage>> GetByMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SessionPackage>> GetByTrainerAsync(Guid gymId, Guid trainerId, CancellationToken cancellationToken = default);
    Task<SessionPackage?> GetActiveByMemberAndTrainerAsync(Guid gymId, Guid memberId, Guid trainerId, CancellationToken cancellationToken = default);
    Task AddAsync(SessionPackage package, CancellationToken cancellationToken = default);
    void Update(SessionPackage package);
}
