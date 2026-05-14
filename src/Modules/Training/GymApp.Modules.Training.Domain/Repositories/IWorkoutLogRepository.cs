using GymApp.Modules.Training.Domain.Entities;

namespace GymApp.Modules.Training.Domain.Repositories;

public interface IWorkoutLogRepository
{
    Task<WorkoutLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkoutLog>> GetByUserAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<WorkoutLog?> GetByUserAndDateAsync(Guid userId, DateOnly date, Guid? workoutId = null, CancellationToken cancellationToken = default);
    Task AddAsync(WorkoutLog workoutLog, CancellationToken cancellationToken = default);
    void Update(WorkoutLog workoutLog);
}
