using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Modules.Training.Application.TrainingPlans.Workouts;

/// <summary>
/// A plan workout the app finished, as uploaded (maybe days later). The day's title, week and
/// place are the app's copy, used only when a coach edit removed the day in the meantime.
/// </summary>
public sealed record PlanWorkoutInput
{
    public required Guid PlanId { get; init; }
    public required string DayKey { get; init; }
    public required DateTime CompletedAt { get; init; }
    public int? PlanVersion { get; init; }
    public string? DayTitle { get; init; }
    public string? DayTitleAr { get; init; }
    public int? Week { get; init; }
    public int? SequenceIndex { get; init; }
    public int? DurationMinutes { get; init; }
    public string? Note { get; init; }
    public IReadOnlyList<CompletedSet>? Sets { get; init; }
}

/// <summary>What the server did with an upload (the app shows a short note for the last three).</summary>
public static class PlanWorkoutOutcome
{
    public const string Created = "created";
    /// <summary>This upload was already saved (a retry): nothing changed.</summary>
    public const string AlreadySynced = "already_synced";
    /// <summary>The same day was already finished on that date (another device, or the coach's session).</summary>
    public const string Duplicate = "duplicate";
    /// <summary>Saved, but the coach changed the plan meanwhile and this day is no longer in it.</summary>
    public const string PlanChanged = "plan_changed";
    /// <summary>Saved against the old plan: the coach gave a new plan meanwhile.</summary>
    public const string PlanReplaced = "plan_replaced";
}

public sealed record PlanWorkoutSyncDto(PlanWorkoutDto Workout, string Outcome);

public sealed record PlanWorkoutDto
{
    public required Guid Id { get; init; }
    public required Guid ClientId { get; init; }
    public required Guid PlanId { get; init; }
    public required string DayKey { get; init; }
    public required string DayTitle { get; init; }
    public string? DayTitleAr { get; init; }
    public required int Week { get; init; }
    public required DateOnly CompletedOn { get; init; }
    public required DateTime CompletedAt { get; init; }
    public required int SetsDone { get; init; }
    public int? DurationMinutes { get; init; }
    public required string Source { get; init; }
    public required bool IsVoided { get; init; }
}

/// <summary>Where the trainee is in the plan. NextDayKey is null once every day is done.</summary>
public sealed record PlanCursorDto(
    string? NextDayKey, int? NextWeek, int LastDoneIndex, int Total, bool Finished, bool DoneToday, IReadOnlyList<string> DoneKeys);

/// <summary>The trainee's home card and plan screen: the plan, the next day, this week, and last weights to prefill.</summary>
public sealed record MyPlanDto(
    TrainingPlanDto Plan, PlanCursorDto Cursor, PlanWeekStats Stats, IReadOnlyDictionary<string, decimal> LastWeights);

public sealed record PlanProgressDto(
    Guid? PlanId,
    PlanCursorDto? Cursor,
    PlanWeekStats Stats,
    IReadOnlyList<PlanWorkoutDto> History,
    IReadOnlyList<ExerciseTrend> Exercises);

internal static class PlanWorkoutMapper
{
    public static PlanWorkoutDto ToDto(this PlanWorkoutCompletion c) => new()
    {
        Id = c.Id,
        ClientId = c.ClientId,
        PlanId = c.PlanId,
        DayKey = c.DayKey,
        DayTitle = c.DayTitle,
        DayTitleAr = c.DayTitleAr,
        Week = c.Week,
        CompletedOn = c.CompletedOn,
        CompletedAt = c.CompletedAt,
        SetsDone = c.SetsDone,
        DurationMinutes = c.DurationMinutes,
        Source = c.Source.ToString(),
        IsVoided = c.IsVoided
    };

    public static PlanCursorDto ToDto(this PlanCursor cursor) => new(
        cursor.Next?.Day.Key, cursor.Next?.Week, cursor.LastDoneIndex, cursor.Total, cursor.Finished, cursor.DoneToday, cursor.DoneKeys);
}
