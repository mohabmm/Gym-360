using GymApp.Modules.Gyms.Domain.Entities;

namespace GymApp.Modules.Gyms.Domain.Repositories;

public interface IGymClassRepository
{
    Task<GymClass?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<GymClass>> GetByGymIdAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task AddAsync(GymClass gymClass, CancellationToken cancellationToken = default);
    void Update(GymClass gymClass);
}
