using GymApp.Common.Presentation;
using GymApp.Modules.Communication.Application.DTOs;
using GymApp.Modules.Communication.Application.Queries.GetConversations;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Communication.Presentation.Endpoints.Conversations;

internal sealed class GetConversationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/communication/conversations", async (
            HttpContext httpContext,
            [FromHeader(Name = "X-Gym-Id")] Guid? gymId,
            int? page,
            int? pageSize,
            ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            if (gymId is null || gymId == Guid.Empty)
                return ApiResults.Problem("GYM_CONTEXT_REQUIRED", "The X-Gym-Id header is required.", 400);

            var query = new GetConversationsQuery(userId, gymId.Value, page ?? 1, pageSize ?? 50);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Get user conversations")
        .WithDescription("Returns a paginated list of conversations for the authenticated user within the gym (X-Gym-Id header).")
        .WithTags("Communication")
        .Produces<ApiResponse<PagedResponse<ConversationDto>>>(200)
        .Produces<ApiResponse<object>>(401);
    }
}
