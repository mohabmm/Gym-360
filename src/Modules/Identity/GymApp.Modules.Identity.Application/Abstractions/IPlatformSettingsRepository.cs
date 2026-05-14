using GymApp.Modules.Identity.Domain.Entities;

namespace GymApp.Modules.Identity.Application.Abstractions;

public interface IPlatformSettingsRepository
{
    Task<PlatformSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlatformSetting>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(PlatformSetting setting, CancellationToken cancellationToken = default);
    void Update(PlatformSetting setting);
}
