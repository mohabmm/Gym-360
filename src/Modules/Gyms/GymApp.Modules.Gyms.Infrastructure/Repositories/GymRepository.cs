using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using GymApp.Modules.Gyms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gyms.Infrastructure.Repositories;

internal sealed class GymRepository : IGymRepository
{
    private readonly GymsDbContext _dbContext;

    public GymRepository(GymsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Gym?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Gyms
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<Gym?> GetByCodeAsync(GymCode code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Gyms
            .FirstOrDefaultAsync(g => g.GymCode == code, cancellationToken);
    }

    public async Task<List<Gym>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Gyms
            .AsNoTracking()
            .Where(g => g.OwnerId == ownerId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Gym gym, CancellationToken cancellationToken = default)
    {
        await _dbContext.Gyms.AddAsync(gym, cancellationToken);
    }

    public void Update(Gym gym)
    {
        _dbContext.Gyms.Update(gym);
    }

    public void Remove(Gym gym)
    {
        _dbContext.Gyms.Remove(gym);
    }
}
