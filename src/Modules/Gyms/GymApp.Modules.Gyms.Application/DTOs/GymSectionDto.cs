namespace GymApp.Modules.Gyms.Application.DTOs;

public sealed record GymSectionDto(
    Guid Id,
    string SectionType,
    string? Name,
    bool IsActive);
