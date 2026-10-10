using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Repositories;
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

    public async Task<IReadOnlyList<SessionPackage>> GetByGymAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.SessionPackages
            .AsNoTracking()
            .Where(sp => sp.GymId == gymId)
            .OrderByDescending(sp => sp.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SessionPackage>> GetByMemberAsync(
        Guid gymId,
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.SessionPackages
            .AsNoTracking()
            .Where(sp => sp.GymId == gymId && sp.MemberId == memberId)
            .OrderByDescending(sp => sp.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SessionPackage>> GetByTrainerAsync(
        Guid gymId,
        Guid trainerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.SessionPackages
            .AsNoTracking()
            .Where(sp => sp.GymId == gymId && sp.TrainerId == trainerId)
            .OrderByDescending(sp => sp.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<SessionPackage?> GetActiveByMemberAndTrainerAsync(
        Guid gymId,
        Guid memberId,
        Guid trainerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.SessionPackages
            .Include(sp => sp.UsageHistory)
            .Where(sp => sp.GymId == gymId
                && sp.MemberId == memberId
                && sp.TrainerId == trainerId
                && sp.Status == PackageStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(SessionPackage package, CancellationToken cancellationToken = default)
    {
        await _dbContext.SessionPackages.AddAsync(package, cancellationToken);
    }

    public void Update(SessionPackage package)
    {
        _dbContext.SessionPackages.Update(package);
    }
}
