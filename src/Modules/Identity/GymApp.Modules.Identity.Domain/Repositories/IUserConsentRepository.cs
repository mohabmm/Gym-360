using GymApp.Modules.Identity.Domain.Entities;

namespace GymApp.Modules.Identity.Domain.Repositories;

public interface IUserConsentRepository
{
    Task<IReadOnlyList<UserConsent>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserConsent?> GetByUserIdAndTypeAsync(Guid userId, string consentType, CancellationToken cancellationToken = default);
    Task AddAsync(UserConsent consent, CancellationToken cancellationToken = default);
    void Update(UserConsent consent);
}
