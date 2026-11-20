namespace GymApp.Modules.Scheduling.Domain.ValueObjects;

public sealed record ExerciseLogDetails
{
    public Guid? ExerciseId { get; init; }
    public required string ExerciseName { get; init; }
    public string? ExerciseNameAr { get; init; }
    public required int OrderIndex { get; init; }
    public required int Sets { get; init; }
    public required string Reps { get; init; }
    public decimal? Weight { get; init; }
    public string? Notes { get; init; }
    public ExerciseTarget? Target { get; init; }

    /// <summary>pt.exercise_progress: reps or time; decides how <see cref="SetEntries"/> are read.</summary>
    public ExerciseMeasure Measure { get; init; } = ExerciseMeasure.Reps;

    /// <summary>Each set as done; when given, <see cref="Sets"/>/<see cref="Reps"/>/<see cref="Weight"/> are derived from it.</summary>
    public IReadOnlyList<ExerciseSetEntry> SetEntries { get; init; } = [];
}

/// <summary>pt.exercise_progress: the coach's goal for this exercise next session (<see cref="Seconds"/> for timed exercises).</summary>
public sealed record ExerciseTarget(int? Sets, string? Reps, decimal? Weight, int? Seconds = null)
{
    public bool IsEmpty => Sets is null && string.IsNullOrWhiteSpace(Reps) && Weight is null && Seconds is null;
}
