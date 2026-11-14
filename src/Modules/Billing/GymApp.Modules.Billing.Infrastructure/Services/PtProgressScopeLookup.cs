using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Infrastructure.Data;
using GymApp.Modules.Billing.Shared;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Billing.Infrastructure.Services;

internal sealed class PtProgressScopeLookup : IPtProgressScopeLookup
{
    private readonly BillingDbContext _dbContext;

    public PtProgressScopeLookup(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PtProgressScope> GetScopeAsync(
        Guid memberId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        var packages = await _dbContext.SessionPackages
            .AsNoTracking()
            .Where(p => p.MemberId == memberId && p.GymId == gymId)
            .Select(p => new { p.Id, p.IsTrial, p.TotalSessions })
            .ToListAsync(cancellationToken);

        var trialPackageIds = packages.Where(p => p.IsTrial).Select(p => p.Id).ToList();
        var trialSessionIds = await CountedSessionsAsync(trialPackageIds, cancellationToken);

        return new PtProgressScope(
            trialSessionIds,
            packages.Where(p => p.IsTrial).Sum(p => p.TotalSessions),
            packages.Any(p => !p.IsTrial));
    }

    // A session counts when it was used (completed) more times than it was reversed.
    private async Task<IReadOnlyCollection<Guid>> CountedSessionsAsync(
        List<Guid> packageIds,
        CancellationToken cancellationToken)
    {
        if (packageIds.Count == 0)
            return Array.Empty<Guid>();

        var usages = await _dbContext.PackageUsages
            .AsNoTracking()
            .Where(u => packageIds.Contains(u.SessionPackageId)
                && (u.UsageType == PackageUsageType.Completed || u.UsageType == PackageUsageType.Reversed))
            .Select(u => new { u.SessionId, u.UsageType })
            .ToListAsync(cancellationToken);

        return usages
            .GroupBy(u => u.SessionId)
            .Where(g => g.Count(u => u.UsageType == PackageUsageType.Completed)
                > g.Count(u => u.UsageType == PackageUsageType.Reversed))
            .Select(g => g.Key)
            .ToHashSet();
    }
}
