using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Identity.Infrastructure.Services;

internal sealed class GymMemberLookup : IGymMemberLookup
{
    private readonly IdentityDbContext _dbContext;

    public GymMemberLookup(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid?> GetUserIdByMemberNumberAsync(Guid gymId, string memberNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(memberNumber)) return null;

        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.GymMemberships.Any(m =>
                m.GymId == gymId &&
                m.MemberNumber == memberNumber &&
                m.IsActive))
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return user;
    }
}
