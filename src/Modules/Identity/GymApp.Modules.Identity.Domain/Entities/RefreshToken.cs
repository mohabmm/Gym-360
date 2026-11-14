using GymApp.Common.Domain;

namespace GymApp.Modules.Identity.Domain.Entities;

public sealed class RefreshToken : Entity
{
    private RefreshToken() { }

    public string Token { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// The gym an owner selected (POST /identity/select-gym) when this token was issued. Refresh
    /// keeps the new access token scoped to it. Null for tokens whose gym comes from the user's
    /// active membership (trainees, trainers, staff) and for owners who have not picked a gym.
    /// </summary>
    public Guid? SelectedGymId { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsExpired && !IsRevoked;

    public static RefreshToken Create(string token, DateTime expiresAt, Guid? selectedGymId = null)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = token,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            SelectedGymId = selectedGymId
        };
    }

    public void Revoke()
    {
        RevokedAt = DateTime.UtcNow;
    }
}
