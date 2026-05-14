using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gyms.Infrastructure.Repositories;

internal sealed class CheckInRepository : ICheckInRepository
{
    private readonly GymsDbContext _dbContext;

    public CheckInRepository(GymsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<CheckIn>> GetByGymIdAsync(Guid gymId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CheckIns
            .AsNoTracking()
            .Where(c => c.GymId == gymId && c.CheckedInAt >= from && c.CheckedInAt <= to)
            .OrderByDescending(c => c.CheckedInAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<CheckIn?> GetLatestForMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CheckIns
            .AsNoTracking()
            .Where(c => c.GymId == gymId && c.MemberId == memberId)
            .OrderByDescending(c => c.CheckedInAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(CheckIn checkIn, CancellationToken cancellationToken = default)
    {
        await _dbContext.CheckIns.AddAsync(checkIn, cancellationToken);
    }

    public async Task<int> GetCountAsync(Guid gymId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CheckIns
            .AsNoTracking()
            .CountAsync(c => c.GymId == gymId && c.CheckedInAt >= from && c.CheckedInAt <= to, cancellationToken);
    }
}
