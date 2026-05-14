namespace GymApp.Modules.Identity.Application.DTOs;

public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string Phone,
    string FirstName,
    string LastName,
    string? ProfilePhotoUrl,
    string? DateOfBirth,
    string Gender,
    string Role,
    string Status,
    string Language,
    List<GymMembershipDto> GymMemberships);
