namespace GymApp.Modules.Identity.Shared;

public sealed record UserProfileInfo(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? ProfilePhotoUrl,
    string Role,
    string Status);

/// <summary>
/// Cross-module contract to resolve user ids to roster-level profile info
/// (name, email, phone, photo, role, status) for staff-facing listings such
/// as the trainer client roster or gym admin member pages.
/// Implementation lives in the Identity module infrastructure.
/// </summary>
public interface IUserProfileLookup
{
    Task<UserProfileInfo?> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, UserProfileInfo>> GetProfilesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Narrows <paramref name="candidateIds"/> to the ids whose Identity profile matches
    /// a free-text <paramref name="search"/> (case-insensitive LIKE over first/last name,
    /// email and phone) and/or a profile <paramref name="status"/> ("active" / "inactive").
    /// Lets staff-facing rosters search/filter the WHOLE membership server-side instead of
    /// paging a capped client-side slice. Order is not guaranteed — the caller re-orders.
    /// "frozen" is a gym-directory concept and is filtered by the caller, not here;
    /// "expired" has no profile backing and applies no constraint.
    /// </summary>
    Task<IReadOnlyList<Guid>> FilterProfileIdsAsync(
        IReadOnlyCollection<Guid> candidateIds,
        string? search,
        string? status,
        CancellationToken cancellationToken = default);
}
