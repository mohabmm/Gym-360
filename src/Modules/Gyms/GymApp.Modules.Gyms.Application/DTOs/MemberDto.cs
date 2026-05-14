namespace GymApp.Modules.Gyms.Application.DTOs;

public sealed record MemberDto(
    Guid MemberId,
    Guid GymId,
    string? Section,
    string? MemberNumber,
    DateTime JoinedAt,
    string? FirstName = null,
    string? LastName = null,
    string? Email = null,
    string? PhoneNumber = null,
    string? ProfilePhotoUrl = null,
    string? Role = null,
    string? Status = null,
    bool IsFrozen = false);
