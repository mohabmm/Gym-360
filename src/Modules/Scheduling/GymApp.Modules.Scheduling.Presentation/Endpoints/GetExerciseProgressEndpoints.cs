using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Presentation;
using GymApp.Common.Presentation.FeatureFlags;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Application.Queries.GetExerciseProgress;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Scheduling.Presentation.Endpoints;

/// <summary>
/// pt.exercise_progress — every number the coach recorded, per day type and per exercise.
/// The member reads their own board; a coach reads a client's board.
/// </summary>
internal sealed class GetExerciseProgressEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/scheduling/me/exercise-progress", (
                [FromHeader(Name = "X-Gym-Id")] Guid? headerGymId,
                HttpContext httpContext,
                ISender sender) => SendAsync(httpContext, sender, memberId: null))
            .RequireAuthorization("trainee")
            .WithSummary("My exercise progress")
            .WithDescription("The trainee's exercise board: what the coach recorded each session (sets, reps, weight, note, next target), grouped by day type, with trends. Trial-only members see their free-trial sessions; after paying the board keeps every session.")
            .WithProgressMetadata();

        app.MapGet("api/v1/scheduling/clients/{memberId:guid}/exercise-progress", (
                Guid memberId,
                [FromHeader(Name = "X-Gym-Id")] Guid? headerGymId,
                HttpContext httpContext,
                ISender sender) => SendAsync(httpContext, sender, memberId))
            .RequireAuthorization("trainer")
            .WithSummary("A client's exercise progress")
            .WithDescription("The same board for a coach who trained the client in this gym or coaches their active package.")
            .WithProgressMetadata();
    }

    private static async Task<IResult> SendAsync(HttpContext httpContext, ISender sender, Guid? memberId)
    {
        if (!UserContext.TryGetUserId(httpContext, out var callerId))
            return Results.Unauthorized();

        var gymId = UserContext.TryGetGymId(httpContext);
        if (gymId is null || gymId == Guid.Empty)
            return ApiResults.Problem("GYM_CONTEXT_REQUIRED", "Select a gym first (/api/v1/identity/select-gym).", 400);

        var result = await sender.Send(new GetExerciseProgressQuery(callerId, memberId ?? callerId, gymId.Value));

        return result.IsSuccess
            ? ApiResults.Ok(result.Value)
            : ApiResults.Problem(result.Error);
    }
}

internal static class ExerciseProgressEndpointMetadata
{
    public static RouteHandlerBuilder WithProgressMetadata(this RouteHandlerBuilder builder) =>
        builder
            .RequireFeature(FeatureKeys.PtExerciseProgress)
            .WithTags("Scheduling")
            .Produces<ApiResponse<ExerciseProgressDto>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden);
}
