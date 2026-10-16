using GymApp.Modules.Identity.Domain.Enums;

namespace GymApp.Modules.Identity.Domain.ValueObjects;

public sealed class GymMembership : IEquatable<GymMembership>
{
    private GymMembership() { }

    public Guid GymId { get; private init; }
    public UserRole Role { get; private init; }
    public DateTime JoinedAt { get; private init; }
    public bool IsActive { get; private set; }
    public string? Section { get; private init; }
    public string? MemberNumber { get; private init; }

    public static GymMembership Create(GymMembershipDetails details, DateTime joinedAt)
    {
        return new GymMembership
        {
            GymId = details.GymId,
            Role = details.Role,
            JoinedAt = joinedAt,
            IsActive = true,
            Section = details.Section,
            MemberNumber = details.MemberNumber
        };
    }

    public void Deactivate() => IsActive = false;

    /// <summary>True when this membership is active and at <paramref name="gymId"/>.</summary>
    public bool IsActiveAt(Guid gymId) => IsActive && GymId == gymId;

    /// <summary>True when this membership is active, at <paramref name="gymId"/>, in <paramref name="role"/>.</summary>
    public bool IsActiveAs(Guid gymId, UserRole role) => IsActiveAt(gymId) && Role == role;

    public bool Equals(GymMembership? other) =>
        other is not null && GymId == other.GymId && Role == other.Role;

    public override bool Equals(object? obj) => obj is GymMembership other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(GymId, Role);
}
