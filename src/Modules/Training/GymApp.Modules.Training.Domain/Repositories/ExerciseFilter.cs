using GymApp.Modules.Training.Domain.Enums;

namespace GymApp.Modules.Training.Domain.Repositories;

/// <summary>Optional criteria narrowing an exercise library listing.</summary>
public sealed record ExerciseFilter
{
    public static readonly ExerciseFilter None = new();

    public ExerciseCategory? Category { get; init; }
    public MuscleGroup? MuscleGroup { get; init; }
    public Equipment? Equipment { get; init; }
    public string? SearchTerm { get; init; }
}
