using GymApp.Common.Presentation;
using GymApp.Modules.Training.Application.DTOs;
using GymApp.Modules.Training.Application.Queries.GetTodaysWorkout;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Training.Presentation.Endpoints;

internal sealed class GetTodaysWorkoutEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/training/todays-workout", async (
            HttpContext httpContext,
            [FromHeader(Name = "X-Gym-Id")] Guid? gymIdHeader,
            [FromQuery] Guid? gymId,
            [FromQuery] Guid? userId,
            ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var authenticatedUserId))
                return Results.Unauthorized();

            var resolvedGymId = gymId ?? gymIdHeader ?? UserContext.TryGetGymId(httpContext);
            if (resolvedGymId is null || resolvedGymId == Guid.Empty)
                return ApiResults.Problem("GYM_CONTEXT_REQUIRED", "The X-Gym-Id header or gymId query parameter is required.", 400);

            var query = new GetTodaysWorkoutQuery(userId ?? authenticatedUserId, resolvedGymId.Value);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Training")
        .WithSummary("Get today's assigned workout for a user")
        .WithDescription("Returns the workout assigned to the authenticated user for today at the gym specified by X-Gym-Id (or gymId query parameter). Optional userId query parameter overrides for trainer lookups.")
        .Produces<ApiResponse<TodaysWorkoutResponse>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .RequireAuthorization();
    }
}
