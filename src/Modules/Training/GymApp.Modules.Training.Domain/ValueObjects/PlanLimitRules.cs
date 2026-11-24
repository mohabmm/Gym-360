using GymApp.Modules.Training.Domain.Enums;

namespace GymApp.Modules.Training.Domain.ValueObjects;

/// <summary>
/// pt.starter_plan: the shape and length of a limited plan.
/// <see cref="TrainingPlanLimit.Starter"/>: one week, at most 3 workout days, over 3 days.
/// <see cref="TrainingPlanLimit.OneWeek"/>: one week (up to 7 workout days), over 7 days.
/// <see cref="TrainingPlanLimit.ThirtyDays"/>: up to 4 weeks, over 30 days.
/// </summary>
public static class PlanLimitRules
{
    public const int StarterWorkoutDays = 3;

    /// <summary>Calendar days a limited plan lasts; 0 when unlimited.</summary>
    public static int LengthDays(TrainingPlanLimit limit) => limit switch
    {
        TrainingPlanLimit.Starter => 3,
        TrainingPlanLimit.OneWeek => 7,
        TrainingPlanLimit.ThirtyDays => 30,
        _ => 0,
    };

    /// <summary>Most plan weeks; 0 when unlimited.</summary>
    public static int MaxWeeks(TrainingPlanLimit limit) => limit switch
    {
        TrainingPlanLimit.None => 0,
        TrainingPlanLimit.ThirtyDays => 4,
        _ => 1,
    };

    /// <summary>Most workout days a week.</summary>
    public static int MaxDaysPerWeek(TrainingPlanLimit limit) =>
        limit == TrainingPlanLimit.Starter ? StarterWorkoutDays : PlanDocumentRules.MaxDaysPerWeek;

    public static bool Fits(TrainingPlanLimit limit, PlanDocument document) =>
        limit == TrainingPlanLimit.None
        || (document.Weeks.Count >= 1 && document.Weeks.Count <= MaxWeeks(limit)
            && document.Weeks.All(w => w.Days.Count >= 1 && w.Days.Count <= MaxDaysPerWeek(limit)));

    /// <summary>The last day of a limited plan that starts on <paramref name="startDate"/>; null when unlimited.</summary>
    public static DateOnly? EndsOn(TrainingPlanLimit limit, DateOnly startDate) =>
        limit == TrainingPlanLimit.None ? null : startDate.AddDays(LengthDays(limit) - 1);

    /// <summary>The plan limit a staff unlock of <paramref name="length"/> allows.</summary>
    public static TrainingPlanLimit For(UnlockLength length) =>
        length == UnlockLength.ThirtyDays ? TrainingPlanLimit.ThirtyDays : TrainingPlanLimit.OneWeek;
}
