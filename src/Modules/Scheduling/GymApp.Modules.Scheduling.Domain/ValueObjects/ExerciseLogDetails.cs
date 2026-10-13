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
}
