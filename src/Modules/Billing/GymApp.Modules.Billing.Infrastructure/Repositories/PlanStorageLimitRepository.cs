using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Billing.Infrastructure.Repositories;

internal sealed class PlanStorageLimitRepository : IPlanStorageLimitRepository
{
    private readonly BillingDbContext _dbContext;

    public PlanStorageLimitRepository(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PlanStorageLimit>> GetByPlanIdAsync(
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PlanStorageLimits
            .AsNoTracking()
            .Where(psl => psl.PlanId == planId)
            .ToListAsync(cancellationToken);
    }

    public async Task<PlanStorageLimit?> GetByPlanAndResourceAsync(
        Guid planId,
        string resourceType,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PlanStorageLimits
            .AsNoTracking()
            .FirstOrDefaultAsync(psl => psl.PlanId == planId && psl.ResourceType == resourceType, cancellationToken);
    }

    public async Task AddAsync(PlanStorageLimit limit, CancellationToken cancellationToken = default)
    {
        await _dbContext.PlanStorageLimits.AddAsync(limit, cancellationToken);
    }

    public void Update(PlanStorageLimit limit)
    {
        _dbContext.PlanStorageLimits.Update(limit);
    }
}
