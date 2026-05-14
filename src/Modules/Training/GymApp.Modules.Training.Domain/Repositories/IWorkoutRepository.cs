using GymApp.Modules.Training.Domain.Entities;

namespace GymApp.Modules.Training.Domain.Repositories;

public interface IWorkoutRepository
{
    Task<Workout?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Workout>> GetByTrainerAndGymAsync(Guid trainerId, Guid gymId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Workout>> GetTemplatesAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task AddAsync(Workout workout, CancellationToken cancellationToken = default);
    void Update(Workout workout);
}
