using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Presentation;
using GymApp.Common.Presentation.FeatureFlags;
using GymApp.Modules.Scheduling.Application.Commands.ReverseSession;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Application.Queries.GetSessionsForReview;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GymApp.Modules.Scheduling.Presentation.Endpoints;

/// <summary>pt.dual_session_logging — desk reviews disputed sessions and reverses them.</summary>
internal sealed class ReviewSessionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/scheduling/sessions/review", async (
            [FromQuery] string? memberConfirmation,
            [FromHeader(Name = "X-Gym-Id")] Guid? headerGymId,
            HttpContext httpContext,
            ISender sender) =>
        {
            var gymId = UserContext.TryGetGymId(httpContext);
            if (gymId is null || gymId == Guid.Empty)
                return ApiResults.Problem("GYM_CONTEXT_REQUIRED", "Select a gym first (/api/v1/identity/select-gym).", 400);

            var result = await sender.Send(new GetSessionsForReviewQuery(gymId.Value, memberConfirmation));
            return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("gym_staff")
        .RequireFeature(FeatureKeys.PtDualSessionLogging)
        .WithTags("Scheduling")
        .WithSummary("Sessions waiting for desk review (default: disputed by the trainee)")
        .Produces<ApiResponse<IReadOnlyList<SessionDto>>>(StatusCodes.Status200OK);

        app.MapPost("api/v1/scheduling/sessions/{sessionId:guid}/reverse", async (
            Guid sessionId,
            [FromHeader(Name = "X-Gym-Id")] Guid? headerGymId,
            HttpContext httpContext,
            ISender sender) =>
        {
            var gymId = UserContext.TryGetGymId(httpContext);
            if (gymId is null || gymId == Guid.Empty)
                return ApiResults.Problem("GYM_CONTEXT_REQUIRED", "Select a gym first (/api/v1/identity/select-gym).", 400);

            var result = await sender.Send(new ReverseSessionCommand(sessionId, gymId.Value));
            return result.IsSuccess ? Results.NoContent() : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("gym_staff")
        .RequireFeature(FeatureKeys.PtDualSessionLogging)
        .WithTags("Scheduling")
        .WithSummary("Reverse a disputed session (gives the package session back)")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);
    }
}
