namespace GymApp.Modules.Identity.Application.DTOs;

// Shape mirrors the frontend AdminUser type (packages/@gymapp/ui/src/types/user.ts).
// firstNameAr/lastNameAr/phone are optional since we don't track Arabic names on the backend yet.
public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string? Phone,
    string FirstName,
    string LastName,
    string FirstNameAr,
    string LastNameAr,
    string Role,
    string? AdminRole,
    string Status,
    string CreatedAt,
    string? LastLoginAt,
    bool IsYou,
    IReadOnlyList<AdminUserGymDto> Gyms);

public sealed record AdminUserGymDto(Guid Id, string Name);
