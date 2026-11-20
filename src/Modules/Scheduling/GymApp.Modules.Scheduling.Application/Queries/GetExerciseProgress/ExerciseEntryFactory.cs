using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Modules.Scheduling.Application.Queries.GetExerciseProgress;

/// <summary>Turns one exercise log into a board entry with its derived numbers.</summary>
internal static class ExerciseEntryFactory
{
    public static ExerciseEntryDto Create(SessionExerciseLog log, ProgressSession session)
    {
        var sets = ExerciseSets.Of(log);
        var timed = log.Measure == ExerciseMeasure.Time;
        var numbers = timed ? TimedNumbers(sets) : RepsNumbers(sets);

        return new ExerciseEntryDto(
            session.Session.Id,
            session.Session.ScheduledDate,
            session.Number,
            session.IsTrial,
            session.Session.DayLabel,
            log.Sets,
            log.Reps,
            numbers.TotalReps,
            numbers.Weight ?? (timed ? null : log.Weight),
            numbers.Volume,
            numbers.OneRepMax,
            log.Notes,
            SessionExerciseLogMapper.TargetOf(log),
            log.Measure,
            sets,
            numbers.TotalSeconds,
            numbers.BestSeconds);
    }

    // Reps: top-set weight, Σ reps × kg per set, best Epley estimate over the weighted sets.
    private static EntryNumbers RepsNumbers(IReadOnlyList<ExerciseSetEntry> sets)
    {
        var weighted = sets.Where(s => s.Weight is not null).ToList();
        return new EntryNumbers(
            TotalReps: sets.Sum(s => s.Reps ?? 0),
            Weight: sets.Max(s => s.Weight),
            Volume: weighted.Count == 0 ? null : weighted.Sum(s => (s.Reps ?? 0) * s.Weight!.Value),
            OneRepMax: weighted.Select(s => EstimateOneRepMax(s.Weight!.Value, s.Reps ?? 0)).Max(),
            TotalSeconds: null,
            BestSeconds: null);
    }

    // Time: seconds held; weight / volume / 1RM do not apply.
    private static EntryNumbers TimedNumbers(IReadOnlyList<ExerciseSetEntry> sets) =>
        new(
            TotalReps: 0,
            Weight: null,
            Volume: null,
            OneRepMax: null,
            TotalSeconds: sets.Sum(s => s.Seconds ?? 0),
            BestSeconds: sets.Max(s => s.Seconds));

    // Epley: weight × (1 + reps / 30), rounded to the nearest 0.5 kg.
    private static decimal? EstimateOneRepMax(decimal weight, int reps)
    {
        if (weight <= 0m || reps <= 0)
            return null;

        var estimate = reps == 1 ? weight : weight * (1 + reps / 30m);
        return Math.Round(estimate * 2, MidpointRounding.AwayFromZero) / 2;
    }

    private sealed record EntryNumbers(
        int TotalReps,
        decimal? Weight,
        decimal? Volume,
        decimal? OneRepMax,
        int? TotalSeconds,
        int? BestSeconds);
}

/// <summary>The sets of a log as done: its per-set entries, or — for logs written before per-set
/// logging — the reps text expanded set by set at the one recorded weight.</summary>
internal static class ExerciseSets
{
    public static IReadOnlyList<ExerciseSetEntry> Of(SessionExerciseLog log) =>
        log.SetEntries.Count > 0
            ? log.SetEntries
            : RepsParser.PerSet(log.Reps, log.Sets).Select(r => new ExerciseSetEntry(r, log.Weight, null)).ToList();
}

/// <summary>A completed session as numbered on the member's board.</summary>
internal sealed record ProgressSession(Session Session, int Number, bool IsTrial);
