using GymApp.Modules.Training.Domain.Enums;

namespace GymApp.Modules.Training.Domain.ValueObjects;

/// <summary>Who assigns what (a workout or a program) to which client, and for which period.</summary>
public sealed record WorkoutAssignmentDetails
{
    public required Guid TrainerId { get; init; }
    public required Guid ClientId { get; init; }
    public required Guid GymId { get; init; }
    public required AssignmentType AssignmentType { get; init; }
    public required DateOnly StartDate { get; init; }
    public Guid? WorkoutId { get; init; }
    public Guid? ProgramId { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? Notes { get; init; }
    public bool IsRemoteMode { get; init; }
}
