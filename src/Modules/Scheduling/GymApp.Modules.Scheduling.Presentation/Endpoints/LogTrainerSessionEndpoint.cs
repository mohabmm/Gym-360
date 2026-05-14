using GymApp.Common.Presentation;
using GymApp.Modules.Scheduling.Application.Commands.LogTrainerSession;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Scheduling.Presentation.Endpoints;

internal sealed class LogTrainerSessionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/scheduling/sessions/{sessionId:guid}/trainer-log", async (
            Guid sessionId,
            LogTrainerSessionRequest request,
            HttpContext httpContext,
            ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(httpContext, out var trainerId))
                return Results.Unauthorized();

            var command = new LogTrainerSessionCommand(
                sessionId,
                trainerId,
                request.ExercisesPerformed,
                request.ActualVsPlanned,
                request.ClientEffortRating,
                request.Feedback,
                request.TechniqueNotes,
                request.AreasToImprove,
                request.NextSessionFocus,
                request.AttendanceConfirmed);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Scheduling")
        .WithSummary("Submit trainer session log")
        .WithDescription("Allows a trainer to record post-session notes including exercises performed, client effort rating, technique observations, and next session focus.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}

internal sealed record LogTrainerSessionRequest(
    string ExercisesPerformed,
    string? ActualVsPlanned,
    int ClientEffortRating,
    string? Feedback,
    string? TechniqueNotes,
    string? AreasToImprove,
    string? NextSessionFocus,
    bool AttendanceConfirmed);
