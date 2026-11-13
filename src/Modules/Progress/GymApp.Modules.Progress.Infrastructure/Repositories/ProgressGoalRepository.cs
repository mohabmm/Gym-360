using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.Repositories;
using GymApp.Modules.Progress.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Progress.Infrastructure.Repositories;

internal sealed class ProgressGoalRepository : IProgressGoalRepository
{
    private readonly ProgressDbContext _dbContext;

    public ProgressGoalRepository(ProgressDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ProgressGoal?> GetAsync(Guid userId, Guid gymId, CancellationToken cancellationToken = default) =>
        _dbContext.ProgressGoals
            .FirstOrDefaultAsync(g => g.UserId == userId && g.GymId == gymId, cancellationToken);

    public Task<ProgressGoal?> FindAsync(Guid userId, Guid gymId, CancellationToken cancellationToken = default) =>
        _dbContext.ProgressGoals
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.UserId == userId && g.GymId == gymId, cancellationToken);

    public async Task AddAsync(ProgressGoal goal, CancellationToken cancellationToken = default)
    {
        await _dbContext.ProgressGoals.AddAsync(goal, cancellationToken);
    }
}
