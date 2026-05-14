namespace GymApp.Common.Application.Abstractions;

/// <summary>
/// Cross-module hook so the Identity module can read a user's gym-side StaffRole
/// (Owner/Manager/Receptionist) at JWT issuance time without referencing the Gyms
/// module's domain. Used to surface a `staffSubRole` claim that authorization
/// policies (e.g. gym_owner_or_manager) gate on.
/// </summary>
public interface IStaffSubRoleResolver
{
    Task<string?> GetSubRoleAsync(Guid gymId, Guid userId, CancellationToken cancellationToken = default);
}
