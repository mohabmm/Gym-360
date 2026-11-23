using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Training.Application.TrainingPlans.Templates;

/// <param name="AsGymTemplate">True: a gym template every coach can use (owner/manager only). False: the coach's own.</param>
public sealed record CreatePlanTemplateCommand(PlanActor Actor, bool AsGymTemplate, PlanContentInput Content)
    : ICommand<PlanTemplateDto>;

public sealed record UpdatePlanTemplateCommand(PlanActor Actor, Guid TemplateId, PlanContentInput Content)
    : ICommand<PlanTemplateDto>;

/// <summary>Hides the template from pickers; plans already copied from it are untouched.</summary>
public sealed record ArchivePlanTemplateCommand(PlanActor Actor, Guid TemplateId) : ICommand;

/// <summary>Copies a template the actor can see, e.g. a coach makes a gym template their own.</summary>
public sealed record DuplicatePlanTemplateCommand(
    PlanActor Actor, Guid TemplateId, bool AsGymTemplate, string? Name, string? NameAr) : ICommand<PlanTemplateDto>;

/// <summary>Seeds the Arabic sample gym templates the first time a gym opens its templates.</summary>
public sealed record EnsureSamplePlanTemplatesCommand(Guid GymId) : ICommand<int>;
