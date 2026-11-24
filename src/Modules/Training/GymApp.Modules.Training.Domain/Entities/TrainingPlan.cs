using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Events;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Modules.Training.Domain.Entities;

/// <summary>
/// pt.training_plan: the plan a coach gave one trainee — from a template (a copy) or built from
/// scratch. One <see cref="TrainingPlanStatus.Active"/> plan per trainee; assigning a new one
/// replaces it and the old one stays as history. Workout days are a sequence, not calendar dates.
/// </summary>
public sealed class TrainingPlan : Entity
{
    private TrainingPlan() { }

    private TrainingPlan(Guid id) : base(id) { }

    public Guid GymId { get; private set; }
    public Guid TraineeId { get; private set; }
    /// <summary>The coach who assigned (or last took over) the plan.</summary>
    public Guid TrainerId { get; private set; }
    /// <summary>Template it was copied from, for reporting only; edits never reach the template.</summary>
    public Guid? SourceTemplateId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? NameAr { get; private set; }
    public PlanGoal Goal { get; private set; }
    public string? GoalText { get; private set; }
    public int DurationWeeks { get; private set; }
    public int DaysPerWeek { get; private set; }
    public PlanDocument Document { get; private set; } = default!;
    public DateOnly StartDate { get; private set; }
    public TrainingPlanStatus Status { get; private set; }
    /// <summary>Starts at 1 and goes up on every edit; also the optimistic-concurrency token.</summary>
    public int Version { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    /// <summary>pt.starter_plan: a 3-day starter plan or a one-week program; None for every other plan.</summary>
    public TrainingPlanLimit Limit { get; private set; }
    /// <summary>Last day of a limited plan; null for every other plan.</summary>
    public DateOnly? EndsOn { get; private set; }

    public PlanContent Content => new() { Name = Name, NameAr = NameAr, Goal = Goal, GoalText = GoalText, Document = Document };

    public bool IsActive => Status == TrainingPlanStatus.Active;

    /// <summary>A limited plan is over the day after <see cref="EndsOn"/>; other plans never end by date.</summary>
    public bool HasEndedOn(DateOnly today) => EndsOn is { } last && today > last;

    public static TrainingPlan Assign(PlanAssignment assignment, PlanContent content)
    {
        var now = DateTime.UtcNow;
        var plan = new TrainingPlan(Guid.NewGuid())
        {
            GymId = assignment.GymId,
            TraineeId = assignment.TraineeId,
            TrainerId = assignment.TrainerId,
            SourceTemplateId = assignment.SourceTemplateId,
            StartDate = assignment.StartDate,
            Status = TrainingPlanStatus.Active,
            Version = 1,
            CreatedAt = now,
            Limit = assignment.Limit,
            EndsOn = PlanLimitRules.EndsOn(assignment.Limit, assignment.StartDate)
        };
        plan.Apply(content, now);
        plan.RaiseDomainEvent(new TrainingPlanAssignedDomainEvent(plan.Id, plan.TrainerId, plan.TraineeId));
        return plan;
    }

    /// <summary>
    /// Coach edits the active plan. <paramref name="expectedVersion"/> (when given) must match,
    /// so two edits from different devices never silently overwrite each other.
    /// </summary>
    public Result Revise(PlanContent content, Guid editedBy, int? expectedVersion)
    {
        if (!IsActive)
            return Result.Failure(TrainingPlanErrors.PlanNotActive);
        if (expectedVersion is { } expected && expected != Version)
            return Result.Failure(TrainingPlanErrors.VersionConflict);
        if (!PlanLimitRules.Fits(Limit, content.Document))
            return Result.Failure(TrainingPlanErrors.PlanTooLong(Limit));
        TrainerId = editedBy;
        Version++;
        Apply(content, DateTime.UtcNow);
        RaiseDomainEvent(new TrainingPlanUpdatedDomainEvent(Id, TraineeId, Version));
        return Result.Success();
    }

    /// <summary>A newer plan was assigned; this one stays in the history.</summary>
    public void Replace() => End(TrainingPlanStatus.Replaced);

    public void Cancel() => End(TrainingPlanStatus.Cancelled);

    private void End(TrainingPlanStatus status)
    {
        if (!IsActive)
            return;
        Status = status;
        EndedAt = DateTime.UtcNow;
        UpdatedAt = EndedAt.Value;
    }

    private void Apply(PlanContent content, DateTime now)
    {
        Name = content.Name;
        NameAr = content.NameAr;
        Goal = content.Goal;
        GoalText = content.GoalText;
        Document = content.Document;
        DurationWeeks = content.DurationWeeks;
        DaysPerWeek = content.DaysPerWeek;
        UpdatedAt = now;
    }
}

/// <summary>Who gets a plan, from whom, from when, and (optionally) which template it was copied from.</summary>
/// <paramref name="Limit"/> marks a pt.starter_plan 3-day plan or one-week program, which ends on its own.
public sealed record PlanAssignment(
    Guid GymId, Guid TrainerId, Guid TraineeId, DateOnly StartDate, Guid? SourceTemplateId,
    TrainingPlanLimit Limit = TrainingPlanLimit.None);
