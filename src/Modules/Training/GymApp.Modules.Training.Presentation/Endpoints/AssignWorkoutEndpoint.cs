using GymApp.Common.Presentation;
using GymApp.Modules.Training.Application.Commands.AssignWorkout;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Training.Presentation.Endpoints;

internal sealed class AssignWorkoutEndpoint : IEndpoint
{
    internal sealed record Request(
        Guid ClientId,
        Guid GymId,
        Guid WorkoutId,
        DateTime StartDate,
        DateTime? EndDate,
        string? Notes,
        bool IsRemoteMode);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/training/assignments/workout", async (Request request, HttpContext httpContext, ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(httpContext, out var trainerId))
                return Results.Unauthorized();

            var command = new AssignWorkoutCommand(
                trainerId,
                request.ClientId,
                request.GymId,
                request.WorkoutId,
                request.StartDate,
                request.EndDate,
                request.Notes,
                request.IsRemoteMode);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Training")
        .WithSummary("Assign a workout to a client")
        .WithDescription("Assigns a workout to a client for a date range, optionally in remote mode. Requires workout_builder feature enabled on gym plan.")
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .RequireAuthorization();
    }
}
