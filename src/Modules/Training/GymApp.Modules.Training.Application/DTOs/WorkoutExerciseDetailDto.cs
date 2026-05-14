namespace GymApp.Modules.Training.Application.DTOs;

public sealed record WorkoutExerciseDetailDto(
    Guid Id,
    Guid ExerciseId,
    int OrderIndex,
    int Sets,
    string Reps,
    decimal? Weight,
    int? DurationSeconds,
    int? RestSeconds,
    string? Notes,
    Guid? SupersetGroupId);
