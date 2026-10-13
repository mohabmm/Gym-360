using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Modules.Scheduling.Domain.Entities;

public sealed class TrainerLog : Entity
{
    private TrainerLog() : base() { }

    private TrainerLog(
        Guid id,
        Guid sessionId,
        string exercisesPerformed,
        string? actualVsPlanned,
        int clientEffortRating,
        string? feedback,
        string? techniqueNotes,
        string? areasToImprove,
        string? nextSessionFocus,
        bool attendanceConfirmed) : base(id)
    {
        SessionId = sessionId;
        ExercisesPerformed = exercisesPerformed;
        ActualVsPlanned = actualVsPlanned;
        ClientEffortRating = clientEffortRating;
        Feedback = feedback;
        TechniqueNotes = techniqueNotes;
        AreasToImprove = areasToImprove;
        NextSessionFocus = nextSessionFocus;
        AttendanceConfirmed = attendanceConfirmed;
        LoggedAt = DateTime.UtcNow;
    }

    public Guid SessionId { get; private set; }
    public string ExercisesPerformed { get; private set; } = default!;
    public string? ActualVsPlanned { get; private set; }
    public int ClientEffortRating { get; private set; }
    public string? Feedback { get; private set; }
    public string? TechniqueNotes { get; private set; }
    public string? AreasToImprove { get; private set; }
    public string? NextSessionFocus { get; private set; }
    public bool AttendanceConfirmed { get; private set; }
    public DateTime LoggedAt { get; private set; }

    public static TrainerLog Create(Guid sessionId, TrainerLogDetails details)
    {
        if (details.ClientEffortRating < 1 || details.ClientEffortRating > 5)
            throw new ArgumentOutOfRangeException(nameof(details), "Client effort rating must be between 1 and 5.");

        return new TrainerLog(
            Guid.NewGuid(),
            sessionId,
            details.ExercisesPerformed,
            details.ActualVsPlanned,
            details.ClientEffortRating,
            details.Feedback,
            details.TechniqueNotes,
            details.AreasToImprove,
            details.NextSessionFocus,
            details.AttendanceConfirmed);
    }
}
