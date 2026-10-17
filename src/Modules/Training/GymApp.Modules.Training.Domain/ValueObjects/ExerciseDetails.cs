using GymApp.Modules.Training.Domain.Enums;

namespace GymApp.Modules.Training.Domain.ValueObjects;

/// <summary>What an exercise is: its names, how to perform it and what it trains.</summary>
public sealed record ExerciseDetails
{
    public required string Name { get; init; }
    public string? NameAr { get; init; }
    public string? Description { get; init; }
    public string? Instructions { get; init; }
    public string? DemoMediaUrl { get; init; }
    public required Equipment Equipment { get; init; }
    public required ExerciseCategory Category { get; init; }
    public required List<MuscleGroup> MuscleGroups { get; init; }
}
