namespace GymApp.Modules.Training.Application.DTOs;

public sealed record WorkoutDetailDto(
    Guid Id,
    Guid TrainerId,
    Guid GymId,
    string Name,
    string? NameAr,
    string? Description,
    int? EstimatedDurationMinutes,
    List<string> TargetMuscleGroups,
    bool IsTemplate,
    List<WorkoutExerciseDetailDto> Exercises,
    DateTime CreatedAt,
    DateTime UpdatedAt);
