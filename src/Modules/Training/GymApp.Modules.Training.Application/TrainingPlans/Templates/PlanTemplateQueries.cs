using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.TrainingPlans.Templates;

/// <param name="IncludeOwn">Coaches see gym templates and their own; the staff web lists gym templates only.</param>
public sealed record ListPlanTemplatesQuery(PlanActor Actor, bool IncludeOwn) : IQuery<IReadOnlyList<PlanTemplateSummaryDto>>;

public sealed record GetPlanTemplateQuery(PlanActor Actor, Guid TemplateId) : IQuery<PlanTemplateDto>;

internal sealed class PlanTemplateQueryHandler
    : IQueryHandler<ListPlanTemplatesQuery, IReadOnlyList<PlanTemplateSummaryDto>>,
      IQueryHandler<GetPlanTemplateQuery, PlanTemplateDto>
{
    private readonly PlanAccess _access;
    private readonly IPlanTemplateRepository _templates;

    public PlanTemplateQueryHandler(PlanAccess access, IPlanTemplateRepository templates)
    {
        _access = access;
        _templates = templates;
    }

    public async Task<Result<IReadOnlyList<PlanTemplateSummaryDto>>> Handle(ListPlanTemplatesQuery request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureEnabledAsync(request.Actor.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<IReadOnlyList<PlanTemplateSummaryDto>>(enabled.Error);
        var templates = await _templates.ListVisibleAsync(
            request.Actor.GymId, request.IncludeOwn ? request.Actor.UserId : null, cancellationToken);
        return Result.Success<IReadOnlyList<PlanTemplateSummaryDto>>(
            templates.Select(t => t.ToSummary(PlanAccess.CanEdit(t, request.Actor))).ToList());
    }

    public async Task<Result<PlanTemplateDto>> Handle(GetPlanTemplateQuery request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureEnabledAsync(request.Actor.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<PlanTemplateDto>(enabled.Error);
        var template = await _templates.GetByIdAsync(request.Actor.GymId, request.TemplateId, cancellationToken);
        if (template?.IsAvailableTo(request.Actor.AsTemplateUser) != true)
            return Result.Failure<PlanTemplateDto>(TrainingPlanErrors.TemplateNotFound);
        return Result.Success(template.ToDto(PlanAccess.CanEdit(template, request.Actor)));
    }
}
