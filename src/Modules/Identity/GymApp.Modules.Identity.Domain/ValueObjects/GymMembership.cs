using GymApp.Modules.Identity.Domain.Enums;

namespace GymApp.Modules.Identity.Domain.ValueObjects;

public sealed class GymMembership : IEquatable<GymMembership>
{
    private GymMembership(Guid gymId, UserRole role, DateTime joinedAt, bool isActive, string? section, string? memberNumber)
    {
        GymId = gymId;
        Role = role;
        JoinedAt = joinedAt;
        IsActive = isActive;
        Section = section;
        MemberNumber = memberNumber;
    }

    public Guid GymId { get; }
    public UserRole Role { get; }
    public DateTime JoinedAt { get; }
    public bool IsActive { get; private set; }
    public string? Section { get; }
    public string? MemberNumber { get; }

    public static GymMembership Create(Guid gymId, UserRole role, DateTime joinedAt, string? section, string? memberNumber)
    {
        return new GymMembership(gymId, role, joinedAt, true, section, memberNumber);
    }

    public void Deactivate() => IsActive = false;

    public bool Equals(GymMembership? other) =>
        other is not null && GymId == other.GymId && Role == other.Role;

    public override bool Equals(object? obj) => obj is GymMembership other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(GymId, Role);
}
