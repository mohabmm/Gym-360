using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.Enums;
using GymApp.Modules.Progress.Domain.Repositories;
using GymApp.Modules.Progress.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Progress.Infrastructure.Repositories;

internal sealed class StreakRepository : IStreakRepository
{
    private readonly ProgressDbContext _dbContext;

    public StreakRepository(ProgressDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Streak?> GetByUserAndTypeAsync(
        Guid userId,
        Guid gymId,
        StreakType streakType,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Streaks
            .FirstOrDefaultAsync(
                s => s.UserId == userId && s.GymId == gymId && s.StreakType == streakType,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Streak>> GetAllByUserAsync(
        Guid userId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Streaks
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.GymId == gymId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Streak streak, CancellationToken cancellationToken = default)
    {
        await _dbContext.Streaks.AddAsync(streak, cancellationToken);
    }

    public void Update(Streak streak)
    {
        _dbContext.Streaks.Update(streak);
    }
}
