using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;

namespace GymApp.Modules.Training.Domain.Repositories;

public interface IExerciseRepository
{
    Task<Exercise?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Exercise>> GetAllAsync(Guid? gymId, ExerciseCategory? category = null, MuscleGroup? muscleGroup = null, Equipment? equipment = null, string? searchTerm = null, CancellationToken cancellationToken = default);
    Task AddAsync(Exercise exercise, CancellationToken cancellationToken = default);
    void Update(Exercise exercise);
}
