using GymApp.Modules.Gamification.Domain.Entities;
using GymApp.Modules.Gamification.Domain.Enums;
using GymApp.Modules.Gamification.Domain.Repositories;
using GymApp.Modules.Gamification.Domain.ValueObjects;
using GymApp.Modules.Gamification.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gamification.Infrastructure.Repositories;

internal sealed class MemberLeaderboardRepository : IMemberLeaderboardRepository
{
    private readonly GamificationDbContext _dbContext;

    public MemberLeaderboardRepository(GamificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MemberLeaderboard?> GetByGymAndTypeAsync(
        Guid gymId,
        LeaderboardType type,
        LeaderboardPeriod period,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.MemberLeaderboards
            .Include(ml => ml.Entries.OrderBy(e => e.Rank))
            .FirstOrDefaultAsync(
                ml => ml.GymId == gymId
                    && ml.LeaderboardType == type
                    && ml.Period.Month == period.Month
                    && ml.Period.Year == period.Year,
                cancellationToken);
    }

    public async Task AddAsync(MemberLeaderboard leaderboard, CancellationToken cancellationToken = default)
    {
        await _dbContext.MemberLeaderboards.AddAsync(leaderboard, cancellationToken);
    }

    public void Update(MemberLeaderboard leaderboard)
    {
        _dbContext.MemberLeaderboards.Update(leaderboard);
    }
}
