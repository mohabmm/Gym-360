using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gyms.Infrastructure.Repositories;

internal sealed class GymSeasonalHoursRepository : IGymSeasonalHoursRepository
{
    private readonly GymsDbContext _dbContext;

    public GymSeasonalHoursRepository(GymsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<GymSeasonalHours>> GetByGymIdAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GymSeasonalHours
            .Where(h => h.GymId == gymId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(GymSeasonalHours hours, CancellationToken cancellationToken = default)
    {
        await _dbContext.GymSeasonalHours.AddAsync(hours, cancellationToken);
    }

    public void Update(GymSeasonalHours hours) => _dbContext.GymSeasonalHours.Update(hours);
    public void Remove(GymSeasonalHours hours) => _dbContext.GymSeasonalHours.Remove(hours);
}
