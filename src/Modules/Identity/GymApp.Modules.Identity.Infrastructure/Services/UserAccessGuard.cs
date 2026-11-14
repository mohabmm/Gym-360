using GymApp.Common.Application.Abstractions;
using GymApp.Common.Infrastructure.Authentication;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Infrastructure.Persistence;
using GymApp.Modules.Identity.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GymApp.Modules.Identity.Infrastructure.Services;

/// <summary>
/// Redis-backed lockout check. Cache value is "blocked" or "ok"; a miss reads the user's
/// status from SQL and caches it. A Redis outage falls back to the SQL read so requests
/// keep flowing (fail-open on cache, never on the actual status).
/// </summary>
internal sealed class UserAccessGuard : IUserAccessGuard, IUserLockoutCache
{
    internal static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);
    private const string Blocked = "blocked";
    private const string Allowed = "ok";

    private readonly ICacheService _cache;
    private readonly IdentityDbContext _dbContext;
    private readonly ILogger<UserAccessGuard> _logger;
    private readonly TimeSpan _deletedTtl;

    public UserAccessGuard(
        ICacheService cache,
        IdentityDbContext dbContext,
        IOptions<JwtOptions> jwtOptions,
        ILogger<UserAccessGuard> logger)
    {
        _cache = cache;
        _dbContext = dbContext;
        _logger = logger;
        _deletedTtl = TimeSpan.FromMinutes(jwtOptions.Value.AccessTokenExpirationMinutes) + DefaultTtl;
    }

    internal static string Key(Guid userId) => $"identity:user-blocked:{userId}";

    public async Task<bool> IsBlockedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cached = await TryCacheAsync(() => _cache.GetAsync<string>(Key(userId), cancellationToken));
        if (cached is not null)
            return cached == Blocked;

        var status = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => (UserStatus?)u.Status)
            .FirstOrDefaultAsync(cancellationToken);

        var blocked = status is UserStatus.Suspended or UserStatus.Deleted;
        await TryCacheAsync(() => _cache.SetAsync(Key(userId), blocked ? Blocked : Allowed, DefaultTtl, cancellationToken));
        return blocked;
    }

    // If Redis is down the write is skipped; reads then also miss Redis and hit SQL, so
    // the lockout still holds (status is already committed by the caller).
    public Task MarkBlockedAsync(Guid userId, TimeSpan? ttl = null, CancellationToken cancellationToken = default) =>
        TryCacheAsync(() => _cache.SetAsync(Key(userId), Blocked, ttl ?? DefaultTtl, cancellationToken));

    public Task MarkDeletedAsync(Guid userId, CancellationToken cancellationToken = default) =>
        MarkBlockedAsync(userId, _deletedTtl, cancellationToken);

    public Task ClearAsync(Guid userId, CancellationToken cancellationToken = default) =>
        TryCacheAsync(() => _cache.RemoveAsync(Key(userId), cancellationToken));

    private async Task TryCacheAsync(Func<Task> action) =>
        await TryCacheAsync(async () => { await action(); return (string?)null; });

    private async Task<string?> TryCacheAsync(Func<Task<string?>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "User access cache unavailable; falling back to the database.");
            return null;
        }
    }
}
