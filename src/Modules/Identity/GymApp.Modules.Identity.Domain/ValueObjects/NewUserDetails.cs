using GymApp.Modules.Identity.Domain.Enums;

namespace GymApp.Modules.Identity.Domain.ValueObjects;

/// <summary>Everything needed to create a <see cref="Entities.User"/> besides its role.</summary>
public sealed record NewUserDetails
{
    public required Email Email { get; init; }
    public required PhoneNumber PhoneNumber { get; init; }
    public required string PasswordHash { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required Gender Gender { get; init; }
    public Language Language { get; init; } = Language.Arabic;
    public string? NationalId { get; init; }
}
