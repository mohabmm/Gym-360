using GymApp.Modules.Gyms.Domain.Entities;

namespace GymApp.Modules.Gyms.Domain.Repositories;

public interface IGymMemberRepository
{
    Task<GymMember?> GetAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
    void Update(GymMember member);
}
