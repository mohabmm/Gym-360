using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Modules.Scheduling.Domain.Entities;

public sealed class TrainerLog : Entity
{
    private TrainerLog() : base() { }

    private TrainerLog(Guid id) : base(id) { }

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

        return new TrainerLog(Guid.NewGuid())
        {
            SessionId = sessionId,
            ExercisesPerformed = details.ExercisesPerformed,
            ActualVsPlanned = details.ActualVsPlanned,
            ClientEffortRating = details.ClientEffortRating,
            Feedback = details.Feedback,
            TechniqueNotes = details.TechniqueNotes,
            AreasToImprove = details.AreasToImprove,
            NextSessionFocus = details.NextSessionFocus,
            AttendanceConfirmed = details.AttendanceConfirmed,
            LoggedAt = DateTime.UtcNow
        };
    }
}
