using GymApp.Modules.Training.Application.TrainingPlans.Workouts;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Modules.Training.Application.TrainingPlans;

/// <summary>
/// The editable part of a plan or template, as the builder sends it.
/// <paramref name="Goal"/>: <c>GeneralFitness</c>, <c>FatLoss</c>, <c>MuscleGain</c> or <c>Other</c> (with <paramref name="GoalText"/>).
/// <paramref name="RepeatWeekOneForWeeks"/>: when set, week 1 is copied to that many weeks ("copy week 1 to all weeks").
/// </summary>
public sealed record PlanContentInput(
    string Name,
    string? NameAr,
    string Goal,
    string? GoalText,
    PlanDocument Document,
    int? RepeatWeekOneForWeeks = null);

public sealed record PlanTemplateSummaryDto(
    Guid Id,
    string Scope,
    Guid? OwnerTrainerId,
    string Name,
    string? NameAr,
    string Goal,
    string? GoalText,
    int DurationWeeks,
    int DaysPerWeek,
    bool IsSample,
    bool CanEdit,
    DateTime UpdatedAt);

public sealed record PlanTemplateDto(PlanTemplateSummaryDto Summary, PlanDocument Document);

public sealed record TrainingPlanSummaryDto(
    Guid Id,
    Guid TraineeId,
    Guid TrainerId,
    Guid? SourceTemplateId,
    string Name,
    string? NameAr,
    string Goal,
    string? GoalText,
    int DurationWeeks,
    int DaysPerWeek,
    int WorkoutCount,
    DateOnly StartDate,
    string Status,
    int Version,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? EndedAt)
{
    /// <summary>pt.starter_plan: <c>Starter</c> (3 days), <c>OneWeek</c> (staff-unlocked week) or <c>None</c>.</summary>
    public string Limit { get; init; } = nameof(TrainingPlanLimit.None);
    /// <summary>Last day of a limited plan; null otherwise.</summary>
    public DateOnly? EndsOn { get; init; }
    /// <summary>A limited plan whose last day has passed.</summary>
    public bool HasEnded { get; init; }
}

public sealed record TrainingPlanDto(TrainingPlanSummaryDto Summary, PlanDocument Document);

internal static class PlanDtoMapper
{
    public static PlanTemplateSummaryDto ToSummary(this PlanTemplate t, bool canEdit) => new(
        t.Id, t.Scope.ToString(), t.OwnerTrainerId, t.Name, t.NameAr, t.Goal.ToString(), t.GoalText,
        t.DurationWeeks, t.DaysPerWeek, t.IsSample, canEdit, t.UpdatedAt);

    public static PlanTemplateDto ToDto(this PlanTemplate t, bool canEdit) => new(t.ToSummary(canEdit), t.Document);

    public static TrainingPlanSummaryDto ToSummary(this TrainingPlan p) => new(
        p.Id, p.TraineeId, p.TrainerId, p.SourceTemplateId, p.Name, p.NameAr, p.Goal.ToString(), p.GoalText,
        p.DurationWeeks, p.DaysPerWeek, p.Document.Sequence().Count, p.StartDate, p.Status.ToString(), p.Version,
        p.CreatedAt, p.UpdatedAt, p.EndedAt)
    {
        Limit = p.Limit.ToString(),
        EndsOn = p.EndsOn,
        HasEnded = p.HasEndedOn(TrainingCalendar.Today())
    };

    public static TrainingPlanDto ToDto(this TrainingPlan p) => new(p.ToSummary(), p.Document);
}
