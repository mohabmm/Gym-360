using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gyms.Infrastructure.Repositories;

internal sealed class GymMemberRepository : IGymMemberRepository
{
    private readonly GymsDbContext _dbContext;

    public GymMemberRepository(GymsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GymMember?> GetAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default)
        => await _dbContext.GymMembers
            .FirstOrDefaultAsync(m => m.GymId == gymId && m.MemberId == memberId, cancellationToken);

    public void Update(GymMember member)
        => _dbContext.GymMembers.Update(member);
}
