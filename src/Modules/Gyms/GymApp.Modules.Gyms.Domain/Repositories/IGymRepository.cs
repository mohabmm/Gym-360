using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Domain.Repositories;

public interface IGymRepository
{
    Task<Gym?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Gym?> GetByCodeAsync(GymCode code, CancellationToken cancellationToken = default);
    Task<List<Gym>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task AddAsync(Gym gym, CancellationToken cancellationToken = default);
    void Update(Gym gym);
    void Remove(Gym gym);
}
