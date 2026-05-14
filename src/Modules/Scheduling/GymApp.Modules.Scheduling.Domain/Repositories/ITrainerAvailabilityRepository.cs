using GymApp.Modules.Scheduling.Domain.Entities;

namespace GymApp.Modules.Scheduling.Domain.Repositories;

public interface ITrainerAvailabilityRepository
{
    Task<TrainerAvailability?> GetByTrainerAndGymAsync(
        Guid trainerId,
        Guid gymId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TrainerAvailability availability,
        CancellationToken cancellationToken = default);

    void Update(TrainerAvailability availability);
}
