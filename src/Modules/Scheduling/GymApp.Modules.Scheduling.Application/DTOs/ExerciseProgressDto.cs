using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Modules.Scheduling.Application.DTOs;

/// <summary>pt.exercise_progress: everything the coach recorded for one member, per day type and per exercise.</summary>
public sealed record ExerciseProgressDto(
    ExerciseProgressSummaryDto Summary,
    IReadOnlyList<TrainingDayDto> Days,
    IReadOnlyList<ExerciseHistoryDto> Exercises);

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
/// <param name="Volume">Total reps × weight; null when no weight was recorded.</param>
/// <param name="EstimatedOneRepMax">Epley estimate from the hardest set; null without a weight.</param>
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
    ExerciseTarget? Target);

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
    bool? HitPreviousTarget);
