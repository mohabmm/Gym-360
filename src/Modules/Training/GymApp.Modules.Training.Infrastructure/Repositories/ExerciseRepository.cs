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
        ExerciseFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        filter ??= ExerciseFilter.None;

        var query = _dbContext.Exercises
            .AsNoTracking()
            .Where(e => e.IsActive)
            .Where(e => e.Scope == ExerciseScope.Global || e.GymId == gymId);

        var exercises = await ApplyServerSideFilters(query, filter)
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

        var muscleGroup = filter.MuscleGroup;

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

    private static IQueryable<Exercise> ApplyServerSideFilters(IQueryable<Exercise> query, ExerciseFilter filter)
    {
        if (filter.Category is { } category)
            query = query.Where(e => e.Category == category);

        if (filter.Equipment is { } equipment)
            query = query.Where(e => e.Equipment == equipment);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            query = WhereNameMatches(query, filter.SearchTerm);

        return query;
    }

    // Match either language: Arabic-UI users search by the Arabic name.
    private static IQueryable<Exercise> WhereNameMatches(IQueryable<Exercise> query, string searchTerm) =>
        query.Where(e => e.Name.Contains(searchTerm) || (e.NameAr != null && e.NameAr.Contains(searchTerm)));

    public async Task AddAsync(Exercise exercise, CancellationToken cancellationToken = default)
    {
        await _dbContext.Exercises.AddAsync(exercise, cancellationToken);
    }

    public void Update(Exercise exercise)
    {
        _dbContext.Exercises.Update(exercise);
    }
}
