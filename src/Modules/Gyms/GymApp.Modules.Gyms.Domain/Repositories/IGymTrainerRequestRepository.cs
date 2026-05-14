using GymApp.Modules.Gyms.Domain.Entities;

namespace GymApp.Modules.Gyms.Domain.Repositories;

public interface IGymTrainerRequestRepository
{
    Task<GymTrainerRequest?> GetAsync(Guid gymId, Guid trainerId, CancellationToken cancellationToken = default);
    Task AddAsync(GymTrainerRequest request, CancellationToken cancellationToken = default);
    void Update(GymTrainerRequest request);
}
