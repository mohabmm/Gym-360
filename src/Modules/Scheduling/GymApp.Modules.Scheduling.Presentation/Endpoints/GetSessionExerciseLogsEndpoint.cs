using GymApp.Common.Presentation;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Application.Queries.GetSessionExerciseLogs;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Scheduling.Presentation.Endpoints;

internal sealed class GetSessionExerciseLogsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/scheduling/sessions/{sessionId:guid}/exercise-logs", async (
            Guid sessionId,
            HttpContext httpContext,
            ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(httpContext, out var callerId))
                return Results.Unauthorized();

            var query = new GetSessionExerciseLogsQuery(sessionId, callerId);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Scheduling")
        .WithSummary("List exercise logs for a session")
        .WithDescription("Returns the structured per-exercise actuals (sets, reps, weight, notes) recorded by the trainer. Accessible to the session's trainer or member.")
        .Produces<ApiResponse<IReadOnlyList<SessionExerciseLogDto>>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}
