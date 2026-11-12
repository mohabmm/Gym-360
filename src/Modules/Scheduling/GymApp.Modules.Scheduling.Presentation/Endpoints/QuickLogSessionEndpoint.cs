using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Presentation;
using GymApp.Common.Presentation.FeatureFlags;
using GymApp.Modules.Scheduling.Application.Commands.QuickLogSession;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GymApp.Modules.Scheduling.Presentation.Endpoints;

/// <summary>pt.dual_session_logging — coach logs a session in one tap (notes optional).</summary>
internal sealed class QuickLogSessionEndpoint : IEndpoint
{
    internal sealed record Request(Guid MemberId, string? Notes);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/scheduling/sessions/quick-log", async (
            Request request,
            [FromHeader(Name = "X-Gym-Id")] Guid? headerGymId,
            HttpContext httpContext,
            ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var trainerId))
                return Results.Unauthorized();

            var gymId = UserContext.TryGetGymId(httpContext);
            if (gymId is null || gymId == Guid.Empty)
                return ApiResults.Problem("GYM_CONTEXT_REQUIRED", "Select a gym first (/api/v1/identity/select-gym).", 400);

            var result = await sender.Send(new QuickLogSessionCommand(gymId.Value, trainerId, request.MemberId, request.Notes));

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("trainer")
        .RequireFeature(FeatureKeys.PtDualSessionLogging)
        .WithTags("Scheduling")
        .WithSummary("Quick-log a PT session (one tap)")
        .WithDescription("Coach logs a session that just happened with a client who has an active package with them. The session is completed immediately, the package is deducted, and the trainee is asked to confirm. A repeat tap within 5 minutes returns the same session.")
        .Produces<ApiResponse<QuickLogSessionResult>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);
    }
}
