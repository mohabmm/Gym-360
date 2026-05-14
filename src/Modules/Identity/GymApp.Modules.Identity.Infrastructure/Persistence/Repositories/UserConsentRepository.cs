using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Identity.Infrastructure.Persistence.Repositories;

internal sealed class UserConsentRepository : IUserConsentRepository
{
    private readonly IdentityDbContext _dbContext;

    public UserConsentRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<UserConsent>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserConsents
            .AsNoTracking()
            .Where(uc => uc.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserConsent?> GetByUserIdAndTypeAsync(Guid userId, string consentType, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserConsents
            .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.ConsentType == consentType, cancellationToken);
    }

    public async Task AddAsync(UserConsent consent, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserConsents.AddAsync(consent, cancellationToken);
    }

    public void Update(UserConsent consent)
    {
        _dbContext.UserConsents.Update(consent);
    }
}
