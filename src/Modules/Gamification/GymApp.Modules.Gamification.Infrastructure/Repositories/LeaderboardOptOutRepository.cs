using GymApp.Modules.Gamification.Domain.Entities;
using GymApp.Modules.Gamification.Domain.Repositories;
using GymApp.Modules.Gamification.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gamification.Infrastructure.Repositories;

internal sealed class LeaderboardOptOutRepository : ILeaderboardOptOutRepository
{
    private readonly GamificationDbContext _dbContext;

    public LeaderboardOptOutRepository(GamificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LeaderboardOptOut?> GetByUserAndGymAsync(
        Guid userId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.LeaderboardOptOuts
            .FirstOrDefaultAsync(
                lo => lo.UserId == userId && lo.GymId == gymId,
                cancellationToken);
    }

    public async Task AddAsync(LeaderboardOptOut optOut, CancellationToken cancellationToken = default)
    {
        await _dbContext.LeaderboardOptOuts.AddAsync(optOut, cancellationToken);
    }

    public void Update(LeaderboardOptOut optOut)
    {
        _dbContext.LeaderboardOptOuts.Update(optOut);
    }
}
