namespace GymApp.Modules.Identity.Application.Abstractions;

/// <summary>
/// Write side of <see cref="GymApp.Modules.Identity.Shared.IUserAccessGuard"/>: called by
/// admin suspend / reactivate / delete so the lockout cache reflects the change at once.
/// </summary>
public interface IUserLockoutCache
{
    /// <summary>Marks the user blocked. <paramref name="ttl"/> null = default cache lifetime.</summary>
    Task MarkBlockedAsync(Guid userId, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Blocks a hard-deleted user for longer than any access token they may still hold,
    /// since the user row (the SQL fallback) is about to disappear.
    /// </summary>
    Task MarkDeletedAsync(Guid userId, CancellationToken cancellationToken = default);

    Task ClearAsync(Guid userId, CancellationToken cancellationToken = default);
}
