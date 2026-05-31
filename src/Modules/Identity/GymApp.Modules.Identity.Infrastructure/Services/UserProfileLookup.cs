using GymApp.Modules.Identity.Shared;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Identity.Infrastructure.Services;

internal sealed class UserProfileLookup : IUserProfileLookup
{
    private readonly IdentityDbContext _dbContext;

    public UserProfileLookup(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserProfileInfo?> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                Email = u.Email.Value,
                Phone = u.PhoneNumber.Value,
                u.FirstName,
                u.LastName,
                u.ProfilePhotoUrl,
                u.Role,
                u.Status,
                u.NationalId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return null;

        return new UserProfileInfo(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Phone,
            user.ProfilePhotoUrl,
            user.Role.ToRoleClaim(),
            user.Status.ToString().ToLowerInvariant(),
            user.NationalId);
    }

    public async Task<IReadOnlyDictionary<Guid, UserProfileInfo>> GetProfilesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToArray();
        if (ids.Length == 0)
            return new Dictionary<Guid, UserProfileInfo>();

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                Email = u.Email.Value,
                Phone = u.PhoneNumber.Value,
                u.FirstName,
                u.LastName,
                u.ProfilePhotoUrl,
                u.Role,
                u.Status,
                u.NationalId
            })
            .ToListAsync(cancellationToken);

        return users.ToDictionary(
            u => u.Id,
            u => new UserProfileInfo(
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.Phone,
                u.ProfilePhotoUrl,
                u.Role.ToRoleClaim(),
                u.Status.ToString().ToLowerInvariant(),
                u.NationalId));
    }

    public async Task<IReadOnlyList<Guid>> FilterProfileIdsAsync(
        IReadOnlyCollection<Guid> candidateIds,
        string? search,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var ids = candidateIds.Distinct().ToArray();
        if (ids.Length == 0)
            return Array.Empty<Guid>();

        var query = _dbContext.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id));

        // Status is value-converted to a string column, so an enum equality predicate
        // translates to SQL cleanly. Map the staff UI's member-status vocabulary onto
        // UserStatus: "frozen" is a gym-directory flag handled by the caller (never
        // reaches here); "expired" has no UserStatus backing (no profile constraint).
        if (!string.IsNullOrWhiteSpace(status))
        {
            switch (status.Trim().ToLowerInvariant())
            {
                case "active":
                    query = query.Where(u => u.Status == UserStatus.Active);
                    break;
                case "inactive":
                    query = query.Where(u => u.Status != UserStatus.Active && u.Status != UserStatus.Deleted);
                    break;
            }
        }

        var term = search?.Trim();
        if (string.IsNullOrEmpty(term))
            return await query.Select(u => u.Id).ToListAsync(cancellationToken);

        // Email/PhoneNumber are value-converted (HasConversion in UserConfiguration), so
        // `.Value` only translates inside a PROJECTION, never inside a WHERE/LIKE. Project
        // the candidate rows (bounded to this gym's members) — mirroring GetProfilesAsync —
        // and match the search term in memory.
        var rows = await query
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                Email = u.Email.Value,
                Phone = u.PhoneNumber.Value,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Where(r =>
                (r.FirstName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.LastName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Phone?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
            .Select(r => r.Id)
            .ToList();
    }
}
