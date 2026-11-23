using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.Abstractions;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Modules.Training.Application.TrainingPlans.Templates;

internal sealed class CreatePlanTemplateCommandHandler : ICommandHandler<CreatePlanTemplateCommand, PlanTemplateDto>
{
    private readonly PlanAccess _access;
    private readonly IPlanTemplateRepository _templates;
    private readonly ITrainingUnitOfWork _unitOfWork;

    public CreatePlanTemplateCommandHandler(PlanAccess access, IPlanTemplateRepository templates, ITrainingUnitOfWork unitOfWork)
    {
        _access = access;
        _templates = templates;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PlanTemplateDto>> Handle(CreatePlanTemplateCommand request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureEnabledAsync(request.Actor.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<PlanTemplateDto>(enabled.Error);
        if (request.AsGymTemplate && !request.Actor.IsGymManager)
            return Result.Failure<PlanTemplateDto>(TrainingPlanErrors.TemplateForbidden);
        var content = PlanContentFactory.From(request.Content);
        if (content.IsFailure)
            return Result.Failure<PlanTemplateDto>(content.Error);

        var template = NewTemplate(request.Actor, request.AsGymTemplate, content.Value);
        await _templates.AddAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(template.ToDto(canEdit: true));
    }

    internal static PlanTemplate NewTemplate(PlanActor actor, bool asGymTemplate, PlanContent content) =>
        asGymTemplate
            ? PlanTemplate.CreateForGym(actor.AsTemplateUser, content)
            : PlanTemplate.CreateForCoach(actor.AsTemplateUser, content);
}
