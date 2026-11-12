using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Shared;

namespace GymApp.Modules.Gyms.Infrastructure.Services;

internal sealed class GymDeskLookup : IGymDeskLookup
{
    private readonly IStaffRoleRepository _staffRoles;

    public GymDeskLookup(IStaffRoleRepository staffRoles)
    {
        _staffRoles = staffRoles;
    }

    public async Task<IReadOnlyList<Guid>> GetDeskUserIdsAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var roles = await _staffRoles.GetByGymIdAsync(gymId, cancellationToken);
        return roles.Select(r => r.UserId).Distinct().ToList();
    }
}
