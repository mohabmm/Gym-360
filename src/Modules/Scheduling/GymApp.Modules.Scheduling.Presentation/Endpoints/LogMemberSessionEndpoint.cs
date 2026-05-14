using GymApp.Common.Presentation;
using GymApp.Modules.Scheduling.Application.Commands.LogMemberSession;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Scheduling.Presentation.Endpoints;

internal sealed class LogMemberSessionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/scheduling/sessions/{sessionId:guid}/member-log", async (
            Guid sessionId,
            LogMemberSessionRequest request,
            HttpContext httpContext,
            ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(httpContext, out var memberId))
                return Results.Unauthorized();

            var command = new LogMemberSessionCommand(
                sessionId,
                memberId,
                request.Rating,
                request.PerceivedExertion,
                request.PersonalNotes,
                request.Discomfort,
                request.Satisfaction);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Scheduling")
        .WithSummary("Submit member session log")
        .WithDescription("Allows a member to submit their post-session feedback including rating, perceived exertion, personal notes, and satisfaction score.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict)
        .RequireAuthorization();
    }
}

internal sealed record LogMemberSessionRequest(
    int Rating,
    int PerceivedExertion,
    string? PersonalNotes,
    string? Discomfort,
    int Satisfaction);
