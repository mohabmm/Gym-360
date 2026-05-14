using GymApp.Modules.Scheduling.Application.Contracts;
using GymApp.Modules.Scheduling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Scheduling.Infrastructure.Services;

internal sealed class GymSessionPolicyService : IGymSessionPolicyService
{
    private readonly SchedulingDbContext _dbContext;

    public GymSessionPolicyService(SchedulingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GymSessionPolicyDto?> GetPolicyAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        // Read from the gyms schema using a lightweight read model.
        // This avoids cross-module project references while reading
        // the gym_session_policies table from the shared SQL Server database.
        var policy = await _dbContext.GymSessionPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GymId == gymId, cancellationToken);

        if (policy is null)
            return null;

        return new GymSessionPolicyDto(
            policy.CancellationWindowHours,
            policy.NoshowThreshold,
            policy.NoshowWindowDays,
            policy.NoshowAction);
    }
}
