namespace GymApp.Modules.Gyms.Application.DTOs;

public sealed record CheckInResultDto(
    bool Success,
    Guid? CheckInId,
    string? MemberName,
    string? MemberPhotoUrl,
    string? GymName,
    string? ErrorCode = null,
    string? ErrorMessage = null);
