using GymApp.Modules.Gyms.Shared;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Infrastructure.Services;

internal sealed class GymMembershipChecker : IGymMembershipChecker
{
    private readonly GymsDbContext _dbContext;

    public GymMembershipChecker(GymsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsActiveMemberAsync(
        Guid gymId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var gym = await _dbContext.Gyms
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gymId, cancellationToken);

        return gym is not null && gym.MemberIds.Contains(new MemberId(userId));
    }

    public async Task<bool> IsGymSuspendedAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        var status = await _dbContext.Gyms
            .AsNoTracking()
            .Where(g => g.Id == gymId)
            .Select(g => (GymStatus?)g.Status)
            .FirstOrDefaultAsync(cancellationToken);

        return status == GymStatus.Suspended;
    }

    public async Task<bool> IsMemberFrozenAsync(
        Guid gymId,
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.GymMembers
            .AsNoTracking()
            .AnyAsync(m => m.GymId == gymId && m.MemberId == memberId && m.IsFrozen, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetMemberIdsAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        var gym = await _dbContext.Gyms
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gymId, cancellationToken);

        return gym is null ? Array.Empty<Guid>() : gym.MemberIds.Select(id => id.Value).ToArray();
    }
}
