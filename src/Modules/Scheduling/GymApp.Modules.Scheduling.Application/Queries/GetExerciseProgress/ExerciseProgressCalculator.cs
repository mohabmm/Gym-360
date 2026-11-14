using GymApp.Modules.Billing.Shared;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Domain.Entities;

namespace GymApp.Modules.Scheduling.Application.Queries.GetExerciseProgress;

/// <summary>
/// pt.exercise_progress: builds the member's board from their completed, logged sessions
/// (oldest first). Pure, so every rule is unit-tested without a database.
/// </summary>
internal static class ExerciseProgressCalculator
{
    public static ExerciseProgressDto Build(IReadOnlyList<Session> sessions, PtProgressScope scope)
    {
        var numbered = sessions
            .Select((s, i) => new ProgressSession(s, i + 1, scope.TrialSessionIds.Contains(s.Id)))
            .ToList();

        var exercises = numbered
            .SelectMany(ps => ps.Session.ExerciseLogs.Select(log => (Key: KeyOf(log), Log: log, Session: ps)))
            .GroupBy(x => x.Key)
            .Select(g => ToHistory(g.Key, g.Select(x => (x.Log, x.Session)).ToList()))
            .OrderBy(h => h.Entries[0].SessionNumber)
            .ToList();

        return new ExerciseProgressDto(Summarize(numbered, scope), Days(numbered), exercises);
    }

    /// <summary>Same exercise across sessions: the library id, else the name ignoring case and spacing.</summary>
    internal static string KeyOf(SessionExerciseLog log) =>
        log.ExerciseId?.ToString()
        ?? string.Join(' ', log.ExerciseName.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static ExerciseHistoryDto ToHistory(string key, List<(SessionExerciseLog Log, ProgressSession Session)> rows)
    {
        var entries = rows.Select(r => ExerciseEntryFactory.Create(r.Log, r.Session)).ToList();
        var latest = rows[^1].Log;

        return new ExerciseHistoryDto(
            key,
            latest.ExerciseName,
            latest.ExerciseNameAr,
            entries,
            ExerciseTrendCalculator.Calculate(entries),
            entries[^1].Target);
    }

    private static IReadOnlyList<TrainingDayDto> Days(IReadOnlyList<ProgressSession> sessions) =>
        sessions
            .GroupBy(ps => ps.Session.DayLabel)
            .Select(g => new TrainingDayDto(
                g.Key,
                g.Count(),
                g.Max(ps => ps.Session.ScheduledDate),
                ExerciseKeysLatestFirst(g.ToList())))
            .ToList();

    // Exercises of the day as last trained, then any older ones not done that time.
    private static IReadOnlyList<string> ExerciseKeysLatestFirst(List<ProgressSession> daySessions) =>
        daySessions
            .AsEnumerable()
            .Reverse()
            .SelectMany(ps => ps.Session.ExerciseLogs.OrderBy(l => l.OrderIndex).Select(KeyOf))
            .Distinct()
            .ToList();

    private static ExerciseProgressSummaryDto Summarize(IReadOnlyList<ProgressSession> sessions, PtProgressScope scope) =>
        new(
            sessions.Count,
            scope.TrialSessionIds.Count,
            scope.TrialSessionsTotal,
            scope.HasPaidPackage,
            !scope.HasPaidPackage,
            sessions.Count == 0 ? null : sessions[0].Session.ScheduledDate,
            sessions.Count == 0 ? null : sessions[^1].Session.ScheduledDate);
}
