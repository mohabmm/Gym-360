using GymApp.Modules.Gyms.Domain.Entities;

namespace GymApp.Modules.Gyms.Domain.Repositories;

public interface IGymSeasonalHoursRepository
{
    Task<List<GymSeasonalHours>> GetByGymIdAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task AddAsync(GymSeasonalHours hours, CancellationToken cancellationToken = default);
    void Update(GymSeasonalHours hours);
    void Remove(GymSeasonalHours hours);
}
