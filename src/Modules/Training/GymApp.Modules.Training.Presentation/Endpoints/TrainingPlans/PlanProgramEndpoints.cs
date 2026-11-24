using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Presentation;
using GymApp.Common.Presentation.FeatureFlags;
using GymApp.Modules.Training.Application.TrainingPlans.Programs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static GymApp.Modules.Training.Presentation.Endpoints.TrainingPlans.PlanEndpointSupport;

namespace GymApp.Modules.Training.Presentation.Endpoints.TrainingPlans;

/// <summary>
/// pt.training_plan — ready programs (برامج جاهزة): built-in plans every coach can start from.
/// Read-only; giving one to a client or saving it as a template goes through the plan and
/// template endpoints with the program's content.
/// </summary>
internal sealed class PlanProgramEndpoints : IEndpoint
{
    private const string Route = "api/v1/training/plan-programs";

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route)
            .RequireAuthorization(CoachOrGymManager)
            .RequireFeature(FeatureKeys.PtTrainingPlan)
            .WithTags("Training plans");

        group.MapGet("", async (HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new ListPlanProgramsQuery(actor)))
                : problem!)
        .WithSummary("Ready programs: classics, Egyptian gym staples, lifestyle and cardio")
        .Produces<ApiResponse<IReadOnlyList<PlanProgramSummaryDto>>>(StatusCodes.Status200OK);

        group.MapGet("{key}", async (string key, HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new GetPlanProgramQuery(actor, key)))
                : problem!)
        .WithSummary("A ready program with its days, linked to the gym's exercise library")
        .Produces<ApiResponse<PlanProgramDto>>(StatusCodes.Status200OK);
    }
}
