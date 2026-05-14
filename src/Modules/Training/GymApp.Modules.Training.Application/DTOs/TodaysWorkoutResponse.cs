namespace GymApp.Modules.Training.Application.DTOs;

public sealed record TodaysWorkoutResponse(
    Guid AssignmentId,
    Guid WorkoutId,
    string WorkoutName,
    string? WorkoutDescription,
    int? EstimatedDurationMinutes,
    List<string> TargetMuscleGroups,
    bool IsRemoteMode,
    DateOnly StartDate,
    DateOnly? EndDate);
