using GymApp.Common.Domain;

namespace GymApp.Modules.Training.Domain.Errors;

/// <summary>pt.training_plan errors. Codes avoid the <c>PLAN.</c> prefix, which means "billing plan" (403).</summary>
public static class TrainingPlanErrors
{
    public static readonly Error TemplateNotFound = new("Training.PlanTemplateNotFound", "The plan template was not found.");
    public static readonly Error PlanNotFound = new("Training.TrainingPlanNotFound", "The training plan was not found.");
    public static readonly Error TemplateForbidden = new("Training.PlanTemplate.Forbidden", "You cannot change this template.");
    public static readonly Error PlanForbidden = new("Training.TrainingPlan.Forbidden", "This trainee is not your client.");
    public static readonly Error PlanNotActive = new("Training.TrainingPlanNotActive", "Only the active plan can be edited.");
    public static readonly Error VersionConflict = new("Training.TrainingPlan.Conflict", "The plan was changed by someone else. Reload it and try again.");

    public static readonly Error DayNotInPlan = new("Training.PlanDayNotFound", "This workout day is not in the plan.");

    public static Error InvalidDocument(string reason) => new("Training.InvalidPlan", reason);

    public static readonly Error SettingsForbidden = new("Training.PlanSettings.Forbidden", "Only the gym owner or a manager can change plan settings.");
    public static readonly Error AlreadyNudged = new("Training.PlanNudge.Conflict", "You already reminded this client today.");

    public static readonly Error AwardNotFound = new("Training.ChallengeAwardNotFound", "The challenge award was not found.");

    public static Error InvalidSettings(string reason) => new("Training.InvalidPlanSettings", reason);

    public static readonly Error StarterPlanOnly = new("Training.StarterPlan.Forbidden",
        "Until the staff unlock the programs for this member, give a 3-day starter plan: one week, at most 3 workout days, no template.");

    public static readonly Error WeekProgramOnly = new("Training.WeekProgram.Forbidden",
        "The staff unlocked one week: give one week of at most 7 workout days.");

    public static readonly Error ThirtyDaysOnly = new("Training.MonthProgram.Forbidden",
        "The staff unlocked 30 days: give at most 4 weeks.");

    /// <summary>The plan's content does not fit its starter, one-week or 30-day limit.</summary>
    public static Error PlanTooLong(Enums.TrainingPlanLimit limit) => limit switch
    {
        Enums.TrainingPlanLimit.Starter => StarterPlanOnly,
        Enums.TrainingPlanLimit.ThirtyDays => ThirtyDaysOnly,
        _ => WeekProgramOnly,
    };

    public static readonly Error UnlockNotYourClient = new("Training.PlanUnlock.NotFound",
        "This coach has no package with this member.");

    public static Error InvalidWorkout(string reason) => new("Training.InvalidPlanWorkout", reason);
}
