using GymApp.Modules.Gyms.Domain.Entities;

namespace GymApp.Modules.Gyms.Domain.Repositories;

public interface IGymSectionRepository
{
    Task<List<GymSection>> GetByGymIdAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task AddAsync(GymSection section, CancellationToken cancellationToken = default);
    void Update(GymSection section);
    void Remove(GymSection section);
}
