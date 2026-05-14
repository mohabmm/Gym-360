namespace GymApp.Modules.Training.Application.DTOs;

public sealed record WorkoutExerciseDto(
    Guid ExerciseId,
    int Sets,
    string Reps,
    decimal? Weight,
    int? DurationSeconds,
    int? RestSeconds,
    string? Notes,
    Guid? SupersetGroupId);
