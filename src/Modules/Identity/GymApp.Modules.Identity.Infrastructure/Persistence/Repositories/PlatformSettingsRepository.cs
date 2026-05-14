using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Identity.Infrastructure.Persistence.Repositories;

internal sealed class PlatformSettingsRepository : IPlatformSettingsRepository
{
    private readonly IdentityDbContext _dbContext;

    public PlatformSettingsRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PlatformSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return _dbContext.PlatformSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformSetting>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.PlatformSettings.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PlatformSetting setting, CancellationToken cancellationToken = default)
    {
        await _dbContext.PlatformSettings.AddAsync(setting, cancellationToken);
    }

    public void Update(PlatformSetting setting)
    {
        _dbContext.PlatformSettings.Update(setting);
    }
}
