using System.Linq.Expressions;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;
using GymApp.Modules.Billing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Billing.Infrastructure.Repositories;

internal sealed class SessionPackageRepository : ISessionPackageRepository
{
    private readonly BillingDbContext _dbContext;

    public SessionPackageRepository(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SessionPackage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SessionPackages
            .Include(sp => sp.UsageHistory)
            .FirstOrDefaultAsync(sp => sp.Id == id, cancellationToken);
    }

    public Task<IReadOnlyList<SessionPackage>> GetByGymAsync(
        Guid gymId,
        CancellationToken cancellationToken = default) =>
        ListNewestFirstAsync(sp => sp.GymId == gymId, cancellationToken);

    public Task<IReadOnlyList<SessionPackage>> GetByMemberAsync(
        Guid gymId,
        Guid memberId,
        CancellationToken cancellationToken = default) =>
        ListNewestFirstAsync(sp => sp.GymId == gymId && sp.MemberId == memberId, cancellationToken);

    public Task<IReadOnlyList<SessionPackage>> GetByTrainerAsync(
        Guid gymId,
        Guid trainerId,
        CancellationToken cancellationToken = default) =>
        ListNewestFirstAsync(sp => sp.GymId == gymId && sp.TrainerId == trainerId, cancellationToken);

    public async Task<SessionPackage?> GetActiveByMemberAndTrainerAsync(
        PackageParticipants participants,
        CancellationToken cancellationToken = default)
    {
        var (gymId, memberId, trainerId) = participants;
        // Free trial sessions are used before paid ones; then oldest package first.
        return await _dbContext.SessionPackages
            .Include(sp => sp.UsageHistory)
            .Where(sp => sp.GymId == gymId
                && sp.MemberId == memberId
                && sp.TrainerId == trainerId
                && sp.Status == PackageStatus.Active)
            .OrderByDescending(sp => sp.IsTrial)
            .ThenBy(sp => sp.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SessionPackage>> GetByGymWithUsageAsync(Guid gymId, CancellationToken cancellationToken = default) =>
        await _dbContext.SessionPackages
            .AsNoTracking()
            .Include(sp => sp.UsageHistory)
            .Where(sp => sp.GymId == gymId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SessionPackage>> GetFinishedTrialsAsync(
        DateTime finishedSinceUtc,
        CancellationToken cancellationToken = default) =>
        await _dbContext.SessionPackages
            .Where(sp => sp.IsTrial
                && sp.Status == PackageStatus.Exhausted
                && sp.ExhaustedAt != null
                && sp.ExhaustedAt >= finishedSinceUtc)
            .ToListAsync(cancellationToken);

    public Task<bool> HasTrialAsync(PackageOwner owner, CancellationToken cancellationToken = default) =>
        _dbContext.SessionPackages
            .AsNoTracking()
            .AnyAsync(sp => sp.GymId == owner.GymId && sp.MemberId == owner.MemberId && sp.IsTrial, cancellationToken);

    public Task<SessionPackage?> FindByUsageSessionAsync(PackageSessionRef session, CancellationToken cancellationToken = default) =>
        _dbContext.SessionPackages
            .Include(sp => sp.UsageHistory)
            .Where(sp => sp.GymId == session.GymId && sp.UsageHistory.Any(u => u.SessionId == session.SessionId))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(SessionPackage package, CancellationToken cancellationToken = default)
    {
        await _dbContext.SessionPackages.AddAsync(package, cancellationToken);
    }

    public void Update(SessionPackage package)
    {
        _dbContext.SessionPackages.Update(package);
    }

    private async Task<IReadOnlyList<SessionPackage>> ListNewestFirstAsync(
        Expression<Func<SessionPackage, bool>> predicate,
        CancellationToken cancellationToken)
    {
        return await _dbContext.SessionPackages
            .AsNoTracking()
            .Where(predicate)
            .OrderByDescending(sp => sp.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
