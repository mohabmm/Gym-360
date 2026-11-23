using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Presentation;
using GymApp.Common.Presentation.FeatureFlags;
using GymApp.Modules.Training.Application.TrainingPlans;
using GymApp.Modules.Training.Application.TrainingPlans.Templates;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static GymApp.Modules.Training.Presentation.Endpoints.TrainingPlans.PlanEndpointSupport;

namespace GymApp.Modules.Training.Presentation.Endpoints.TrainingPlans;

/// <summary>
/// pt.training_plan — plan templates. Coaches see gym templates plus their own; the gym's owner
/// and managers create and edit gym templates (staff web).
/// </summary>
internal sealed class PlanTemplateEndpoints : IEndpoint
{
    internal sealed record CreateRequest(bool AsGymTemplate, PlanContentInput Content);

    internal sealed record DuplicateRequest(bool AsGymTemplate, string? Name, string? NameAr);

    private const string Route = "api/v1/training/plan-templates";

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route)
            .RequireAuthorization(CoachOrGymManager)
            .RequireFeature(FeatureKeys.PtTrainingPlan)
            .WithTags("Training plans");

        group.MapGet("", async (HttpContext http, ISender sender) =>
        {
            if (!TryGetActor(http, out var actor, out var problem))
                return problem!;
            await sender.Send(new EnsureSamplePlanTemplatesCommand(actor.GymId));
            return ToResult(await sender.Send(new ListPlanTemplatesQuery(actor, IncludeOwn: IsTrainer(http.User))));
        })
        .WithSummary("Plan templates: the gym's, then the coach's own")
        .WithDescription("The first call for a gym seeds three editable Arabic sample templates. Summaries only; open one for its days.")
        .Produces<ApiResponse<IReadOnlyList<PlanTemplateSummaryDto>>>(StatusCodes.Status200OK);

        group.MapGet("{templateId:guid}", async (Guid templateId, HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new GetPlanTemplateQuery(actor, templateId)))
                : problem!)
        .WithSummary("A template with its workout days")
        .Produces<ApiResponse<PlanTemplateDto>>(StatusCodes.Status200OK);

        group.MapPost("", async (CreateRequest request, HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new CreatePlanTemplateCommand(actor, request.AsGymTemplate, request.Content)))
                : problem!)
        .WithSummary("Create a template (gym template: owner or manager only)")
        .Produces<ApiResponse<PlanTemplateDto>>(StatusCodes.Status200OK);

        group.MapPut("{templateId:guid}", async (Guid templateId, PlanContentInput content, HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new UpdatePlanTemplateCommand(actor, templateId, content)))
                : problem!)
        .WithSummary("Edit a template. Plans already assigned from it do not change.")
        .Produces<ApiResponse<PlanTemplateDto>>(StatusCodes.Status200OK);

        group.MapDelete("{templateId:guid}", async (Guid templateId, HttpContext http, ISender sender) =>
        {
            if (!TryGetActor(http, out var actor, out var problem))
                return problem!;
            var result = await sender.Send(new ArchivePlanTemplateCommand(actor, templateId));
            return result.IsSuccess ? ApiResults.Ok(new { archived = true }) : ApiResults.Problem(result.Error);
        })
        .WithSummary("Archive a template (hidden from pickers; nothing is deleted)");

        group.MapPost("{templateId:guid}/duplicate", async (Guid templateId, DuplicateRequest request, HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new DuplicatePlanTemplateCommand(actor, templateId, request.AsGymTemplate, request.Name, request.NameAr)))
                : problem!)
        .WithSummary("Copy a template, e.g. make a gym template your own")
        .Produces<ApiResponse<PlanTemplateDto>>(StatusCodes.Status200OK);
    }
}
