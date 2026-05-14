using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Billing.Infrastructure.Repositories;

internal sealed class PlanChangeLogRepository : IPlanChangeLogRepository
{
    private readonly BillingDbContext _dbContext;

    public PlanChangeLogRepository(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PlanChangeLog>> GetByGymIdAsync(
        Guid gymId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PlanChangeLogs
            .AsNoTracking()
            .Where(pcl => pcl.GymId == gymId)
            .OrderByDescending(pcl => pcl.ChangedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlanChangeLog>> GetAllAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PlanChangeLogs
            .AsNoTracking()
            .OrderByDescending(pcl => pcl.ChangedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PlanChangeLog log, CancellationToken cancellationToken = default)
    {
        await _dbContext.PlanChangeLogs.AddAsync(log, cancellationToken);
    }
}
