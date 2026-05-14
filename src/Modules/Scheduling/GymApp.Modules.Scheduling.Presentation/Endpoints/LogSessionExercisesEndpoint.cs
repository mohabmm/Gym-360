using GymApp.Common.Presentation;
using GymApp.Modules.Scheduling.Application.Commands.LogSessionExercises;
using GymApp.Modules.Scheduling.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Scheduling.Presentation.Endpoints;

internal sealed class LogSessionExercisesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/scheduling/sessions/{sessionId:guid}/exercise-logs", async (
            Guid sessionId,
            LogSessionExercisesRequest request,
            HttpContext httpContext,
            ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(httpContext, out var trainerId))
                return Results.Unauthorized();

            var items = (request.Items ?? Array.Empty<ExerciseLogItemRequest>())
                .Select(i => new ExerciseLogItem(
                    i.ExerciseId,
                    i.ExerciseName,
                    i.ExerciseNameAr,
                    i.OrderIndex,
                    i.Sets,
                    i.Reps,
                    i.Weight,
                    i.Notes))
                .ToList();

            var command = new LogSessionExercisesCommand(sessionId, trainerId, items);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value, $"api/v1/scheduling/sessions/{sessionId}/exercise-logs")
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Scheduling")
        .WithSummary("Log session exercises (bulk)")
        .WithDescription("Trainer records per-exercise actuals (sets, reps, weight, notes) for a completed session. Member sees them on session detail.")
        .Produces<ApiResponse<IReadOnlyList<SessionExerciseLogDto>>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}

internal sealed record LogSessionExercisesRequest(IReadOnlyList<ExerciseLogItemRequest>? Items);

internal sealed record ExerciseLogItemRequest(
    Guid? ExerciseId,
    string ExerciseName,
    string? ExerciseNameAr,
    int OrderIndex,
    int Sets,
    string Reps,
    decimal? Weight,
    string? Notes);
