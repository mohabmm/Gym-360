using GymApp.Modules.Gyms.Domain.Entities;

namespace GymApp.Modules.Gyms.Domain.Repositories;

public interface IMemberOnboardingRepository
{
    Task<MemberOnboarding?> GetByMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
    Task AddAsync(MemberOnboarding onboarding, CancellationToken cancellationToken = default);
    void Update(MemberOnboarding onboarding);
}
