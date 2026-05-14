using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Billing.Infrastructure.Repositories;

internal sealed class GymFeatureOverrideRepository : IGymFeatureOverrideRepository
{
    private readonly BillingDbContext _dbContext;

    public GymFeatureOverrideRepository(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<GymFeatureOverride>> GetByGymIdAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.GymFeatureOverrides
            .AsNoTracking()
            .Where(gfo => gfo.GymId == gymId)
            .ToListAsync(cancellationToken);
    }

    public async Task<GymFeatureOverride?> GetByGymAndFeatureAsync(
        Guid gymId,
        Guid featureId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.GymFeatureOverrides
            .FirstOrDefaultAsync(gfo => gfo.GymId == gymId && gfo.FeatureId == featureId, cancellationToken);
    }

    public async Task AddAsync(GymFeatureOverride gymFeatureOverride, CancellationToken cancellationToken = default)
    {
        await _dbContext.GymFeatureOverrides.AddAsync(gymFeatureOverride, cancellationToken);
    }

    public void Update(GymFeatureOverride gymFeatureOverride)
    {
        _dbContext.GymFeatureOverrides.Update(gymFeatureOverride);
    }

    public void Delete(GymFeatureOverride gymFeatureOverride)
    {
        _dbContext.GymFeatureOverrides.Remove(gymFeatureOverride);
    }
}
