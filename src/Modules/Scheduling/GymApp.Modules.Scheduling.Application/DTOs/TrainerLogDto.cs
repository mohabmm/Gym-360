namespace GymApp.Modules.Scheduling.Application.DTOs;

public sealed record TrainerLogDto(
    Guid Id,
    string ExercisesPerformed,
    string? ActualVsPlanned,
    int ClientEffortRating,
    string? Feedback,
    string? TechniqueNotes,
    string? AreasToImprove,
    string? NextSessionFocus,
    bool AttendanceConfirmed,
    DateTime LoggedAt);
