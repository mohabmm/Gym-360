using GymApp.Common.Presentation;
using GymApp.Modules.Communication.Application.Commands.Broadcast;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Communication.Presentation.Endpoints.Conversations;

internal sealed class BroadcastEndpoint : IEndpoint
{
    internal sealed record BroadcastRequest(
        Guid GymId,
        Guid SenderId,
        string Content,
        List<Guid> RecipientIds);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/communication/broadcast", async (
            BroadcastRequest request,
            ISender sender) =>
        {
            var command = new BroadcastCommand(
                request.GymId,
                request.SenderId,
                request.Content,
                request.RecipientIds);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Broadcast a message")
        .WithDescription("Sends a broadcast message from a gym admin to multiple recipients at once.")
        .Produces(204)
        .WithTags("Communication")
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401);
    }
}
