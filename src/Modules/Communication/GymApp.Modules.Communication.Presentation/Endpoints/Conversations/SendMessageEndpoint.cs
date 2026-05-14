using GymApp.Common.Presentation;
using GymApp.Modules.Communication.Application.Commands.SendMessage;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Communication.Presentation.Endpoints.Conversations;

internal sealed class SendMessageEndpoint : IEndpoint
{
    internal sealed record SendMessageRequest(
        string Content,
        string? MediaUrl,
        string? MediaType);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/communication/conversations/{conversationId:guid}/messages", async (
            HttpContext httpContext,
            Guid conversationId,
            SendMessageRequest request,
            ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var senderId))
                return Results.Unauthorized();

            var command = new SendMessageCommand(
                conversationId,
                senderId,
                request.Content,
                request.MediaUrl,
                request.MediaType);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Send a message")
        .WithDescription("Sends a new message to an existing conversation. Sender derived from JWT.")
        .WithTags("Communication")
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404);
    }
}
