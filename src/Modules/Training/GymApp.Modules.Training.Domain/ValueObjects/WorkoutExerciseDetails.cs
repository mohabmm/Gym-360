namespace GymApp.Modules.Training.Domain.ValueObjects;

/// <summary>How an exercise is prescribed inside a workout: sets, reps, load, timing and superset grouping.</summary>
public sealed record WorkoutExerciseDetails
{
    public required Guid ExerciseId { get; init; }
    public required int Sets { get; init; }
    public required string Reps { get; init; }
    public decimal? Weight { get; init; }
    public int? DurationSeconds { get; init; }
    public int? RestSeconds { get; init; }
    public string? Notes { get; init; }
    public Guid? SupersetGroupId { get; init; }
}
