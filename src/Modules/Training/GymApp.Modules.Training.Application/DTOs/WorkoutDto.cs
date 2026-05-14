namespace GymApp.Modules.Training.Application.DTOs;

public sealed record WorkoutDto(
    Guid Id,
    Guid TrainerId,
    Guid GymId,
    string Name,
    string? NameAr,
    string? Description,
    int? EstimatedDurationMinutes,
    List<string> TargetMuscleGroups,
    bool IsTemplate,
    int ExerciseCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);
