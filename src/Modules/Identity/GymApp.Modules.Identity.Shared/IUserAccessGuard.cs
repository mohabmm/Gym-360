namespace GymApp.Modules.Identity.Shared;

/// <summary>
/// Per-request check used by the API host's JWT validation: is this user locked out
/// (suspended or deleted by a platform admin)? Backed by a short-lived cache so a
/// suspension takes effect on the user's very next request instead of when their
/// access token expires. Unknown users are NOT blocked.
/// </summary>
public interface IUserAccessGuard
{
    Task<bool> IsBlockedAsync(Guid userId, CancellationToken cancellationToken = default);
}
