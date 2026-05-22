namespace GymApp.Modules.Identity.Shared;

/// <summary>
/// Cross-module contract to mutate user profile fields. Implementation lives in
/// the Identity module. Use from gym-staff flows where staff update a member's
/// display name or phone from within a gym-scoped endpoint.
/// </summary>
public interface IUserProfileUpdater
{
    Task<bool> UpdateAsync(
        Guid userId,
        string? firstName,
        string? lastName,
        string? phone,
        CancellationToken cancellationToken = default);
}
