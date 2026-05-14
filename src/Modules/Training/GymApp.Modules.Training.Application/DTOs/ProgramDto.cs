namespace GymApp.Modules.Training.Application.DTOs;

public sealed record ProgramDto(
    Guid Id,
    Guid TrainerId,
    Guid GymId,
    string Name,
    string? NameAr,
    string? Description,
    int DurationWeeks,
    bool IsTemplate,
    int WeekCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);
