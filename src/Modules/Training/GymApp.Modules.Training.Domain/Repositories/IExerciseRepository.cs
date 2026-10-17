using GymApp.Modules.Training.Domain.Entities;

namespace GymApp.Modules.Training.Domain.Repositories;

public interface IExerciseRepository
{
    Task<Exercise?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Exercise>> GetAllAsync(Guid? gymId, ExerciseFilter? filter = null, CancellationToken cancellationToken = default);
    Task AddAsync(Exercise exercise, CancellationToken cancellationToken = default);
    void Update(Exercise exercise);
}
