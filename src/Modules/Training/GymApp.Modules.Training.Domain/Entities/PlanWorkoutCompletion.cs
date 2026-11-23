using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Modules.Training.Domain.Entities;

/// <summary>
/// pt.training_plan: one plan workout the trainee finished. Written by the app — often offline,
/// synced later — under a client-made <see cref="ClientId"/> so a retried upload never counts twice.
/// The day's title and place in the plan are copied in, so a later coach edit cannot rewrite history.
/// </summary>
public sealed class PlanWorkoutCompletion : Entity
{
    private PlanWorkoutCompletion() { }

    private PlanWorkoutCompletion(Guid id) : base(id) { }

    public Guid GymId { get; private set; }
    public Guid PlanId { get; private set; }
    public Guid TraineeId { get; private set; }
    /// <summary>Made by the device when the workout was finished; the idempotency key of the upload.</summary>
    public Guid ClientId { get; private set; }
    public string DayKey { get; private set; } = default!;
    public string DayTitle { get; private set; } = default!;
    public string? DayTitleAr { get; private set; }
    public int Week { get; private set; }
    public int SequenceIndex { get; private set; }
    /// <summary>The plan version the trainee was looking at.</summary>
    public int PlanVersion { get; private set; }
    public DateTime CompletedAt { get; private set; }
    /// <summary>Gym-local date of <see cref="CompletedAt"/>; what weekly/monthly counts use.</summary>
    public DateOnly CompletedOn { get; private set; }
    public DateTime ReceivedAt { get; private set; }
    public PlanCompletionSource Source { get; private set; }
    public Guid? SessionId { get; private set; }
    public int? DurationMinutes { get; private set; }
    public string? Note { get; private set; }
    public IReadOnlyList<CompletedSet> Sets { get; private set; } = Array.Empty<CompletedSet>();
    public bool IsVoided { get; private set; }
    public DateTime? VoidedAt { get; private set; }

    public CompletionMark Mark => new(DayKey, SequenceIndex, CompletedOn);

    public int SetsDone => Sets.Count(s => s.Done);

    public static PlanWorkoutCompletion Record(TrainingPlan plan, PlanSlot slot, PlanWorkoutLog log)
    {
        var completion = new PlanWorkoutCompletion(Guid.NewGuid())
        {
            GymId = plan.GymId,
            PlanId = plan.Id,
            TraineeId = plan.TraineeId,
            PlanVersion = plan.Version,
            DayKey = slot.Day.Key,
            DayTitle = slot.Day.Title,
            DayTitleAr = slot.Day.TitleAr,
            Week = slot.Week,
            SequenceIndex = slot.Index,
            ReceivedAt = DateTime.UtcNow
        };
        completion.Apply(log);
        return completion;
    }

    /// <summary>The linked session was reversed or disputed: the workout stops counting.</summary>
    public void Void()
    {
        if (IsVoided)
            return;
        IsVoided = true;
        VoidedAt = DateTime.UtcNow;
    }

    private void Apply(PlanWorkoutLog log)
    {
        ClientId = log.ClientId;
        CompletedAt = log.CompletedAt;
        CompletedOn = log.CompletedOn;
        Source = log.Source;
        SessionId = log.SessionId;
        DurationMinutes = log.DurationMinutes;
        Note = log.Note.OrNull();
        Sets = log.Sets.ToList();
    }
}

/// <summary>What was done: when (UTC and the gym-local date), the sets, and where it came from.</summary>
public sealed record PlanWorkoutLog
{
    public required Guid ClientId { get; init; }
    public required DateTime CompletedAt { get; init; }
    public required DateOnly CompletedOn { get; init; }
    public IReadOnlyList<CompletedSet> Sets { get; init; } = Array.Empty<CompletedSet>();
    public int? DurationMinutes { get; init; }
    public string? Note { get; init; }
    public PlanCompletionSource Source { get; init; } = PlanCompletionSource.App;
    public Guid? SessionId { get; init; }
}
