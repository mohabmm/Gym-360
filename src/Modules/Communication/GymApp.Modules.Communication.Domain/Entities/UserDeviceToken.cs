using GymApp.Common.Domain;
using GymApp.Modules.Communication.Domain.Enums;

namespace GymApp.Modules.Communication.Domain.Entities;

public sealed class UserDeviceToken : Entity
{
    private UserDeviceToken() { }

    public Guid UserId { get; private set; }
    public string Token { get; private set; } = default!;
    public DevicePlatform Platform { get; private set; }
    public DateTime RegisteredAtUtc { get; private set; }
    public DateTime LastSeenUtc { get; private set; }

    public static UserDeviceToken Create(Guid userId, string token, DevicePlatform platform)
    {
        return new UserDeviceToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            Platform = platform,
            RegisteredAtUtc = DateTime.UtcNow,
            LastSeenUtc = DateTime.UtcNow
        };
    }

    public void UpdateToken(string newToken)
    {
        Token = newToken;
        LastSeenUtc = DateTime.UtcNow;
    }

    public void RefreshLastSeen()
    {
        LastSeenUtc = DateTime.UtcNow;
    }
}
