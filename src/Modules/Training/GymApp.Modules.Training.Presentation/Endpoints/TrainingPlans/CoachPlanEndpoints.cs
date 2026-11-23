using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Presentation;
using GymApp.Common.Presentation.FeatureFlags;
using GymApp.Modules.Training.Application.TrainingPlans.Coaching;
using GymApp.Modules.Training.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static GymApp.Modules.Training.Presentation.Endpoints.TrainingPlans.PlanEndpointSupport;

namespace GymApp.Modules.Training.Presentation.Endpoints.TrainingPlans;

/// <summary>pt.training_plan — the coach's clients at a glance, reminders, and the gym's plan settings.</summary>
internal sealed class CoachPlanEndpoints : IEndpoint
{
    internal sealed record NudgeRequest(string? Message);

    internal sealed record ChallengeRequest(int Target, int WindowDays, string? RewardTextAr, string? RewardTextEn);

    /// <summary>Send only what changes; challenge needs mem.first_month_challenge on.</summary>
    internal sealed record SettingsRequest(int? InactiveDays, ChallengeRequest? Challenge);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var coach = app.MapGroup("api/v1/training")
            .RequireAuthorization("trainer")
            .RequireFeature(FeatureKeys.PtTrainingPlan)
            .WithTags("Training plans");

        coach.MapGet("coach/plan-overview", async (HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new GetCoachPlanOverviewQuery(actor)))
                : problem!)
        .WithSummary("My clients' plans: this week, last workout, and who needs a nudge")
        .WithDescription("Clients with a plan and no workout for the gym's inactiveDays (default 5) come first.")
        .Produces<ApiResponse<CoachPlanOverviewDto>>(StatusCodes.Status200OK);

        coach.MapPost("trainees/{traineeId:guid}/plan-nudge", async (Guid traineeId, NudgeRequest request, HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new NudgeTraineeCommand(actor, traineeId, request.Message)))
                : problem!)
        .WithSummary("Send a client a push reminder about their plan (once a day per client)")
        .WithDescription("message is optional (max 300); without it the standard Arabic/English reminder is sent. 409 Training.PlanNudge.Conflict when already sent today.")
        .Produces<ApiResponse<NudgeResultDto>>(StatusCodes.Status200OK);

        var settings = app.MapGroup("api/v1/training/plan-settings")
            .RequireFeature(FeatureKeys.PtTrainingPlan)
            .WithTags("Training plans");

        settings.MapGet("", async (HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new GetPlanSettingsQuery(actor)))
                : problem!)
        .RequireAuthorization(CoachOrGymManager)
        .WithSummary("The gym's training-plan settings")
        .Produces<ApiResponse<PlanSettingsDto>>(StatusCodes.Status200OK);

        settings.MapPut("", async (SettingsRequest request, HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new UpdatePlanSettingsCommand(actor, request.InactiveDays, ToChallenge(request.Challenge))))
                : problem!)
        .RequireAuthorization("gym_owner_or_manager")
        .WithSummary("Change the gym's training-plan settings (owner or manager)")
        .WithDescription("inactiveDays 2–30; challenge {target 1–60, windowDays 7–90, rewardTextAr/En max 300, blank = default} needs mem.first_month_challenge.")
        .Produces<ApiResponse<PlanSettingsDto>>(StatusCodes.Status200OK);
    }

    private static ChallengeSettings? ToChallenge(ChallengeRequest? c) =>
        c is null ? null : new ChallengeSettings(c.Target, c.WindowDays, c.RewardTextAr, c.RewardTextEn);
}
