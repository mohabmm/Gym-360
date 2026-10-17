using GymApp.Modules.Training.Domain.Enums;

namespace GymApp.Modules.Training.Domain.ValueObjects;

/// <summary>One performed set of an exercise within a workout log.</summary>
public sealed record LogSetDetails
{
    public required Guid ExerciseId { get; init; }
    public required int SetNumber { get; init; }
    public required int Reps { get; init; }
    public decimal? Weight { get; init; }
    public WeightUnit WeightUnit { get; init; } = WeightUnit.Kg;
    public int? DurationSeconds { get; init; }
    public bool IsCompleted { get; init; } = true;
    public string? Notes { get; init; }
}
