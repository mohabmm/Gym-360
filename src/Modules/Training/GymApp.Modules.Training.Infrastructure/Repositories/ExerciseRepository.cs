using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Training.Infrastructure.Repositories;

internal sealed class ExerciseRepository : IExerciseRepository
{
    private readonly TrainingDbContext _dbContext;

    public ExerciseRepository(TrainingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Exercise?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Exercises
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Exercise>> GetAllAsync(
        Guid? gymId,
        ExerciseCategory? category = null,
        MuscleGroup? muscleGroup = null,
        Equipment? equipment = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Exercises
            .AsNoTracking()
            .Where(e => e.IsActive)
            .Where(e => e.Scope == ExerciseScope.Global || e.GymId == gymId);

        if (category.HasValue)
            query = query.Where(e => e.Category == category.Value);

        if (equipment.HasValue)
            query = query.Where(e => e.Equipment == equipment.Value);

        if (muscleGroup.HasValue)
        {
            var mgStr = muscleGroup.Value.ToString();
            query = query.Where(e => EF.Property<string>(e, "muscle_groups").Contains(mgStr));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(e => e.Name.Contains(searchTerm));

        return await query
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Exercise exercise, CancellationToken cancellationToken = default)
    {
        await _dbContext.Exercises.AddAsync(exercise, cancellationToken);
    }

    public void Update(Exercise exercise)
    {
        _dbContext.Exercises.Update(exercise);
    }
}
