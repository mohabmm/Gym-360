using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;
using GymApp.Modules.Billing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Billing.Infrastructure.Repositories;

internal sealed class PackageInterestRepository : IPackageInterestRepository
{
    private readonly BillingDbContext _dbContext;

    public PackageInterestRepository(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PackageInterest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.PackageInterests.FirstOrDefaultAsync(pi => pi.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PackageInterest>> GetByMemberAsync(PackageOwner owner, CancellationToken cancellationToken = default) =>
        await _dbContext.PackageInterests
            .AsNoTracking()
            .Where(pi => pi.GymId == owner.GymId && pi.MemberId == owner.MemberId)
            .OrderByDescending(pi => pi.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PackageInterest>> GetByGymAsync(Guid gymId, CancellationToken cancellationToken = default) =>
        await _dbContext.PackageInterests
            .AsNoTracking()
            .Where(pi => pi.GymId == gymId)
            .OrderByDescending(pi => pi.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(PackageInterest interest, CancellationToken cancellationToken = default) =>
        await _dbContext.PackageInterests.AddAsync(interest, cancellationToken);
}
