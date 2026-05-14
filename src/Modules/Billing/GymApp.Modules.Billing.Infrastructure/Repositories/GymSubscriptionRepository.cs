using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Billing.Infrastructure.Repositories;

internal sealed class GymSubscriptionRepository : IGymSubscriptionRepository
{
    private readonly BillingDbContext _dbContext;

    public GymSubscriptionRepository(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GymSubscription?> GetByGymIdAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GymSubscriptions
            .FirstOrDefaultAsync(gs => gs.GymId == gymId, cancellationToken);
    }

    public async Task AddAsync(GymSubscription subscription, CancellationToken cancellationToken = default)
    {
        await _dbContext.GymSubscriptions.AddAsync(subscription, cancellationToken);
    }

    public void Update(GymSubscription subscription)
    {
        _dbContext.GymSubscriptions.Update(subscription);
    }
}
