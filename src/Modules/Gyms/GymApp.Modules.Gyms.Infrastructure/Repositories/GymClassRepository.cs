using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gyms.Infrastructure.Repositories;

internal sealed class GymClassRepository : IGymClassRepository
{
    private readonly GymsDbContext _dbContext;

    public GymClassRepository(GymsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GymClass?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GymClasses
            .Include(c => c.Bookings)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<List<GymClass>> GetByGymIdAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GymClasses
            .AsNoTracking()
            .Include(c => c.Bookings)
            .Where(c => c.GymId == gymId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(GymClass gymClass, CancellationToken cancellationToken = default)
    {
        await _dbContext.GymClasses.AddAsync(gymClass, cancellationToken);
    }

    public void Update(GymClass gymClass)
    {
        _dbContext.GymClasses.Update(gymClass);
    }
}
