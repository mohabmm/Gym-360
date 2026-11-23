namespace GymApp.Modules.Training.Domain.ValueObjects;

/// <summary>One set the trainee did (or skipped) in a plan workout: what they actually lifted.</summary>
public sealed record CompletedSet
{
    public required string ExerciseKey { get; init; }
    public Guid? ExerciseId { get; init; }
    public required string ExerciseName { get; init; }
    public string? ExerciseNameAr { get; init; }
    public required int SetNumber { get; init; }
    public bool Done { get; init; } = true;
    public int? Reps { get; init; }
    public decimal? WeightKg { get; init; }
    public int? Seconds { get; init; }

    public string Identity() => ExerciseIdentity.Of(ExerciseId, ExerciseName);
}

/// <summary>The same exercise across plans and edits: the library id, else the lower-cased name.</summary>
public static class ExerciseIdentity
{
    public static string Of(Guid? exerciseId, string name) =>
        exerciseId?.ToString() ?? name.Trim().ToLowerInvariant();

    public static string Of(PlanExercise exercise) => Of(exercise.ExerciseId, exercise.Name);
}
