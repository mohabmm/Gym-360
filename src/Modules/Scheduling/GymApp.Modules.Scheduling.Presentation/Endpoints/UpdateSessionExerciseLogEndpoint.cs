using GymApp.Common.Presentation;
using GymApp.Modules.Scheduling.Application.Commands.UpdateSessionExerciseLog;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Scheduling.Presentation.Endpoints;

internal sealed class UpdateSessionExerciseLogEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/scheduling/sessions/{sessionId:guid}/exercise-logs/{logId:guid}", async (
            Guid sessionId,
            Guid logId,
            UpdateSessionExerciseLogRequest request,
            HttpContext httpContext,
            ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(httpContext, out var trainerId))
                return Results.Unauthorized();

            var command = new UpdateSessionExerciseLogCommand(
                sessionId,
                logId,
                trainerId,
                request.ExerciseId,
                request.ExerciseName,
                request.ExerciseNameAr,
                request.OrderIndex,
                request.Sets,
                request.Reps,
                request.Weight,
                request.Notes);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Scheduling")
        .WithSummary("Update a session exercise log")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}

internal sealed record UpdateSessionExerciseLogRequest(
    Guid? ExerciseId,
    string ExerciseName,
    string? ExerciseNameAr,
    int OrderIndex,
    int Sets,
    string Reps,
    decimal? Weight,
    string? Notes);
