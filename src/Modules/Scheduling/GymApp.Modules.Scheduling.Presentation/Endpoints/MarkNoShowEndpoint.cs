using GymApp.Common.Presentation;
using GymApp.Modules.Scheduling.Application.Commands.MarkNoShow;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Scheduling.Presentation.Endpoints;

internal sealed class MarkNoShowEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/scheduling/sessions/{sessionId:guid}/no-show", async (
            Guid sessionId,
            ISender sender) =>
        {
            var command = new MarkNoShowCommand(sessionId);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Scheduling")
        .WithSummary("Mark session as no-show")
        .WithDescription("Marks a member as a no-show for a scheduled session. Per gym policy, a no-show may deduct sessions from the member's package.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}
