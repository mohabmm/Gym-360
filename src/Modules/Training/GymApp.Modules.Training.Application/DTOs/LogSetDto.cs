namespace GymApp.Modules.Training.Application.DTOs;

public sealed record LogSetDto(
    Guid ExerciseId,
    int SetNumber,
    int Reps,
    decimal? Weight,
    string WeightUnit,
    int? DurationSeconds,
    bool IsCompleted,
    string? Notes);
