using GymApp.Common.Domain;
using GymApp.Modules.Identity.Domain.Enums;

namespace GymApp.Modules.Identity.Domain.Entities;

public sealed class OtpCode : Entity
{
    private OtpCode() { }

    public Guid UserId { get; private set; }
    public string Code { get; private set; } = default!;
    public OtpPurpose Purpose { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public static OtpCode Create(Guid userId, string code, OtpPurpose purpose, TimeSpan validity)
    {
        return new OtpCode
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Code = code,
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.Add(validity),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkUsed()
    {
        IsUsed = true;
    }
}
