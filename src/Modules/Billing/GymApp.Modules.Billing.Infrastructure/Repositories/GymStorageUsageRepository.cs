using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Billing.Infrastructure.Repositories;

internal sealed class GymStorageUsageRepository : IGymStorageUsageRepository
{
    private readonly BillingDbContext _dbContext;

    public GymStorageUsageRepository(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<GymStorageUsage>> GetByGymIdAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.GymStorageUsages
            .AsNoTracking()
            .Where(gsu => gsu.GymId == gymId)
            .ToListAsync(cancellationToken);
    }

    public async Task<GymStorageUsage?> GetByGymAndResourceAsync(
        Guid gymId,
        string resourceType,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.GymStorageUsages
            .FirstOrDefaultAsync(gsu => gsu.GymId == gymId && gsu.ResourceType == resourceType, cancellationToken);
    }

    public async Task AddAsync(GymStorageUsage usage, CancellationToken cancellationToken = default)
    {
        await _dbContext.GymStorageUsages.AddAsync(usage, cancellationToken);
    }

    public void Update(GymStorageUsage usage)
    {
        _dbContext.GymStorageUsages.Update(usage);
    }
}
