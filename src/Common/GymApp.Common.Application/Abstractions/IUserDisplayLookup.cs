namespace GymApp.Common.Application.Abstractions;

public sealed record UserDisplay(Guid UserId, string FullName, string? PhotoUrl);

/// <summary>
/// Cross-module contract to resolve user ids to display name + photo for read-only
/// projections (e.g. leaderboards) that live outside the Identity module.
/// Implementation lives in the Identity module infrastructure.
/// </summary>
public interface IUserDisplayLookup
{
    Task<IReadOnlyDictionary<Guid, UserDisplay>> GetDisplayNamesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);
}
