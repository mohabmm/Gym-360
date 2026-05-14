namespace GymApp.Modules.Training.Application.DTOs;

public sealed record ProgramDetailDto(
    Guid Id,
    Guid TrainerId,
    Guid GymId,
    string Name,
    string? NameAr,
    string? Description,
    int DurationWeeks,
    bool IsTemplate,
    List<ProgramWeekDto> Weeks,
    DateTime CreatedAt,
    DateTime UpdatedAt);
