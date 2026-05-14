namespace GymApp.Common.Application.Abstractions;

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
}
