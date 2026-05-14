using GymApp.Common.Presentation;
using GymApp.Modules.Scheduling.Application.Commands.CancelSession;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Scheduling.Presentation.Endpoints;

internal sealed class CancelSessionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/scheduling/sessions/{sessionId:guid}/cancel", async (
            Guid sessionId,
            CancelSessionRequest request,
            ISender sender) =>
        {
            var command = new CancelSessionCommand(sessionId, request.CancelledBy, request.Reason);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Scheduling")
        .WithSummary("Cancel a session")
        .WithDescription("Cancels an existing scheduled session. The cancellation window is gym-configured (not hardcoded); late cancellations within the window deduct the session from the member's package.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}

internal sealed record CancelSessionRequest(
    Guid CancelledBy,
    string? Reason);
