namespace GymApp.Modules.Training.Application.DTOs;

public sealed record ExerciseDto(
    Guid Id,
    string Name,
    string? NameAr,
    string? Description,
    string? Instructions,
    string? DemoMediaUrl,
    List<string> MuscleGroups,
    string Equipment,
    string Category,
    string Scope,
    Guid? GymId,
    Guid? CreatedBy,
    bool IsActive);
