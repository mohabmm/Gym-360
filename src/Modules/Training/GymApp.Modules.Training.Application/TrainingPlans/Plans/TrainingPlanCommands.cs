using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Training.Application.TrainingPlans.Plans;

/// <summary>
/// Gives <paramref name="TraineeId"/> a plan starting <paramref name="StartDate"/>, replacing their
/// active one. Built from scratch: send <paramref name="Content"/> only. From a template: send
/// <paramref name="TemplateId"/> alone to use it as is, or with <paramref name="Content"/> when the
/// coach changed it in the builder. Either way the plan is a copy; the template never changes.
/// </summary>
public sealed record AssignTrainingPlanCommand(
    PlanActor Actor,
    Guid TraineeId,
    DateOnly StartDate,
    Guid? TemplateId,
    PlanContentInput? Content) : ICommand<TrainingPlanDto>;

/// <param name="ExpectedVersion">The version the coach edited; a newer one on the server is a conflict.</param>
public sealed record UpdateTrainingPlanCommand(
    PlanActor Actor,
    Guid PlanId,
    int? ExpectedVersion,
    PlanContentInput Content) : ICommand<TrainingPlanDto>;

/// <summary>Saves a trainee's plan as a new template (the plan itself is untouched).</summary>
public sealed record SavePlanAsTemplateCommand(
    PlanActor Actor,
    Guid PlanId,
    bool AsGymTemplate,
    string? Name,
    string? NameAr) : ICommand<PlanTemplateDto>;
