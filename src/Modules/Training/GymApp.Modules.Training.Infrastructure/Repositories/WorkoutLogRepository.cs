using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Training.Infrastructure.Repositories;

internal sealed class WorkoutLogRepository : IWorkoutLogRepository
{
    private readonly TrainingDbContext _dbContext;

    public WorkoutLogRepository(TrainingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WorkoutLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkoutLogs
            .Include(wl => wl.LogSets.OrderBy(ls => ls.SetNumber))
            .FirstOrDefaultAsync(wl => wl.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkoutLog>> GetByUserAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkoutLogs
            .AsNoTracking()
            .Include(wl => wl.LogSets.OrderBy(ls => ls.SetNumber))
            .Where(wl => wl.UserId == userId && wl.Date >= startDate && wl.Date <= endDate)
            .OrderByDescending(wl => wl.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkoutLog?> GetByUserAndDateAsync(
        Guid userId,
        DateOnly date,
        Guid? workoutId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.WorkoutLogs
            .AsNoTracking()
            .Include(wl => wl.LogSets.OrderBy(ls => ls.SetNumber))
            .Where(wl => wl.UserId == userId && wl.Date == date);

        if (workoutId.HasValue)
            query = query.Where(wl => wl.WorkoutId == workoutId.Value);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(WorkoutLog workoutLog, CancellationToken cancellationToken = default)
    {
        await _dbContext.WorkoutLogs.AddAsync(workoutLog, cancellationToken);
    }

    public void Update(WorkoutLog workoutLog)
    {
        _dbContext.WorkoutLogs.Update(workoutLog);
    }
}
