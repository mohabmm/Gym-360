using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gyms.Infrastructure.Repositories;

internal sealed class GymSessionPolicyRepository : IGymSessionPolicyRepository
{
    private readonly GymsDbContext _dbContext;

    public GymSessionPolicyRepository(GymsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GymSessionPolicy?> GetByGymIdAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GymSessionPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GymId == gymId, cancellationToken);
    }

    public async Task AddAsync(GymSessionPolicy policy, CancellationToken cancellationToken = default)
    {
        await _dbContext.GymSessionPolicies.AddAsync(policy, cancellationToken);
    }

    public void Update(GymSessionPolicy policy)
    {
        _dbContext.GymSessionPolicies.Update(policy);
    }
}
