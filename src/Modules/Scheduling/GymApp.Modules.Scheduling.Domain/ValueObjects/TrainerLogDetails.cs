namespace GymApp.Modules.Scheduling.Domain.ValueObjects;

public sealed record TrainerLogDetails
{
    public required string ExercisesPerformed { get; init; }
    public string? ActualVsPlanned { get; init; }
    public required int ClientEffortRating { get; init; }
    public string? Feedback { get; init; }
    public string? TechniqueNotes { get; init; }
    public string? AreasToImprove { get; init; }
    public string? NextSessionFocus { get; init; }
    public required bool AttendanceConfirmed { get; init; }
}
