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

        if (!string.IsNullOrWhiteSpace(searchTerm))
            // Match either language: Arabic-UI users search by the Arabic name.
            query = query.Where(e =>
                e.Name.Contains(searchTerm) || (e.NameAr != null && e.NameAr.Contains(searchTerm)));

        var exercises = await query
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

        // MuscleGroups is persisted as a comma-joined string via a value
        // converter, so EF cannot translate a server-side filter on it — the
        // previous EF.Property<string>(e, "muscle_groups") referenced the
        // column name (not a model property) and threw at query translation.
        // Filter in memory after materialisation; the handler already
        // paginates in memory, so this changes no observable behaviour.
        if (muscleGroup.HasValue)
            return exercises
                .Where(e => e.MuscleGroups.Contains(muscleGroup.Value))
                .ToList();

        return exercises;
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
