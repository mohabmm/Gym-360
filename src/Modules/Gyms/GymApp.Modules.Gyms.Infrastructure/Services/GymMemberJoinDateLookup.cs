using GymApp.Modules.Gyms.Infrastructure.Data;
using GymApp.Modules.Gyms.Shared;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gyms.Infrastructure.Services;

internal sealed class GymMemberJoinDateLookup : IGymMemberJoinDateLookup
{
    private readonly GymsDbContext _dbContext;

    public GymMemberJoinDateLookup(GymsDbContext dbContext) => _dbContext = dbContext;

    public Task<DateTime?> GetJoinedAtAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default) =>
        _dbContext.GymMembers
            .AsNoTracking()
            .Where(m => m.GymId == gymId && m.MemberId == memberId)
            .Select(m => (DateTime?)m.JoinedAt)
            .FirstOrDefaultAsync(cancellationToken);
}
