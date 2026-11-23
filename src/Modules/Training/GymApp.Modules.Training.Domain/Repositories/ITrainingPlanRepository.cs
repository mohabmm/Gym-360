using GymApp.Modules.Training.Domain.Entities;

namespace GymApp.Modules.Training.Domain.Repositories;

public interface ITrainingPlanRepository
{
    Task<TrainingPlan?> GetByIdAsync(Guid gymId, Guid planId, CancellationToken cancellationToken = default);

    Task<TrainingPlan?> GetActiveForTraineeAsync(Guid gymId, Guid traineeId, CancellationToken cancellationToken = default);

    /// <summary>Every plan of the trainee at this gym, newest first (read-only).</summary>
    Task<IReadOnlyList<TrainingPlan>> ListForTraineeAsync(Guid gymId, Guid traineeId, CancellationToken cancellationToken = default);

    /// <summary>Active plans of these trainees (read-only).</summary>
    Task<IReadOnlyList<TrainingPlan>> ListActiveForTraineesAsync(Guid gymId, IReadOnlyCollection<Guid> traineeIds, CancellationToken cancellationToken = default);

    Task AddAsync(TrainingPlan plan, CancellationToken cancellationToken = default);
}
