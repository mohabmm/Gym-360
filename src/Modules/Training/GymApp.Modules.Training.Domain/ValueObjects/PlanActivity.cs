using GymApp.Modules.Training.Domain.Entities;

namespace GymApp.Modules.Training.Domain.ValueObjects;

/// <summary>
/// How active a trainee is on their plan, for the coach. Counted from the last workout — or
/// from the plan's start when none was done yet. A plan that has not started is never inactive.
/// </summary>
public sealed record PlanActivity(DateOnly? LastWorkoutOn, int? DaysSince, bool IsInactive)
{
    public static PlanActivity For(TrainingPlan plan, IEnumerable<PlanWorkoutCompletion> workouts, DateOnly today, int inactiveDays)
    {
        var last = workouts.Where(w => !w.IsVoided && w.PlanId == plan.Id).Select(w => (DateOnly?)w.CompletedOn).Max();
        var from = last ?? plan.StartDate;
        if (from > today)
            return new PlanActivity(last, null, false);
        var days = today.DayNumber - from.DayNumber;
        return new PlanActivity(last, days, days >= inactiveDays);
    }
}
