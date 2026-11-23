using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.Abstractions;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.TrainingPlans.Templates;

internal sealed class DuplicatePlanTemplateCommandHandler : ICommandHandler<DuplicatePlanTemplateCommand, PlanTemplateDto>
{
    private readonly PlanAccess _access;
    private readonly IPlanTemplateRepository _templates;
    private readonly ITrainingUnitOfWork _unitOfWork;

    public DuplicatePlanTemplateCommandHandler(PlanAccess access, IPlanTemplateRepository templates, ITrainingUnitOfWork unitOfWork)
    {
        _access = access;
        _templates = templates;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PlanTemplateDto>> Handle(DuplicatePlanTemplateCommand request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureEnabledAsync(request.Actor.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<PlanTemplateDto>(enabled.Error);
        if (request.AsGymTemplate && !request.Actor.IsGymManager)
            return Result.Failure<PlanTemplateDto>(TrainingPlanErrors.TemplateForbidden);
        var source = await _templates.GetByIdAsync(request.Actor.GymId, request.TemplateId, cancellationToken);
        if (source is null || !CanCopy(source, request.Actor))
            return Result.Failure<PlanTemplateDto>(TrainingPlanErrors.TemplateNotFound);

        var content = PlanContentFactory.Renamed(source.Content, request.Name ?? $"{source.Name} (2)", request.NameAr ?? source.NameAr);
        var copy = CreatePlanTemplateCommandHandler.NewTemplate(request.Actor, request.AsGymTemplate, content);
        await _templates.AddAsync(copy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(copy.ToDto(canEdit: true));
    }

    // Managers may copy any live template of the gym; coaches the ones they can see.
    private static bool CanCopy(Domain.Entities.PlanTemplate source, PlanActor actor) =>
        actor.IsGymManager ? !source.IsArchived : source.IsAvailableTo(actor.AsTemplateUser);
}
