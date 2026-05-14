using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Training.Infrastructure.Repositories;

internal sealed class WorkoutRepository : IWorkoutRepository
{
    private readonly TrainingDbContext _dbContext;

    public WorkoutRepository(TrainingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Workout?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Workouts
            .Include(w => w.WorkoutExercises.OrderBy(we => we.Order))
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Workout>> GetByTrainerAndGymAsync(
        Guid trainerId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Workouts
            .AsNoTracking()
            .Include(w => w.WorkoutExercises.OrderBy(we => we.Order))
            .Where(w => w.TrainerId == trainerId && w.GymId == gymId)
            .OrderByDescending(w => w.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Workout>> GetTemplatesAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Workouts
            .AsNoTracking()
            .Include(w => w.WorkoutExercises.OrderBy(we => we.Order))
            .Where(w => w.GymId == gymId && w.IsTemplate)
            .OrderByDescending(w => w.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Workout workout, CancellationToken cancellationToken = default)
    {
        await _dbContext.Workouts.AddAsync(workout, cancellationToken);
    }

    public void Update(Workout workout)
    {
        _dbContext.Workouts.Update(workout);
    }
}
