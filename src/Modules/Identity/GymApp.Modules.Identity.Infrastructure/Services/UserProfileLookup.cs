using GymApp.Common.Application.Abstractions;
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
                u.Status
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
            user.Status.ToString().ToLowerInvariant());
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
                u.Status
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
                u.Status.ToString().ToLowerInvariant()));
    }
}
