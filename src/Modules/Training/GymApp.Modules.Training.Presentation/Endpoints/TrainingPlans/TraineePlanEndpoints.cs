using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Presentation;
using GymApp.Common.Presentation.FeatureFlags;
using GymApp.Modules.Training.Application.TrainingPlans.Workouts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static GymApp.Modules.Training.Presentation.Endpoints.TrainingPlans.PlanEndpointSupport;

namespace GymApp.Modules.Training.Presentation.Endpoints.TrainingPlans;

/// <summary>pt.training_plan — the trainee's "today's workout", offline-safe workout uploads, and progress.</summary>
internal sealed class TraineePlanEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var me = app.MapGroup("api/v1/training/me")
            .RequireAuthorization("trainee")
            .RequireFeature(FeatureKeys.PtTrainingPlan)
            .WithTags("Training plans");

        me.MapGet("plan", async (HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new GetMyPlanQuery(actor)))
                : problem!)
        .WithSummary("My active plan with today's (next) workout")
        .WithDescription("Plan days are a sequence: the next workout is the day after the furthest one done. data is null when the coach has not given a plan.")
        .Produces<ApiResponse<MyPlanDto>>(StatusCodes.Status200OK);

        me.MapPut("plan-workouts/{clientId:guid}", async (Guid clientId, PlanWorkoutInput workout, HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new SyncPlanWorkoutCommand(actor, clientId, workout)))
                : problem!)
        .WithSummary("Save a finished plan workout (offline-safe, idempotent)")
        .WithDescription("clientId is made on the device when the workout is finished; repeating the call returns the first save (outcome already_synced). Other outcomes: created, duplicate (same day already done that date), plan_changed, plan_replaced.")
        .Produces<ApiResponse<PlanWorkoutSyncDto>>(StatusCodes.Status200OK);

        me.MapGet("plan-progress", async (HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new GetPlanProgressQuery(actor, actor.UserId)))
                : problem!)
        .WithSummary("My plan workouts this week and month, history, and weight progress per exercise")
        .Produces<ApiResponse<PlanProgressDto>>(StatusCodes.Status200OK);

        app.MapGet("api/v1/training/trainees/{traineeId:guid}/plan-progress", async (Guid traineeId, HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new GetPlanProgressQuery(actor, traineeId)))
                : problem!)
        .RequireAuthorization(CoachOrGymManager)
        .RequireFeature(FeatureKeys.PtTrainingPlan)
        .WithTags("Training plans")
        .WithSummary("A trainee's plan progress (their coach or the gym's owner/manager)")
        .Produces<ApiResponse<PlanProgressDto>>(StatusCodes.Status200OK);
    }
}
