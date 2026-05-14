using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Billing.Infrastructure.Repositories;

internal sealed class MembershipPlanRepository : IMembershipPlanRepository
{
    private readonly BillingDbContext _dbContext;

    public MembershipPlanRepository(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MembershipPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MembershipPlans
            .FirstOrDefaultAsync(mp => mp.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<MembershipPlan>> GetByGymIdAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MembershipPlans
            .AsNoTracking()
            .Where(mp => mp.GymId == gymId)
            .OrderBy(mp => mp.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(MembershipPlan plan, CancellationToken cancellationToken = default)
    {
        await _dbContext.MembershipPlans.AddAsync(plan, cancellationToken);
    }

    public void Update(MembershipPlan plan)
    {
        _dbContext.MembershipPlans.Update(plan);
    }
}
