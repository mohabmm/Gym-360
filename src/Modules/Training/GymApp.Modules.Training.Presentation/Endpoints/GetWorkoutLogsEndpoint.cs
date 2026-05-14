using GymApp.Common.Presentation;
using GymApp.Modules.Training.Application.DTOs;
using GymApp.Modules.Training.Application.Queries.GetWorkoutLogs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Training.Presentation.Endpoints;

internal sealed class GetWorkoutLogsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/training/workout-logs", async (
            HttpContext httpContext,
            [FromHeader(Name = "X-Gym-Id")] Guid? gymId,
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to,
            ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            if (gymId is null || gymId == Guid.Empty)
                return ApiResults.Problem("GYM_CONTEXT_REQUIRED", "The X-Gym-Id header is required.", 400);

            var query = new GetWorkoutLogsQuery(userId, gymId.Value, from, to);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Training")
        .WithSummary("List workout logs for the authenticated user")
        .WithDescription("Returns workout logs for the authenticated user within a gym, optionally filtered by date range.")
        .Produces<ApiResponse<List<WorkoutLogDto>>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .RequireAuthorization();
    }
}
