using GymApp.Modules.Billing.Domain.ValueObjects;
using GymApp.Modules.Billing.Domain.Entities;

namespace GymApp.Modules.Billing.Domain.Repositories;

public interface ISessionPackageRepository
{
    Task<SessionPackage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SessionPackage>> GetByGymAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SessionPackage>> GetByMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SessionPackage>> GetByTrainerAsync(Guid gymId, Guid trainerId, CancellationToken cancellationToken = default);
    Task<SessionPackage?> GetActiveByMemberAndTrainerAsync(PackageParticipants participants, CancellationToken cancellationToken = default);
    /// <summary>All of the gym's packages with their usage history, read-only (PT reporting).</summary>
    Task<IReadOnlyList<SessionPackage>> GetByGymWithUsageAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<bool> HasTrialAsync(PackageOwner owner, CancellationToken cancellationToken = default);
    Task<SessionPackage?> FindByUsageSessionAsync(PackageSessionRef session, CancellationToken cancellationToken = default);
    Task AddAsync(SessionPackage package, CancellationToken cancellationToken = default);
    void Update(SessionPackage package);
}
