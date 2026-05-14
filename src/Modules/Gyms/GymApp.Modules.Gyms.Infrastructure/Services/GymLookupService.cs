using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Tenancy;

namespace GymApp.Modules.Gyms.Infrastructure.Services;

/// <summary>
/// Cross-tenant gym name lookup. Uses the global <see cref="IGymRegistry"/>
/// instead of the per-gym <see cref="Data.GymsDbContext"/> so the call works
/// even when no tenant context is resolved (e.g., during anonymous login).
/// </summary>
internal sealed class GymLookupService : IGymLookupService
{
    private readonly IGymRegistry _registry;

    public GymLookupService(IGymRegistry registry)
    {
        _registry = registry;
    }

    public async Task<string?> GetGymNameAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var registration = await _registry.FindAsync(gymId, cancellationToken);
        return registration?.Name;
    }
}
