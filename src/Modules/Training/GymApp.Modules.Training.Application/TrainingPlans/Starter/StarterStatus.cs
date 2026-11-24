using GymApp.Modules.Training.Application.TrainingPlans.Workouts;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Modules.Training.Application.TrainingPlans.Starter;

/// <summary>pt.starter_plan: the limited-plan and unlock rows the coach and the desk see.</summary>
internal static class StarterStatus
{
    public static LimitedPlanStatusDto? Of(TrainingPlan? plan) =>
        plan is { Limit: not TrainingPlanLimit.None, EndsOn: { } endsOn }
            ? new LimitedPlanStatusDto(plan.Id, plan.Limit.ToString(), plan.StartDate, endsOn, plan.HasEndedOn(TrainingCalendar.Today()))
            : null;

    public static CoachPlanAccessDto Access(TrainingPlanLimit limit, TrainingPlan? active) =>
        new(limit.ToString(), Of(active))
        {
            LengthDays = PlanLimitRules.LengthDays(limit),
            MaxWeeks = PlanLimitRules.MaxWeeks(limit),
            MaxDaysPerWeek = PlanLimitRules.MaxDaysPerWeek(limit),
        };

    public static ProgramUnlockDto Row(Guid trainerId, string trainerName, PlanProgramUnlock? unlock, TrainingPlan? active) => new(
        trainerId,
        trainerName,
        unlock?.IsUnlocked ?? false,
        (unlock?.Length ?? UnlockLength.OneWeek).ToString(),
        unlock?.LockedAt ?? unlock?.UnlockedAt,
        active?.TrainerId == trainerId ? Of(active) : null);
}
