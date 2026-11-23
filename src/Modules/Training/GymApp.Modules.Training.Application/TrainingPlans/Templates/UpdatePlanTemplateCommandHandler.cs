using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.Abstractions;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.TrainingPlans.Templates;

internal sealed class UpdatePlanTemplateCommandHandler
    : ICommandHandler<UpdatePlanTemplateCommand, PlanTemplateDto>, ICommandHandler<ArchivePlanTemplateCommand>
{
    private readonly PlanAccess _access;
    private readonly IPlanTemplateRepository _templates;
    private readonly ITrainingUnitOfWork _unitOfWork;

    public UpdatePlanTemplateCommandHandler(PlanAccess access, IPlanTemplateRepository templates, ITrainingUnitOfWork unitOfWork)
    {
        _access = access;
        _templates = templates;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PlanTemplateDto>> Handle(UpdatePlanTemplateCommand request, CancellationToken cancellationToken)
    {
        var found = await LoadEditableAsync(request.Actor, request.TemplateId, cancellationToken);
        if (found.IsFailure)
            return Result.Failure<PlanTemplateDto>(found.Error);
        var content = PlanContentFactory.From(request.Content);
        if (content.IsFailure)
            return Result.Failure<PlanTemplateDto>(content.Error);

        found.Value.Update(content.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(found.Value.ToDto(canEdit: true));
    }

    public async Task<Result> Handle(ArchivePlanTemplateCommand request, CancellationToken cancellationToken)
    {
        var found = await LoadEditableAsync(request.Actor, request.TemplateId, cancellationToken);
        if (found.IsFailure)
            return found;
        found.Value.Archive();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result<PlanTemplate>> LoadEditableAsync(PlanActor actor, Guid templateId, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureEnabledAsync(actor.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<PlanTemplate>(enabled.Error);
        var template = await _templates.GetByIdAsync(actor.GymId, templateId, cancellationToken);
        if (template is null || template.IsArchived)
            return Result.Failure<PlanTemplate>(TrainingPlanErrors.TemplateNotFound);
        return PlanAccess.CanEdit(template, actor)
            ? Result.Success(template)
            : Result.Failure<PlanTemplate>(TrainingPlanErrors.TemplateForbidden);
    }
}
