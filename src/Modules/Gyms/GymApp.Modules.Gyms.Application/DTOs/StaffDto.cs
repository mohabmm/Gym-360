namespace GymApp.Modules.Gyms.Application.DTOs;

public sealed record StaffDto(
    Guid Id,
    Guid GymId,
    Guid UserId,
    string Role,
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    string? ProfilePhotoUrl);
