using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gyms.Infrastructure.Repositories;

internal sealed class MemberOnboardingRepository : IMemberOnboardingRepository
{
    private readonly GymsDbContext _dbContext;

    public MemberOnboardingRepository(GymsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MemberOnboarding?> GetByMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MemberOnboardings
            .FirstOrDefaultAsync(o => o.GymId == gymId && o.MemberId == memberId, cancellationToken);
    }

    public async Task AddAsync(MemberOnboarding onboarding, CancellationToken cancellationToken = default)
    {
        await _dbContext.MemberOnboardings.AddAsync(onboarding, cancellationToken);
    }

    public void Update(MemberOnboarding onboarding)
    {
        _dbContext.MemberOnboardings.Update(onboarding);
    }
}
