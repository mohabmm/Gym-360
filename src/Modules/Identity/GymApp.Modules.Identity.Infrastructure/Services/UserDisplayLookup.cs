using GymApp.Modules.Identity.Shared;
using GymApp.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Identity.Infrastructure.Services;

internal sealed class UserDisplayLookup : IUserDisplayLookup
{
    private readonly IdentityDbContext _dbContext;

    public UserDisplayLookup(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyDictionary<Guid, UserDisplay>> GetDisplayNamesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, UserDisplay>();
        }

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.ProfilePhotoUrl })
            .ToListAsync(cancellationToken);

        return users.ToDictionary(
            u => u.Id,
            u => new UserDisplay(u.Id, $"{u.FirstName} {u.LastName}".Trim(), u.ProfilePhotoUrl));
    }
}
