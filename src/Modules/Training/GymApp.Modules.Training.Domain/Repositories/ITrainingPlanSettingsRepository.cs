using GymApp.Modules.Training.Domain.Entities;

namespace GymApp.Modules.Training.Domain.Repositories;

public interface ITrainingPlanSettingsRepository
{
    /// <summary>The gym's saved settings (tracked), or null when it still uses the defaults.</summary>
    Task<TrainingPlanSettings?> GetAsync(Guid gymId, CancellationToken cancellationToken = default);

    Task AddAsync(TrainingPlanSettings settings, CancellationToken cancellationToken = default);
}
