using GymApp.Modules.Gyms.Domain.Entities;

namespace GymApp.Modules.Gyms.Domain.Repositories;

public interface IGymSessionPolicyRepository
{
    Task<GymSessionPolicy?> GetByGymIdAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task AddAsync(GymSessionPolicy policy, CancellationToken cancellationToken = default);
    void Update(GymSessionPolicy policy);
}
