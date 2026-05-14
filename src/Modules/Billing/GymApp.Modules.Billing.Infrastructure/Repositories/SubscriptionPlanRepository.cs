using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Billing.Infrastructure.Repositories;

internal sealed class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly BillingDbContext _dbContext;

    public SubscriptionPlanRepository(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SubscriptionPlans
            .FirstOrDefaultAsync(sp => sp.Id == id, cancellationToken);
    }

    public async Task<SubscriptionPlan?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(sp => sp.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<SubscriptionPlan>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .Where(sp => sp.IsActive)
            .OrderBy(sp => sp.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default)
    {
        await _dbContext.SubscriptionPlans.AddAsync(plan, cancellationToken);
    }

    public void Update(SubscriptionPlan plan)
    {
        _dbContext.SubscriptionPlans.Update(plan);
    }
}
