namespace GymApp.Modules.Gyms.Application.DTOs;

public sealed record CheckInDto(
    Guid Id,
    Guid GymId,
    Guid MemberId,
    string Method,
    DateTime CheckedInAt,
    Guid? StaffId,
    string? MemberName,
    string? MemberPhotoUrl,
    string? MemberNumber);
