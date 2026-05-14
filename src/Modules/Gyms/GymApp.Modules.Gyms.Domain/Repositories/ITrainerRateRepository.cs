using GymApp.Modules.Gyms.Domain.Entities;

namespace GymApp.Modules.Gyms.Domain.Repositories;

public interface ITrainerRateRepository
{
    Task<TrainerRate?> GetByTrainerAsync(Guid gymId, Guid trainerId, CancellationToken cancellationToken = default);
    Task AddAsync(TrainerRate trainerRate, CancellationToken cancellationToken = default);
    void Update(TrainerRate trainerRate);
}
