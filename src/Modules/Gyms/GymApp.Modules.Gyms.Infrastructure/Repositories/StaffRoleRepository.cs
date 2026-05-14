using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gyms.Infrastructure.Repositories;

internal sealed class StaffRoleRepository : IStaffRoleRepository
{
    private readonly GymsDbContext _dbContext;

    public StaffRoleRepository(GymsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<StaffRole>> GetByGymIdAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StaffRoles
            .AsNoTracking()
            .Where(s => s.GymId == gymId)
            .ToListAsync(cancellationToken);
    }

    public async Task<StaffRole?> GetByUserAndGymAsync(Guid userId, Guid gymId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StaffRoles
            .FirstOrDefaultAsync(s => s.UserId == userId && s.GymId == gymId, cancellationToken);
    }

    public async Task AddAsync(StaffRole staffRole, CancellationToken cancellationToken = default)
    {
        await _dbContext.StaffRoles.AddAsync(staffRole, cancellationToken);
    }

    public void Update(StaffRole staffRole)
    {
        _dbContext.StaffRoles.Update(staffRole);
    }

    public void Remove(StaffRole staffRole)
    {
        _dbContext.StaffRoles.Remove(staffRole);
    }
}
