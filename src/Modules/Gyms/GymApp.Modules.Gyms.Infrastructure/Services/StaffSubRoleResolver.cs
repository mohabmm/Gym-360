using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Infrastructure.Services;

/// <summary>
/// Implements the cross-module resolver so the Identity JWT issuer can read the
/// gym-side StaffRole sub-role (Owner/Manager/Receptionist) at login time and
/// embed it as a `staffSubRole` claim. Authorization policies (e.g.
/// gym_owner_or_manager) gate on the claim instead of issuing a per-request
/// DB lookup.
/// </summary>
internal sealed class StaffSubRoleResolver : IStaffSubRoleResolver
{
    private readonly IStaffRoleRepository _staffRoleRepository;

    public StaffSubRoleResolver(IStaffRoleRepository staffRoleRepository)
    {
        _staffRoleRepository = staffRoleRepository;
    }

    public async Task<string?> GetSubRoleAsync(Guid gymId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (gymId == Guid.Empty || userId == Guid.Empty) return null;
        var role = await _staffRoleRepository.GetByUserAndGymAsync(userId, gymId, cancellationToken);
        return role?.Role.ToString();
    }
}
