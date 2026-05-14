using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gyms.Infrastructure.Repositories;

internal sealed class GymSectionRepository : IGymSectionRepository
{
    private readonly GymsDbContext _dbContext;

    public GymSectionRepository(GymsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<GymSection>> GetByGymIdAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GymSections
            .Where(s => s.GymId == gymId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(GymSection section, CancellationToken cancellationToken = default)
    {
        await _dbContext.GymSections.AddAsync(section, cancellationToken);
    }

    public void Update(GymSection section) => _dbContext.GymSections.Update(section);
    public void Remove(GymSection section) => _dbContext.GymSections.Remove(section);
}
