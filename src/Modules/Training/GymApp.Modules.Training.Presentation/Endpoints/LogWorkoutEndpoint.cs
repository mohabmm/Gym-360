using GymApp.Common.Presentation;
using GymApp.Modules.Training.Application.Commands.LogWorkout;
using GymApp.Modules.Training.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Training.Presentation.Endpoints;

internal sealed class LogWorkoutEndpoint : IEndpoint
{
    internal sealed record Request(
        Guid? WorkoutId,
        DateOnly Date,
        DateTime StartedAt,
        DateTime? CompletedAt,
        int? DurationMinutes,
        string? Notes,
        List<LogSetDto> Sets,
        int? PerceivedExertion);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/training/workout-logs", async (
            HttpContext httpContext,
            [FromHeader(Name = "X-Gym-Id")] Guid? gymId,
            Request request,
            ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            if (gymId is null || gymId == Guid.Empty)
                return ApiResults.Problem("GYM_CONTEXT_REQUIRED", "The X-Gym-Id header is required.", 400);

            var command = new LogWorkoutCommand(
                userId,
                gymId.Value,
                request.WorkoutId,
                request.Date,
                request.StartedAt,
                request.CompletedAt,
                request.DurationMinutes,
                request.Notes,
                request.Sets,
                request.PerceivedExertion);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Training")
        .WithSummary("Log a completed workout session")
        .WithDescription("Records a completed workout with all sets and perceived exertion. Requires session_logging feature enabled on gym plan.")
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .RequireAuthorization();
    }
}
