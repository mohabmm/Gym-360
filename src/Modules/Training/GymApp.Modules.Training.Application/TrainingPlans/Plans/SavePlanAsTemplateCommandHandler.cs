using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.Abstractions;
using GymApp.Modules.Training.Application.TrainingPlans.Templates;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.TrainingPlans.Plans;

internal sealed class SavePlanAsTemplateCommandHandler : ICommandHandler<SavePlanAsTemplateCommand, PlanTemplateDto>
{
    private readonly PlanAccess _access;
    private readonly ITrainingPlanRepository _plans;
    private readonly IPlanTemplateRepository _templates;
    private readonly ITrainingUnitOfWork _unitOfWork;

    public SavePlanAsTemplateCommandHandler(
        PlanAccess access,
        ITrainingPlanRepository plans,
        IPlanTemplateRepository templates,
        ITrainingUnitOfWork unitOfWork)
    {
        _access = access;
        _plans = plans;
        _templates = templates;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PlanTemplateDto>> Handle(SavePlanAsTemplateCommand request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureEnabledAsync(request.Actor.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<PlanTemplateDto>(enabled.Error);
        if (request.AsGymTemplate && !request.Actor.IsGymManager)
            return Result.Failure<PlanTemplateDto>(TrainingPlanErrors.TemplateForbidden);
        var plan = await _plans.GetByIdAsync(request.Actor.GymId, request.PlanId, cancellationToken);
        if (plan is null)
            return Result.Failure<PlanTemplateDto>(TrainingPlanErrors.PlanNotFound);
        if (!await _access.CanManageAsync(request.Actor, plan, cancellationToken))
            return PlanAccess.Forbidden<PlanTemplateDto>();

        var content = PlanContentFactory.Renamed(plan.Content, request.Name, request.NameAr);
        var template = CreatePlanTemplateCommandHandler.NewTemplate(request.Actor, request.AsGymTemplate, content);
        await _templates.AddAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(template.ToDto(canEdit: true));
    }
}
