using GymApp.Modules.Billing.Domain.Entities;

namespace GymApp.Modules.Billing.Domain.Repositories;

public interface IGymSubscriptionRepository
{
    Task<GymSubscription?> GetByGymIdAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task AddAsync(GymSubscription subscription, CancellationToken cancellationToken = default);
    void Update(GymSubscription subscription);
}
