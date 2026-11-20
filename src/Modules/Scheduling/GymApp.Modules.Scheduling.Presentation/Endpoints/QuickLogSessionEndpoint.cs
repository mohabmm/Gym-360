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
    /// <param name="Date">Gym-local day the session happened (up to 7 days back); default today.</param>
    /// <param name="StartTime">Gym-local start time; omitted = a session that just finished.</param>
    internal sealed record Request(
        Guid MemberId,
        string? Notes,
        DateOnly? Date = null,
        TimeOnly? StartTime = null,
        int? DurationMinutes = null);

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

            var result = await sender.Send(new QuickLogSessionCommand(
                gymId.Value, trainerId, request.MemberId, request.Notes,
                request.Date, request.StartTime, request.DurationMinutes));

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization("trainer")
        .RequireFeature(FeatureKeys.PtDualSessionLogging)
        .WithTags("Scheduling")
        .WithSummary("Record a PT session that happened")
        .WithDescription("Coach records a session with a client who has an active package with them: today or up to 7 days back (date + start time, gym-local), duration 15–180 min (default 60). Without a time it is a session that just finished. The session is completed, the package is deducted, and the trainee is asked to confirm. Recording the same slot again (or a repeat one-tap within 5 minutes) returns the existing session with alreadyLogged = true.")
        .Produces<ApiResponse<QuickLogSessionResult>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);
    }
}
