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

    public static Error InvalidSettings(string reason) => new("Training.InvalidPlanSettings", reason);

    public static Error InvalidWorkout(string reason) => new("Training.InvalidPlanWorkout", reason);
}
