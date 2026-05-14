using GymApp.Common.Domain;

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

    public static TrainerLog Create(
        Guid sessionId,
        string exercisesPerformed,
        string? actualVsPlanned,
        int clientEffortRating,
        string? feedback,
        string? techniqueNotes,
        string? areasToImprove,
        string? nextSessionFocus,
        bool attendanceConfirmed)
    {
        if (clientEffortRating < 1 || clientEffortRating > 5)
            throw new ArgumentOutOfRangeException(nameof(clientEffortRating), "Client effort rating must be between 1 and 5.");

        return new TrainerLog(
            Guid.NewGuid(),
            sessionId,
            exercisesPerformed,
            actualVsPlanned,
            clientEffortRating,
            feedback,
            techniqueNotes,
            areasToImprove,
            nextSessionFocus,
            attendanceConfirmed);
    }
}
