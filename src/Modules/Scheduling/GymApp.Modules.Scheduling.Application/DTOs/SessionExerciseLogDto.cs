namespace GymApp.Modules.Scheduling.Application.DTOs;

public sealed record SessionExerciseLogDto(
    Guid Id,
    Guid SessionId,
    Guid? ExerciseId,
    string ExerciseName,
    string? ExerciseNameAr,
    int OrderIndex,
    int Sets,
    string Reps,
    decimal? Weight,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
