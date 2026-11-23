using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Presentation;
using GymApp.Common.Presentation.FeatureFlags;
using GymApp.Modules.Training.Application.TrainingPlans.Challenge;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static GymApp.Modules.Training.Presentation.Endpoints.TrainingPlans.PlanEndpointSupport;

namespace GymApp.Modules.Training.Presentation.Endpoints.TrainingPlans;

/// <summary>
/// mem.first_month_challenge (needs pt.training_plan too) — the trainee's progress, the desk's
/// "earned the reward" list; and the owner dashboard's plan summary (pt.owner_dashboard).
/// </summary>
internal sealed class ChallengeEndpoints : IEndpoint
{
    internal sealed record DeliveredRequest(bool Delivered);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/training/me/challenge", async (HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new GetMyChallengeQuery(actor)))
                : problem!)
        .RequireAuthorization("trainee")
        .RequireFeature(FeatureKeys.MemFirstMonthChallenge)
        .WithTags("Training plans")
        .WithSummary("My first-month challenge: workouts done in the window, the target and the reward")
        .WithDescription("status: active | reached | ended | not_started. Counts plan workouts (app or a PT session linked to a plan day) from the join date for windowDays.")
        .Produces<ApiResponse<ChallengeStatusDto>>(StatusCodes.Status200OK);

        var desk = app.MapGroup("api/v1/training/challenge/awards")
            .RequireAuthorization("gym_staff")
            .RequireFeature(FeatureKeys.MemFirstMonthChallenge)
            .WithTags("Training plans");

        desk.MapGet("", async (bool? delivered, HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new ListChallengeAwardsQuery(actor, delivered)))
                : problem!)
        .WithSummary("Members who earned the first-month reward (desk)")
        .WithDescription("delivered=false: still to hand over; true: handed over; omitted: all. Newest first.")
        .Produces<ApiResponse<IReadOnlyList<ChallengeAwardDto>>>(StatusCodes.Status200OK);

        desk.MapPut("{awardId:guid}/delivered", async (Guid awardId, DeliveredRequest request, HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new SetChallengeAwardDeliveredCommand(actor, awardId, request.Delivered)))
                : problem!)
        .WithSummary("Tick (or untick) \"reward delivered\" (desk)")
        .Produces<ApiResponse<ChallengeAwardDto>>(StatusCodes.Status200OK);

        app.MapGet("api/v1/training/reports/plan-summary", async (HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new GetPlanSummaryQuery(actor)))
                : problem!)
        .RequireAuthorization("gym_owner_or_manager")
        .RequireFeature(FeatureKeys.PtOwnerDashboard)
        .WithTags("Training plans")
        .WithSummary("Owner dashboard: active plans, plan workouts this month, members who reached the challenge")
        .Produces<ApiResponse<PlanSummaryDto>>(StatusCodes.Status200OK);
    }
}
