using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Modules.Scheduling.Application.DTOs;

/// <summary>pt.exercise_progress: everything the coach recorded for one member, per session, day type and exercise.</summary>
/// <param name="Sessions">Every logged session, oldest first.</param>
public sealed record ExerciseProgressDto(
    ExerciseProgressSummaryDto Summary,
    IReadOnlyList<TrainingDayDto> Days,
    IReadOnlyList<ExerciseHistoryDto> Exercises,
    IReadOnlyList<SessionSummaryDto> Sessions);

/// <param name="SessionNumber">1-based position of the session on the member's board.</param>
/// <param name="ExerciseKeys">Keys into <see cref="ExerciseProgressDto.Exercises"/>, in the coach's order.</param>
/// <param name="CoachRecap">The coach's post-session log; null until they write one.</param>
public sealed record SessionSummaryDto(
    Guid SessionId,
    int SessionNumber,
    DateOnly Date,
    string? DayLabel,
    bool IsTrialSession,
    IReadOnlyList<string> ExerciseKeys,
    SessionCoachRecapDto? CoachRecap);

/// <param name="EffortRating">How hard the member worked, 1–5.</param>
public sealed record SessionCoachRecapDto(
    string? Feedback,
    string? NextSessionFocus,
    int EffortRating);

/// <param name="IsTrialView">The member has not paid yet: the board only holds their free trial.</param>
public sealed record ExerciseProgressSummaryDto(
    int SessionsLogged,
    int TrialSessionsUsed,
    int TrialSessionsTotal,
    bool HasPaidPackage,
    bool IsTrialView,
    DateOnly? FirstSessionDate,
    DateOnly? LastSessionDate);

/// <param name="Label">The coach's day label ("Push Day"); null for sessions without one.</param>
/// <param name="ExerciseKeys">Keys into <see cref="ExerciseProgressDto.Exercises"/>, in the order last trained.</param>
public sealed record TrainingDayDto(
    string? Label,
    int SessionsCount,
    DateOnly LastTrained,
    IReadOnlyList<string> ExerciseKeys);

public sealed record ExerciseHistoryDto(
    string Key,
    string Name,
    string? NameAr,
    IReadOnlyList<ExerciseEntryDto> Entries,
    ExerciseTrendDto Trend,
    ExerciseTarget? NextTarget);

/// <param name="SessionNumber">1-based position of the session on the member's board.</param>
/// <param name="Weight">Top-set weight (kg); null for timed exercises.</param>
/// <param name="Volume">Σ reps × kg over the sets; null when no weight was recorded or the exercise is timed.</param>
/// <param name="EstimatedOneRepMax">Best Epley estimate over the weighted sets; null without a weight.</param>
/// <param name="SetEntries">Each set as done (legacy logs are expanded from Sets/Reps/Weight).</param>
/// <param name="TotalSeconds">Timed exercises: seconds held over all sets.</param>
/// <param name="BestSeconds">Timed exercises: the longest set.</param>
/// <param name="IsPersonalBest">This session beat every earlier time the exercise was done.</param>
/// <param name="HitPreviousTarget">Whether this session met the target set the time before; null when there was none.</param>
public sealed record ExerciseEntryDto(
    Guid SessionId,
    DateOnly Date,
    int SessionNumber,
    bool IsTrialSession,
    string? DayLabel,
    int Sets,
    string Reps,
    int TotalReps,
    decimal? Weight,
    decimal? Volume,
    decimal? EstimatedOneRepMax,
    string? Notes,
    ExerciseTarget? Target,
    ExerciseMeasure Measure,
    IReadOnlyList<ExerciseSetEntry> SetEntries,
    int? TotalSeconds,
    int? BestSeconds,
    bool IsPersonalBest = false,
    bool? HitPreviousTarget = null);

/// <param name="LatestBestSeconds">Timed exercises: longest hold last session.</param>
/// <param name="BestSecondsEver">Timed exercises: longest hold on record.</param>
/// <param name="SecondsChangePercent">Timed exercises: first → latest longest hold.</param>
/// <param name="HitPreviousTarget">Whether the latest entry met the target set the session before; null when there was none.</param>
public sealed record ExerciseTrendDto(
    decimal? FirstWeight,
    decimal? LatestWeight,
    decimal? BestWeight,
    decimal? WeightChangePercent,
    decimal? LatestVolume,
    decimal? VolumeChangePercent,
    decimal? LatestEstimatedOneRepMax,
    bool LatestIsPersonalBest,
    bool? HitPreviousTarget,
    int? LatestBestSeconds = null,
    int? BestSecondsEver = null,
    decimal? SecondsChangePercent = null);
