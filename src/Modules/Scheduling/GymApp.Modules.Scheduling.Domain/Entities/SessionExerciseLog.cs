using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Modules.Scheduling.Domain.Entities;

public sealed class SessionExerciseLog : Entity
{
    private SessionExerciseLog() : base() { }

    private SessionExerciseLog(Guid id) : base(id) { }

    public Guid SessionId { get; private set; }
    public Guid? ExerciseId { get; private set; }
    public string ExerciseName { get; private set; } = default!;
    public string? ExerciseNameAr { get; private set; }
    public int OrderIndex { get; private set; }
    public int Sets { get; private set; }
    public string Reps { get; private set; } = default!;
    public decimal? Weight { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>pt.exercise_progress: what the coach wants next time (all optional).</summary>
    public int? TargetSets { get; private set; }
    public string? TargetReps { get; private set; }
    public decimal? TargetWeight { get; private set; }
    public int? TargetSeconds { get; private set; }

    /// <summary>pt.exercise_progress: reps or time.</summary>
    public ExerciseMeasure Measure { get; private set; }

    // NULL in the database for logs written before per-set logging.
    private IReadOnlyList<ExerciseSetEntry>? _setEntries;

    /// <summary>Each set as done; empty for logs written before per-set logging (read Sets/Reps/Weight then).</summary>
    public IReadOnlyList<ExerciseSetEntry> SetEntries => _setEntries ?? [];
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    internal static SessionExerciseLog Create(Guid sessionId, ExerciseLogDetails details)
    {
        var log = new SessionExerciseLog(Guid.NewGuid())
        {
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow
        };
        log.Write(details);
        return log;
    }

    internal void Apply(ExerciseLogDetails details)
    {
        Write(details);
        UpdatedAt = DateTime.UtcNow;
    }

    private void Write(ExerciseLogDetails details)
    {
        var summary = ExerciseLogSummary.Of(details);
        ExerciseId = details.ExerciseId;
        ExerciseName = details.ExerciseName;
        ExerciseNameAr = details.ExerciseNameAr;
        OrderIndex = details.OrderIndex;
        Measure = details.Measure;
        _setEntries = details.SetEntries.Count == 0 ? null : details.SetEntries.Select(WithTrimmedNote).ToList();
        Sets = summary.Sets;
        Reps = summary.Reps;
        Weight = summary.Weight;
        Notes = details.Notes;
        TargetSets = details.Target?.Sets;
        TargetReps = details.Target?.Reps;
        TargetWeight = details.Target?.Weight;
        TargetSeconds = details.Target?.Seconds;
    }

    // A blank set note is stored as no note, so the member never sees an empty "note" line.
    private static ExerciseSetEntry WithTrimmedNote(ExerciseSetEntry set) =>
        set with { Note = string.IsNullOrWhiteSpace(set.Note) ? null : set.Note.Trim() };
}
