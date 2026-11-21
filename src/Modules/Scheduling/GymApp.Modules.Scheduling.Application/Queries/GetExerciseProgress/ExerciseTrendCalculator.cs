using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Modules.Scheduling.Application.Queries.GetExerciseProgress;

/// <summary>
/// First → latest change, personal best and target check for one exercise's entries (oldest first).
/// Only entries counted the same way as the latest one (reps or time) are compared.
/// </summary>
internal static class ExerciseTrendCalculator
{
    public static ExerciseTrendDto Calculate(IReadOnlyList<ExerciseEntryDto> all)
    {
        var entries = ComparableWithLatest(all);
        var first = entries[0];
        var latest = entries[^1];

        return new ExerciseTrendDto(
            first.Weight,
            latest.Weight,
            entries.Max(e => e.Weight),
            PercentChange(first.Weight, latest.Weight),
            latest.Volume,
            PercentChange(first.Volume, latest.Volume),
            latest.EstimatedOneRepMax,
            IsPersonalBest(entries),
            HitPreviousTarget(entries),
            latest.BestSeconds,
            entries.Max(e => e.BestSeconds),
            PercentChange(first.BestSeconds, latest.BestSeconds));
    }

    /// <summary>Each entry flagged as it stood that day: a new best, and whether it met the target set the time before.</summary>
    public static IReadOnlyList<ExerciseEntryDto> FlagEachSession(IReadOnlyList<ExerciseEntryDto> all) =>
        all.Select((entry, i) =>
            {
                var upToThen = ComparableWithLatest(all.Take(i + 1).ToList());
                return entry with { IsPersonalBest = IsPersonalBest(upToThen), HitPreviousTarget = HitPreviousTarget(upToThen) };
            })
            .ToList();

    private static List<ExerciseEntryDto> ComparableWithLatest(IReadOnlyList<ExerciseEntryDto> all) =>
        all.Where(e => e.Measure == all[^1].Measure).ToList();

    private static decimal? PercentChange(decimal? from, decimal? to)
    {
        if (from is not { } a || to is not { } b || a <= 0)
            return null;

        return Math.Round((b - a) / a * 100, 1);
    }

    private static bool IsPersonalBest(IReadOnlyList<ExerciseEntryDto> entries)
    {
        if (entries.Count < 2)
            return false;

        var latest = entries[^1];
        var earlier = entries.Take(entries.Count - 1).ToList();
        return latest.Measure == ExerciseMeasure.Time
            ? IsLongestHold(earlier, latest)
            : IsStrongest(earlier, latest);
    }

    private static bool IsLongestHold(List<ExerciseEntryDto> earlier, ExerciseEntryDto latest) =>
        latest.BestSeconds is { } seconds && seconds > (earlier.Max(e => e.BestSeconds) ?? 0);

    // New best working weight, or the same best weight for more reps than ever before.
    private static bool IsStrongest(List<ExerciseEntryDto> earlier, ExerciseEntryDto latest)
    {
        if (latest.Weight is not { } weight)
            return false;

        var bestBefore = earlier.Max(e => e.Weight) ?? 0;
        if (weight != bestBefore)
            return weight > bestBefore;

        return latest.TotalReps > earlier.Where(e => e.Weight == weight).Max(e => e.TotalReps);
    }

    private static bool? HitPreviousTarget(IReadOnlyList<ExerciseEntryDto> entries)
    {
        if (entries.Count < 2 || entries[^2].Target is not { IsEmpty: false } target)
            return null;

        var latest = entries[^1];
        return WeightMet(target, latest) && SetsMet(target, latest) && RepsMet(target, latest) && SecondsMet(target, latest);
    }

    private static bool WeightMet(ExerciseTarget target, ExerciseEntryDto latest) =>
        target.Weight is not { } w || (latest.SetEntries.Max(s => s.Weight) ?? latest.Weight ?? 0) >= w;

    private static bool SetsMet(ExerciseTarget target, ExerciseEntryDto latest) =>
        target.Sets is not { } s || latest.Sets >= s;

    // Every set must reach the target reps.
    private static bool RepsMet(ExerciseTarget target, ExerciseEntryDto latest) =>
        latest.Measure == ExerciseMeasure.Time
        || RepsParser.FirstOrNull(target.Reps) is not { } r
        || latest.SetEntries.Select(s => s.Reps ?? 0).DefaultIfEmpty(0).Min() >= r;

    // Every set must be held for the target seconds.
    private static bool SecondsMet(ExerciseTarget target, ExerciseEntryDto latest) =>
        latest.Measure != ExerciseMeasure.Time
        || target.Seconds is not { } t
        || latest.SetEntries.Select(s => s.Seconds ?? 0).DefaultIfEmpty(0).Min() >= t;
}
