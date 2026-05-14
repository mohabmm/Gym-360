using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Billing.Infrastructure.Repositories;

internal sealed class FeatureRepository : IFeatureRepository
{
    private readonly BillingDbContext _dbContext;

    public FeatureRepository(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Feature?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Features
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<Feature?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Features
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<Feature>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Features
            .AsNoTracking()
            .OrderBy(f => f.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Feature feature, CancellationToken cancellationToken = default)
    {
        await _dbContext.Features.AddAsync(feature, cancellationToken);
    }

    public void Update(Feature feature)
    {
        _dbContext.Features.Update(feature);
    }
}
