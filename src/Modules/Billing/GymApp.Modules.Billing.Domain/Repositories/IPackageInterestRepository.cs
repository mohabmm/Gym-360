using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Modules.Billing.Domain.Repositories;

public interface IPackageInterestRepository
{
    Task<PackageInterest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The member's leads in the gym, newest first (read-only).</summary>
    Task<IReadOnlyList<PackageInterest>> GetByMemberAsync(PackageOwner owner, CancellationToken cancellationToken = default);

    /// <summary>All of the gym's leads, newest first (read-only).</summary>
    Task<IReadOnlyList<PackageInterest>> GetByGymAsync(Guid gymId, CancellationToken cancellationToken = default);

    Task AddAsync(PackageInterest interest, CancellationToken cancellationToken = default);
}
