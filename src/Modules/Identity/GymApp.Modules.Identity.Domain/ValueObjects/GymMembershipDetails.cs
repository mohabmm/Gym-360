using GymApp.Modules.Identity.Domain.Enums;

namespace GymApp.Modules.Identity.Domain.ValueObjects;

/// <summary>Which gym a <see cref="GymMembership"/> belongs to, in what role, and its optional section / member number.</summary>
public sealed record GymMembershipDetails
{
    public required Guid GymId { get; init; }
    public required UserRole Role { get; init; }
    public string? Section { get; init; }
    public string? MemberNumber { get; init; }
}
