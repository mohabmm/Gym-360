using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Modules.Scheduling.Application.Queries.GetExerciseProgress;

/// <summary>First → latest change, personal best and target check for one exercise's entries (oldest first).</summary>
internal static class ExerciseTrendCalculator
{
    public static ExerciseTrendDto Calculate(IReadOnlyList<ExerciseEntryDto> entries)
    {
        var first = entries[0];
        var latest = entries[^1];
        var bestWeight = entries.Max(e => e.Weight);

        return new ExerciseTrendDto(
            first.Weight,
            latest.Weight,
            bestWeight,
            PercentChange(first.Weight, latest.Weight),
            latest.Volume,
            PercentChange(first.Volume, latest.Volume),
            latest.EstimatedOneRepMax,
            IsPersonalBest(entries, latest),
            HitPreviousTarget(entries));
    }

    private static decimal? PercentChange(decimal? from, decimal? to)
    {
        if (from is not { } a || to is not { } b || a <= 0)
            return null;

        return Math.Round((b - a) / a * 100, 1);
    }

    // New best working weight, or the same best weight for more reps than ever before.
    private static bool IsPersonalBest(IReadOnlyList<ExerciseEntryDto> entries, ExerciseEntryDto latest)
    {
        if (entries.Count < 2 || latest.Weight is not { } weight)
            return false;

        var earlier = entries.Take(entries.Count - 1).ToList();
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
        return WeightMet(target, latest) && SetsMet(target, latest) && RepsMet(target, latest);
    }

    private static bool WeightMet(ExerciseTarget target, ExerciseEntryDto latest) =>
        target.Weight is not { } w || (latest.Weight ?? 0) >= w;

    private static bool SetsMet(ExerciseTarget target, ExerciseEntryDto latest) =>
        target.Sets is not { } s || latest.Sets >= s;

    // Every set must reach the target reps.
    private static bool RepsMet(ExerciseTarget target, ExerciseEntryDto latest) =>
        RepsParser.FirstOrNull(target.Reps) is not { } r
        || RepsParser.PerSet(latest.Reps, latest.Sets).DefaultIfEmpty(0).Min() >= r;
}
