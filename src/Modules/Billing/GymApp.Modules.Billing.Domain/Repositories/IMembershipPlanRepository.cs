using GymApp.Modules.Billing.Domain.Entities;

namespace GymApp.Modules.Billing.Domain.Repositories;

public interface IMembershipPlanRepository
{
    Task<MembershipPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MembershipPlan>> GetByGymIdAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task AddAsync(MembershipPlan plan, CancellationToken cancellationToken = default);
    void Update(MembershipPlan plan);
}
