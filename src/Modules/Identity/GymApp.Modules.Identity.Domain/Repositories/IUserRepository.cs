using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.ValueObjects;

namespace GymApp.Modules.Identity.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<User?> GetByPhoneAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default);
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Email email, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    void Update(User user);

    // Admin console queries
    Task<IReadOnlyList<User>> ListAsync(
        GymApp.Modules.Identity.Domain.Enums.UserRole? role,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, GymApp.Modules.Identity.Domain.Enums.AdminRole>> GetAdminRolesForUsersAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);

    void Remove(User user);

    Task AddPlatformAdminAsync(PlatformAdmin platformAdmin, CancellationToken cancellationToken = default);

    Task<PlatformAdmin?> GetPlatformAdminByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    void RemovePlatformAdmin(PlatformAdmin platformAdmin);

    Task AddOwnerAsync(Owner owner, CancellationToken cancellationToken = default);

    Task AddContractAsync(Contract contract, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk-deletes every <c>user_gym_memberships</c> row pointing at the given
    /// gym. Used by the GymDeleted integration handler so principal users are not
    /// left with orphan memberships after a hard gym delete. Bypasses the
    /// aggregate (raw SQL) to avoid loading N users. No SaveChanges required.
    /// </summary>
    Task<int> RemoveMembershipsForGymAsync(Guid gymId, CancellationToken cancellationToken = default);
}
