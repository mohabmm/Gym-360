using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Billing.Infrastructure.Repositories;

internal sealed class PlanFeatureRepository : IPlanFeatureRepository
{
    private readonly BillingDbContext _dbContext;

    public PlanFeatureRepository(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PlanFeature>> GetByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PlanFeatures
            .AsNoTracking()
            .Where(pf => pf.PlanId == planId)
            .ToListAsync(cancellationToken);
    }

    public async Task<PlanFeature?> GetByPlanAndFeatureAsync(
        Guid planId,
        Guid featureId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PlanFeatures
            .FirstOrDefaultAsync(pf => pf.PlanId == planId && pf.FeatureId == featureId, cancellationToken);
    }

    public async Task AddAsync(PlanFeature planFeature, CancellationToken cancellationToken = default)
    {
        await _dbContext.PlanFeatures.AddAsync(planFeature, cancellationToken);
    }

    public void Update(PlanFeature planFeature)
    {
        _dbContext.PlanFeatures.Update(planFeature);
    }

    public async Task DeleteByPlanAndFeatureAsync(
        Guid planId,
        Guid featureId,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.PlanFeatures
            .Where(pf => pf.PlanId == planId && pf.FeatureId == featureId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
