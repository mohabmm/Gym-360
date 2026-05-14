using GymApp.Modules.Training.Domain.Entities;

namespace GymApp.Modules.Training.Domain.Repositories;

public interface IWorkoutAssignmentRepository
{
    Task<WorkoutAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkoutAssignment>> GetByClientAsync(Guid clientId, Guid gymId, CancellationToken cancellationToken = default);
    Task<WorkoutAssignment?> GetActiveByTrainerAndClientAsync(Guid trainerId, Guid clientId, Guid gymId, CancellationToken cancellationToken = default);
    Task AddAsync(WorkoutAssignment assignment, CancellationToken cancellationToken = default);
    void Update(WorkoutAssignment assignment);
}
