using GymApp.Common.Presentation;
using GymApp.Modules.Communication.Application.DTOs;
using GymApp.Modules.Communication.Application.Queries.GetMessages;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Communication.Presentation.Endpoints.Conversations;

internal sealed class GetMessagesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/communication/conversations/{conversationId:guid}/messages", async (
            HttpContext httpContext,
            Guid conversationId,
            int? page,
            int? pageSize,
            ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            var query = new GetMessagesQuery(conversationId, userId, page ?? 1, pageSize ?? 50);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Get conversation messages")
        .WithDescription("Returns a paginated list of messages for a specific conversation. Caller derived from JWT.")
        .WithTags("Communication")
        .Produces<ApiResponse<PagedResponse<MessageDto>>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404);
    }
}
