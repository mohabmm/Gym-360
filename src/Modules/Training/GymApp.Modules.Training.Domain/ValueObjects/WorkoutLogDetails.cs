using GymApp.Modules.Training.Domain.Enums;

namespace GymApp.Modules.Training.Domain.ValueObjects;

/// <summary>When and what a user trained in a logged workout session.</summary>
public sealed record WorkoutLogDetails
{
    public required DateOnly Date { get; init; }
    public required DateTime StartedAt { get; init; }
    public Guid? WorkoutId { get; init; }
    public string? Notes { get; init; }
    public List<MuscleGroup>? MuscleGroupsWorked { get; init; }
    public int? PerceivedExertion { get; init; }
}
