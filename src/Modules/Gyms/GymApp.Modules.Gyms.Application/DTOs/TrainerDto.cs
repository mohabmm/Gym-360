namespace GymApp.Modules.Gyms.Application.DTOs;

public sealed record TrainerDto(
    Guid TrainerId,
    Guid GymId,
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    string? ProfilePhotoUrl,
    string? Status,
    IReadOnlyList<string> Specializations,
    string? Bio,
    int ActiveMemberCount,
    decimal Rating,
    decimal MonthlyScore);
