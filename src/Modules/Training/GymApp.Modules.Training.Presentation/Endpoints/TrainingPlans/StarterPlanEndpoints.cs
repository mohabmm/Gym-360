using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Presentation;
using GymApp.Common.Presentation.FeatureFlags;
using GymApp.Modules.Training.Application.TrainingPlans.Starter;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static GymApp.Modules.Training.Presentation.Endpoints.TrainingPlans.PlanEndpointSupport;

namespace GymApp.Modules.Training.Presentation.Endpoints.TrainingPlans;

/// <summary>
/// pt.starter_plan — the coach checks what they may give a client (3-day plan, one week, or
/// anything); the staff (desk, manager, owner) unlock one week per coach–member pair.
/// </summary>
internal sealed class StarterPlanEndpoints : IEndpoint
{
    /// <param name="Length"><c>OneWeek</c> (default) or <c>ThirtyDays</c>.</param>
    internal sealed record UnlockRequest(Guid TrainerId, Guid TraineeId, bool Unlocked, UnlockLength? Length);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/training/trainees/{traineeId:guid}/plan-access", async (Guid traineeId, HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new GetCoachPlanAccessQuery(actor, traineeId)))
                : problem!)
        .RequireAuthorization("trainer")
        .RequireFeature(FeatureKeys.PtTrainingPlan)
        .WithTags("Training plans")
        .WithSummary("What plan can I give this client: a 3-day starter plan, one week, or anything?")
        .WithDescription("limit: Starter (3 days, no template) until the staff unlock you and this client, then OneWeek (1 week) or ThirtyDays (up to 4 weeks) from a template or ready program, which uses the unlock up; None while pt.starter_plan is off. limitedPlan is the client's active limited plan.")
        .Produces<ApiResponse<CoachPlanAccessDto>>(StatusCodes.Status200OK);

        var staff = app.MapGroup("api/v1/training/plan-unlocks")
            .RequireAuthorization("gym_staff")
            .RequireFeature(FeatureKeys.PtStarterPlan)
            .WithTags("Training plans");

        staff.MapGet("", async (Guid traineeId, HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new GetMemberProgramUnlocksQuery(actor.GymId, traineeId)))
                : problem!)
        .WithSummary("A member's coach and whether a one-week program is unlocked (staff)")
        .Produces<ApiResponse<MemberProgramUnlocksDto>>(StatusCodes.Status200OK);

        staff.MapPut("", async (UnlockRequest request, HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new SetProgramUnlockCommand(
                    new CoachTrainee(actor.GymId, request.TrainerId, request.TraineeId), actor.UserId, request.Unlocked,
                    request.Length ?? UnlockLength.OneWeek)))
                : problem!)
        .WithSummary("Unlock (unlocked: true) one program — length OneWeek or ThirtyDays — for a coach and member, or take it back (staff)")
        .WithDescription("Use it when the member stops coming. Unlocking needs the coach to have an active package with the member (404 Training.PlanUnlock.NotFound) and notifies the coach; the coach giving the week locks it again.")
        .Produces<ApiResponse<ProgramUnlockDto>>(StatusCodes.Status200OK);
    }
}
