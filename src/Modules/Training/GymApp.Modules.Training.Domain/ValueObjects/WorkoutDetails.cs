using GymApp.Modules.Training.Domain.Enums;

namespace GymApp.Modules.Training.Domain.ValueObjects;

/// <summary>What a workout is: its names, description, expected duration and targeted muscles.</summary>
public sealed record WorkoutDetails
{
    public required string Name { get; init; }
    public string? NameAr { get; init; }
    public string? Description { get; init; }
    public int? EstimatedDurationMinutes { get; init; }
    public List<MuscleGroup>? TargetMuscleGroups { get; init; }
}
