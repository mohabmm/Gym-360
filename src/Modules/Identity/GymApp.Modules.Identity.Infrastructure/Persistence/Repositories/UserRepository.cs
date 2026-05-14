using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Identity.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _dbContext;

    public UserRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.GymMemberships)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.GymMemberships)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByPhoneAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.GymMemberships)
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        // Find the refresh_tokens row, then load the owning user (tracked, so we can mutate tokens).
        var tokenRow = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (tokenRow is null)
            return null;

        var userId = (Guid)_dbContext.Entry(tokenRow).Property("UserId").CurrentValue!;

        return await _dbContext.Users
            .Include(u => u.GymMemberships)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
    }

    public void Update(User user)
    {
        _dbContext.Users.Update(user);
    }

    public async Task<IReadOnlyList<User>> ListAsync(
        UserRole? role,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users.AsNoTracking();
        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        return await query
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, AdminRole>> GetAdminRolesForUsersAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToArray();
        if (ids.Length == 0)
            return new Dictionary<Guid, AdminRole>();

        var rows = await _dbContext.PlatformAdmins
            .AsNoTracking()
            .Where(a => ids.Contains(a.UserId))
            .Select(a => new { a.UserId, a.AdminRole })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.UserId, r => r.AdminRole);
    }

    public void Remove(User user)
    {
        _dbContext.Users.Remove(user);
    }

    public async Task AddPlatformAdminAsync(PlatformAdmin platformAdmin, CancellationToken cancellationToken = default)
    {
        await _dbContext.PlatformAdmins.AddAsync(platformAdmin, cancellationToken);
    }

    public async Task<PlatformAdmin?> GetPlatformAdminByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PlatformAdmins.FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);
    }

    public void RemovePlatformAdmin(PlatformAdmin platformAdmin)
    {
        _dbContext.PlatformAdmins.Remove(platformAdmin);
    }

    public async Task AddOwnerAsync(Owner owner, CancellationToken cancellationToken = default)
    {
        await _dbContext.Owners.AddAsync(owner, cancellationToken);
    }

    public async Task AddContractAsync(Contract contract, CancellationToken cancellationToken = default)
    {
        await _dbContext.Contracts.AddAsync(contract, cancellationToken);
    }

    public async Task<int> RemoveMembershipsForGymAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        // Raw SQL: gym_memberships is an owned table, EF would force a per-user
        // load + tracked detach. We're cleaning up after a hard gym delete, so
        // load-N-aggregates is wasted IO. ExecuteSqlRaw commits immediately —
        // no SaveChanges call needed by the caller.
        return await _dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM [identity].[user_gym_memberships] WHERE [gym_id] = {0}",
            new object[] { gymId },
            cancellationToken);
    }
}
