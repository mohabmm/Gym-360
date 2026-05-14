using GymApp.Modules.Communication.Application.Contracts;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Communication.Infrastructure.Repositories;

internal sealed class FcmTokenStore : IFcmTokenStore
{
    private readonly CommunicationDbContext _dbContext;

    public FcmTokenStore(CommunicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<string>> GetTokensForUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await _dbContext.DeviceTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => t.Token)
            .ToListAsync(ct);
    }

    public async Task RegisterTokenAsync(Guid userId, string token, string platform, CancellationToken ct = default)
    {
        if (!Enum.TryParse<DevicePlatform>(platform, ignoreCase: true, out var devicePlatform))
            devicePlatform = DevicePlatform.Android;

        // Check if the exact token already exists for this user
        var existing = await _dbContext.DeviceTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Token == token, ct);

        if (existing is not null)
        {
            existing.RefreshLastSeen();
            return;
        }

        // Check if a different token exists for another user (stolen/re-registered token — remove it)
        var staleToken = await _dbContext.DeviceTokens
            .FirstOrDefaultAsync(t => t.Token == token && t.UserId != userId, ct);

        if (staleToken is not null)
        {
            _dbContext.DeviceTokens.Remove(staleToken);
        }

        var deviceToken = UserDeviceToken.Create(userId, token, devicePlatform);
        await _dbContext.DeviceTokens.AddAsync(deviceToken, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task RemoveTokenAsync(Guid userId, string token, CancellationToken ct = default)
    {
        await _dbContext.DeviceTokens
            .Where(t => t.UserId == userId && t.Token == token)
            .ExecuteDeleteAsync(ct);
    }
}
