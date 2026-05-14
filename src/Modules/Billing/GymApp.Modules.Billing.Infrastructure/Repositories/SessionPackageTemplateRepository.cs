using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Billing.Infrastructure.Repositories;

internal sealed class SessionPackageTemplateRepository : ISessionPackageTemplateRepository
{
    private readonly BillingDbContext _dbContext;

    public SessionPackageTemplateRepository(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SessionPackageTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SessionPackageTemplates
            .FirstOrDefaultAsync(spt => spt.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<SessionPackageTemplate>> GetByGymIdAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SessionPackageTemplates
            .AsNoTracking()
            .Where(spt => spt.GymId == gymId && spt.IsActive)
            .OrderBy(spt => spt.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(SessionPackageTemplate template, CancellationToken cancellationToken = default)
    {
        await _dbContext.SessionPackageTemplates.AddAsync(template, cancellationToken);
    }

    public void Update(SessionPackageTemplate template)
    {
        _dbContext.SessionPackageTemplates.Update(template);
    }
}
