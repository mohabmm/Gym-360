using GymApp.Common.Presentation;
using GymApp.Modules.Communication.Application.Commands.CreateConversation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;


namespace GymApp.Modules.Communication.Presentation.Endpoints.Conversations;

internal sealed class CreateConversationEndpoint : IEndpoint
{
    internal sealed record CreateConversationRequest(
        Guid GymId,
        Guid Participant2);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/communication/conversations", async (
            CreateConversationRequest request,
            HttpContext httpContext,
            ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(httpContext, out var participant1))
                return Results.Unauthorized();

            var command = new CreateConversationCommand(
                request.GymId,
                participant1,
                request.Participant2);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value, $"api/v1/communication/conversations/{result.Value}")
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Create a new conversation")
        .WithDescription("Creates a direct conversation between two participants within a gym.")
        .WithTags("Communication")
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401);
    }
}
