using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.ValueObjects;

namespace GymApp.Modules.Identity.Infrastructure.Seed;

/// <summary>A demo gym membership to ensure for the user with <see cref="Email"/>.</summary>
public sealed record DemoGymLink
{
    public required string Email { get; init; }
    public required Guid GymId { get; init; }
    public required UserRole Role { get; init; }
    public string? Section { get; init; }
    public string? MemberNumber { get; init; }

    internal bool IsSatisfiedBy(GymMembership membership) =>
        membership.IsActiveAs(GymId, Role);
}

/// <summary>The Owner aggregate details to ensure for the user with <paramref name="Email"/>.</summary>
public sealed record DemoOwnerProfile(string Email, string CompanyName, string BillingEmail);
