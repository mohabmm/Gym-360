using System.Globalization;

namespace GymApp.Modules.Scheduling.Domain.ValueObjects;

/// <summary>pt.exercise_progress: how an exercise is counted.</summary>
public enum ExerciseMeasure
{
    /// <summary>Reps per set, with an optional weight (no weight = bodyweight).</summary>
    Reps = 0,

    /// <summary>Seconds held per set (plank, wall sit), with an optional weight.</summary>
    Time = 1
}

/// <summary>
/// One set as the member did it: <see cref="Reps"/> for <see cref="ExerciseMeasure.Reps"/>,
/// <see cref="Seconds"/> for <see cref="ExerciseMeasure.Time"/>; <see cref="Weight"/> (kg) is optional for both.
/// <see cref="Note"/> is the coach's optional remark on this one set ("last 2 reps assisted").
/// </summary>
public sealed record ExerciseSetEntry(int? Reps, decimal? Weight, int? Seconds, string? Note = null)
{
    public const int MaxNoteLength = 200;

    /// <summary>Most sets one exercise can hold; keeps the CSV summary inside its 50-char column.</summary>
    public const int MaxSets = 10;

    public const int MaxReps = 999;

    /// <summary>16:39 — far beyond any hold; three digits keep the summary short.</summary>
    public const int MaxSeconds = 999;

    public const decimal MaxWeightKg = 500m;
}

/// <summary>
/// The legacy one-row view of an exercise (sets count, reps text, working weight), derived from
/// the per-set entries so older app builds and the live-session screen keep reading it.
/// </summary>
public sealed record ExerciseLogSummary(int Sets, string Reps, decimal? Weight)
{
    public static ExerciseLogSummary Of(ExerciseLogDetails details)
    {
        var sets = details.SetEntries;
        if (sets.Count == 0)
            return new ExerciseLogSummary(details.Sets, details.Reps, details.Weight);

        var values = details.Measure == ExerciseMeasure.Time
            ? sets.Select(s => (s.Seconds ?? 0).ToString(CultureInfo.InvariantCulture) + "s").ToList()
            : sets.Select(s => (s.Reps ?? 0).ToString(CultureInfo.InvariantCulture)).ToList();

        var reps = values.Distinct().Count() == 1 ? values[0] : string.Join(',', values);
        return new ExerciseLogSummary(sets.Count, reps, sets.Max(s => s.Weight));
    }
}
